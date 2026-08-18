-- ============================================================================
-- Automated Air Traffic Control (ATC) Simulation & Testing Framework
-- Database Schema Script (SQL Server - Database-First EF Core ready)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'ATC_DB')
BEGIN
    CREATE DATABASE [ATC_DB];
END
GO

USE [ATC_DB];
GO

-- Drop tables in reverse dependency order
IF OBJECT_ID('dbo.VectorCommand', 'U') IS NOT NULL DROP TABLE dbo.VectorCommand;
IF OBJECT_ID('dbo.ConflictEvent', 'U') IS NOT NULL DROP TABLE dbo.ConflictEvent;
IF OBJECT_ID('dbo.PositionLog', 'U') IS NOT NULL DROP TABLE dbo.PositionLog;
IF OBJECT_ID('dbo.Aircraft', 'U') IS NOT NULL DROP TABLE dbo.Aircraft;
IF OBJECT_ID('dbo.SimulationRun', 'U') IS NOT NULL DROP TABLE dbo.SimulationRun;
IF OBJECT_ID('dbo.AppUser', 'U') IS NOT NULL DROP TABLE dbo.AppUser;
GO

-- 1. AppUser Table
CREATE TABLE dbo.AppUser (
    UserId INT IDENTITY(1,1) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARBINARY(256) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    CONSTRAINT PK_AppUser PRIMARY KEY CLUSTERED (UserId ASC),
    CONSTRAINT UQ_AppUser_Username UNIQUE (Username),
    CONSTRAINT CK_AppUser_Role CHECK (Role IN ('Supervisor', 'Engineer', 'Analyst'))
);
GO

-- 2. SimulationRun Table
CREATE TABLE dbo.SimulationRun (
    RunId INT IDENTITY(1,1) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status VARCHAR(20) NOT NULL,
    CreatedByUserId INT NOT NULL,
    CONSTRAINT PK_SimulationRun PRIMARY KEY CLUSTERED (RunId ASC),
    CONSTRAINT FK_SimulationRun_AppUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AppUser(UserId),
    CONSTRAINT CK_SimulationRun_Status CHECK (Status IN ('Running', 'Completed', 'Aborted'))
);
GO

-- 3. Aircraft Table
CREATE TABLE dbo.Aircraft (
    AircraftId INT IDENTITY(1,1) NOT NULL,
    Callsign VARCHAR(10) NOT NULL,
    Icao24 VARCHAR(6) NOT NULL,
    RunId INT NOT NULL,
    CONSTRAINT PK_Aircraft PRIMARY KEY CLUSTERED (AircraftId ASC),
    CONSTRAINT FK_Aircraft_SimulationRun FOREIGN KEY (RunId) REFERENCES dbo.SimulationRun(RunId)
);
GO

-- 4. PositionLog Table
CREATE TABLE dbo.PositionLog (
    PositionId BIGINT IDENTITY(1,1) NOT NULL,
    AircraftId INT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    Latitude DECIMAL(9,6) NOT NULL,
    Longitude DECIMAL(9,6) NOT NULL,
    AltitudeFt INT NOT NULL,
    HeadingDeg DECIMAL(5,2) NOT NULL,
    SpeedKts INT NOT NULL,
    CONSTRAINT PK_PositionLog PRIMARY KEY CLUSTERED (PositionId ASC),
    CONSTRAINT FK_PositionLog_Aircraft FOREIGN KEY (AircraftId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT CK_PositionLog_Heading CHECK (HeadingDeg >= 0 AND HeadingDeg < 360)
);
GO

-- 5. ConflictEvent Table
CREATE TABLE dbo.ConflictEvent (
    ConflictId INT IDENTITY(1,1) NOT NULL,
    RunId INT NOT NULL,
    AircraftAId INT NOT NULL,
    AircraftBId INT NOT NULL,
    DetectedAt DATETIME2 NOT NULL,
    HorizontalDistNm DECIMAL(6,3) NOT NULL,
    VerticalDistFt INT NOT NULL,
    ResolvedAt DATETIME2 NULL,
    ResolutionAction VARCHAR(50) NULL,
    CONSTRAINT PK_ConflictEvent PRIMARY KEY CLUSTERED (ConflictId ASC),
    CONSTRAINT FK_ConflictEvent_SimulationRun FOREIGN KEY (RunId) REFERENCES dbo.SimulationRun(RunId),
    CONSTRAINT FK_ConflictEvent_AircraftA FOREIGN KEY (AircraftAId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT FK_ConflictEvent_AircraftB FOREIGN KEY (AircraftBId) REFERENCES dbo.Aircraft(AircraftId)
);
GO

-- 6. VectorCommand Table
CREATE TABLE dbo.VectorCommand (
    CommandId INT IDENTITY(1,1) NOT NULL,
    ConflictId INT NOT NULL,
    AircraftId INT NOT NULL,
    CommandType VARCHAR(20) NOT NULL,
    Value DECIMAL(6,2) NOT NULL,
    IssuedAt DATETIME2 NOT NULL,
    CONSTRAINT PK_VectorCommand PRIMARY KEY CLUSTERED (CommandId ASC),
    CONSTRAINT FK_VectorCommand_ConflictEvent FOREIGN KEY (ConflictId) REFERENCES dbo.ConflictEvent(ConflictId),
    CONSTRAINT FK_VectorCommand_Aircraft FOREIGN KEY (AircraftId) REFERENCES dbo.Aircraft(AircraftId),
    CONSTRAINT CK_VectorCommand_CommandType CHECK (CommandType IN ('Heading', 'Altitude'))
);
GO

-- Seed Data
INSERT INTO dbo.AppUser (Username, PasswordHash, Role)
VALUES 
    ('supervisor_admin', 0x0102030405060708090A0B0C0D0E0F, 'Supervisor'),
    ('engineer_dev',     0x0102030405060708090A0B0C0D0E0F, 'Engineer'),
    ('analyst_user',     0x0102030405060708090A0B0C0D0E0F, 'Analyst');
GO
