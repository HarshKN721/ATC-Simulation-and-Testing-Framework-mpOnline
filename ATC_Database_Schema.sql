-- ============================================================================
-- Automated Air Traffic Control (ATC) Simulation & Testing Framework
-- Database Schema Script (SQL Server — Database-First EF Core ready)
--
-- Run once against a SQL Server instance to create the database, tables,
-- indexes, constraints, and seed data.
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- 0. CREATE DATABASE (idempotent)
-- ────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'ATC_DB')
BEGIN
    CREATE DATABASE [ATC_DB];
END
GO

USE [ATC_DB];
GO

-- ────────────────────────────────────────────────────────────────────────────
-- Drop tables in reverse dependency order (safe re-runs)
-- ────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.VectorCommand', 'U') IS NOT NULL DROP TABLE dbo.VectorCommand;
IF OBJECT_ID('dbo.ConflictEvent', 'U') IS NOT NULL DROP TABLE dbo.ConflictEvent;
IF OBJECT_ID('dbo.PositionLog', 'U') IS NOT NULL DROP TABLE dbo.PositionLog;
IF OBJECT_ID('dbo.Aircraft', 'U') IS NOT NULL DROP TABLE dbo.Aircraft;
IF OBJECT_ID('dbo.SimulationRun', 'U') IS NOT NULL DROP TABLE dbo.SimulationRun;
IF OBJECT_ID('dbo.AppUser', 'U') IS NOT NULL DROP TABLE dbo.AppUser;
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 1. AppUser — authenticated operators (role-based access)
-- ════════════════════════════════════════════════════════════════════════════
CREATE TABLE dbo.AppUser (
    UserId       INT            IDENTITY(1,1)  NOT NULL,
    Username     VARCHAR(50)    NOT NULL,
    PasswordHash VARBINARY(256) NOT NULL,
    Role         VARCHAR(20)    NOT NULL,
    CreatedAt    DATETIME2      NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AppUser            PRIMARY KEY CLUSTERED (UserId ASC),
    CONSTRAINT UQ_AppUser_Username   UNIQUE (Username),
    CONSTRAINT CK_AppUser_Role       CHECK (Role IN ('Supervisor', 'Engineer', 'Analyst'))
);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 2. SimulationRun — each simulation session
-- ════════════════════════════════════════════════════════════════════════════
CREATE TABLE dbo.SimulationRun (
    RunId           INT         IDENTITY(1,1)  NOT NULL,
    StartTime       DATETIME2   NOT NULL,
    EndTime         DATETIME2   NULL,
    Status          VARCHAR(20) NOT NULL,
    CreatedByUserId INT         NOT NULL,

    CONSTRAINT PK_SimulationRun          PRIMARY KEY CLUSTERED (RunId ASC),
    CONSTRAINT FK_SimulationRun_AppUser  FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AppUser(UserId),
    CONSTRAINT CK_SimulationRun_Status   CHECK (Status IN ('Running', 'Completed', 'Aborted')),
    CONSTRAINT CK_SimulationRun_Times    CHECK (EndTime IS NULL OR EndTime >= StartTime)
);
GO

-- Tick loop: FirstOrDefault(r => r.Status == "Running")
-- Controller: OrderByDescending(r => r.StartTime)
CREATE NONCLUSTERED INDEX IX_SimulationRun_Status
    ON dbo.SimulationRun (Status)
    INCLUDE (StartTime, EndTime, CreatedByUserId);
GO

CREATE NONCLUSTERED INDEX IX_SimulationRun_StartTime
    ON dbo.SimulationRun (StartTime DESC);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 3. Aircraft — aircraft participating in a run
-- ════════════════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Aircraft (
    AircraftId INT         IDENTITY(1,1)  NOT NULL,
    Callsign   VARCHAR(10) NOT NULL,
    Icao24     VARCHAR(6)  NOT NULL,
    RunId      INT         NOT NULL,

    CONSTRAINT PK_Aircraft               PRIMARY KEY CLUSTERED (AircraftId ASC),
    CONSTRAINT FK_Aircraft_SimulationRun FOREIGN KEY (RunId) REFERENCES dbo.SimulationRun(RunId),
    CONSTRAINT CK_Aircraft_Callsign     CHECK (LEN(Callsign) >= 2),
    CONSTRAINT CK_Aircraft_Icao24       CHECK (LEN(Icao24) = 6)
);
GO

-- Tick loop + API: WHERE RunId = @runId
CREATE NONCLUSTERED INDEX IX_Aircraft_RunId
    ON dbo.Aircraft (RunId)
    INCLUDE (Callsign, Icao24);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 4. PositionLog — per-tick position history (high-volume table)
