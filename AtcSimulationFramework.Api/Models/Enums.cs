namespace AtcSimulationFramework.Api.Models;

public enum SimulationStatus
{
    Created = 0,
    Running = 1,
    Paused = 2,
    Completed = 3,
    Terminated = 4
}

public enum AircraftStatus
{
    Airborne = 0,
    Approaching = 1,
    Conflict = 2,
    Emergency = 3,
    Landed = 4
}

public enum ConflictType
{
    LossOfSeparation = 0,
    TerrainProximity = 1,
    RouteConvergence = 2
}

public enum ConflictSeverity
{
    Low = 0,
    Medium = 1,
    Critical = 2
}

public enum VectorCommandType
{
    Heading = 0,
    Altitude = 1,
    Speed = 2,
    DirectToWaypoint = 3,
    Squawk = 4
}

public enum VectorCommandStatus
{
    Pending = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum UserRole
{
    Controller = 0,
    Supervisor = 1,
    Admin = 2
}
