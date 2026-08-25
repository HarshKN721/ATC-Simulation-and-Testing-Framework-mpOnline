using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.DTOs;

public record ConflictDetailDto(
    int Id,
    int SimulationRunId,
    int PrimaryAircraftId,
    string PrimaryAircraftCallsign,
    int SecondaryAircraftId,
    string SecondaryAircraftCallsign,
    ConflictType ConflictType,
    ConflictSeverity Severity,
    string SeverityDescription,
    double DistanceNauticalMiles,
    double AltitudeDifferenceFeet,
    long DetectedAtTick,
    long? ResolvedAtTick,
    bool IsResolved,
    DateTime CreatedAt,
    DateTime? ResolvedAt);