--    The tick loop writes one row per aircraft per second and reads the
--    latest row per aircraft every tick. This is the hottest table.
-- ════════════════════════════════════════════════════════════════════════════
CREATE TABLE dbo.PositionLog (
    PositionId  BIGINT       IDENTITY(1,1)  NOT NULL,
    AircraftId  INT          NOT NULL,
    Timestamp   DATETIME2    NOT NULL,
    Latitude    DECIMAL(9,6) NOT NULL,
    Longitude   DECIMAL(9,6) NOT NULL,
    AltitudeFt  INT          NOT NULL,
    HeadingDeg  DECIMAL(5,2) NOT NULL,
    SpeedKts    INT          NOT NULL,

    CONSTRAINT PK_PositionLog            PRIMARY KEY CLUSTERED (PositionId ASC),
    CONSTRAINT FK_PositionLog_Aircraft   FOREIGN KEY (AircraftId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT CK_PositionLog_Latitude   CHECK (Latitude  BETWEEN -90.0 AND 90.0),
    CONSTRAINT CK_PositionLog_Longitude  CHECK (Longitude BETWEEN -180.0 AND 180.0),
    CONSTRAINT CK_PositionLog_Heading    CHECK (HeadingDeg >= 0 AND HeadingDeg < 360),
    CONSTRAINT CK_PositionLog_Altitude   CHECK (AltitudeFt >= 0),
    CONSTRAINT CK_PositionLog_Speed      CHECK (SpeedKts >= 0)
);
GO

-- Critical: "latest position per aircraft" query every tick
--   .Where(p => aircraftIds.Contains(p.AircraftId))
--   .GroupBy(p => p.AircraftId)
--   .Select(g => g.OrderByDescending(p => p.Timestamp).First())
-- Also used for pruning: WHERE AircraftId IN (...) AND Timestamp < @cutoff
CREATE NONCLUSTERED INDEX IX_PositionLog_AircraftId_Timestamp
    ON dbo.PositionLog (AircraftId, Timestamp DESC)
    INCLUDE (Latitude, Longitude, AltitudeFt, HeadingDeg, SpeedKts);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 5. ConflictEvent — detected separation violations
-- ════════════════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ConflictEvent (
    ConflictId       INT           IDENTITY(1,1)  NOT NULL,
    RunId            INT           NOT NULL,
    AircraftAId      INT           NOT NULL,
    AircraftBId      INT           NOT NULL,
    DetectedAt       DATETIME2     NOT NULL,
    HorizontalDistNm DECIMAL(6,3) NOT NULL,
    VerticalDistFt   INT           NOT NULL,
    ResolvedAt       DATETIME2     NULL,
    ResolutionAction VARCHAR(100)  NULL,

    CONSTRAINT PK_ConflictEvent                PRIMARY KEY CLUSTERED (ConflictId ASC),
    CONSTRAINT FK_ConflictEvent_SimulationRun  FOREIGN KEY (RunId) REFERENCES dbo.SimulationRun(RunId),
    CONSTRAINT FK_ConflictEvent_AircraftA      FOREIGN KEY (AircraftAId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT FK_ConflictEvent_AircraftB      FOREIGN KEY (AircraftBId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT CK_ConflictEvent_DifferentAC    CHECK (AircraftAId <> AircraftBId),
    CONSTRAINT CK_ConflictEvent_HorizDist      CHECK (HorizontalDistNm >= 0),
    CONSTRAINT CK_ConflictEvent_VertDist       CHECK (VerticalDistFt >= 0),
    CONSTRAINT CK_ConflictEvent_ResolvedAt     CHECK (ResolvedAt IS NULL OR ResolvedAt >= DetectedAt)
);
GO

-- Tick loop: unresolved conflicts for a run
--   .Where(c => c.RunId == runId && c.ResolvedAt == null)
-- This filtered index only stores unresolved rows → very compact.
-- NOTE: Filtered indexes require QUOTED_IDENTIFIER ON.
SET QUOTED_IDENTIFIER ON;
GO

CREATE NONCLUSTERED INDEX IX_ConflictEvent_Unresolved
    ON dbo.ConflictEvent (RunId, AircraftAId, AircraftBId)
    INCLUDE (DetectedAt, HorizontalDistNm, VerticalDistFt, ResolvedAt, ResolutionAction)
    WHERE ResolvedAt IS NULL;
GO

-- API: fetch all conflicts for a run (including resolved)
CREATE NONCLUSTERED INDEX IX_ConflictEvent_RunId
    ON dbo.ConflictEvent (RunId, DetectedAt DESC);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 6. VectorCommand — commands issued by the Controller Agent
-- ════════════════════════════════════════════════════════════════════════════
CREATE TABLE dbo.VectorCommand (
    CommandId   INT           IDENTITY(1,1)  NOT NULL,
    ConflictId  INT           NOT NULL,
    AircraftId  INT           NOT NULL,
    CommandType VARCHAR(20)   NOT NULL,
    Value       DECIMAL(6,2)  NOT NULL,
    IssuedAt    DATETIME2     NOT NULL,

    CONSTRAINT PK_VectorCommand                 PRIMARY KEY CLUSTERED (CommandId ASC),
    CONSTRAINT FK_VectorCommand_ConflictEvent   FOREIGN KEY (ConflictId) REFERENCES dbo.ConflictEvent(ConflictId),
    CONSTRAINT FK_VectorCommand_Aircraft        FOREIGN KEY (AircraftId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT CK_VectorCommand_CommandType     CHECK (CommandType IN ('Heading', 'Altitude'))
);
GO

-- API: join VectorCommand → ConflictEvent → Aircraft WHERE c.RunId = @id
CREATE NONCLUSTERED INDEX IX_VectorCommand_ConflictId
    ON dbo.VectorCommand (ConflictId)
    INCLUDE (AircraftId, CommandType, Value, IssuedAt);
GO

-- API: commands for a specific aircraft
CREATE NONCLUSTERED INDEX IX_VectorCommand_AircraftId
    ON dbo.VectorCommand (AircraftId, IssuedAt DESC);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- SEED DATA — three default users, one per role
-- ════════════════════════════════════════════════════════════════════════════
INSERT INTO dbo.AppUser (Username, PasswordHash, Role)
VALUES 
    ('supervisor_admin', 0x0102030405060708090A0B0C0D0E0F, 'Supervisor'),
    ('engineer_dev',     0x0102030405060708090A0B0C0D0E0F, 'Engineer'),
    ('analyst_user',     0x0102030405060708090A0B0C0D0E0F, 'Analyst');
GO

PRINT '✅ ATC_DB schema created successfully with all indexes and constraints.';
GO
