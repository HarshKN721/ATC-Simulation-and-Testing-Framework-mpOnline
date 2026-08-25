using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.DTOs;

public record RadarSnapshotDto(
    int SimulationRunId,
    string SimulationName,
    SimulationStatus Status,
    string StatusDescription,
    long CurrentTick,
    double ElapsedSeconds,
    DateTime ServerTimestampUtc,
    List<AircraftRadarDto> Aircraft,
    List<ConflictRadarDto> ActiveConflicts,
    List<VectorCommandRadarDto> ActiveCommands);

public record AircraftRadarDto(
    int Id,
    string Callsign,
    string ModelType,
    double Latitude,
    double Longitude,
    double AltitudeFeet,
    double SpeedKnots,
    double HeadingDegrees,
    double TargetAltitudeFeet,
    double TargetSpeedKnots,
    double TargetHeadingDegrees,
    double VerticalSpeedFpm,
    AircraftStatus Status,
    string StatusDescription,
    string SquawkCode,
    string AssignedSector,
    string FlightPlanRoute,
    DateTime UpdatedAt);

public record ConflictRadarDto(
    int Id,
    int PrimaryAircraftId,
    string PrimaryCallsign,
    int SecondaryAircraftId,
    string SecondaryCallsign,
    ConflictType ConflictType,
    ConflictSeverity Severity,
    string SeverityDescription,
    double DistanceNauticalMiles,
    double AltitudeDifferenceFeet,
    long DetectedAtTick,
    DateTime CreatedAt);

public record VectorCommandRadarDto(
    int Id,
    int AircraftId,
    string AircraftCallsign,
    VectorCommandType CommandType,
    string CommandTypeDescription,
    double TargetValue,
    string? WaypointTarget,
    VectorCommandStatus Status,
    DateTime IssuedAt);
