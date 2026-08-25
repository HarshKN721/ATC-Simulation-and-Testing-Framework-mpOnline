using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.DTOs;

public record IssueVectorCommandRequest(
    int AircraftId,
    VectorCommandType CommandType,
    double TargetValue,
    string? WaypointTarget = null,
    int? IssuedByUserId = null);

public record VectorCommandDetailDto(
    int Id,
    int SimulationRunId,
    int AircraftId,
    string AircraftCallsign,
    VectorCommandType CommandType,
    string CommandTypeDescription,
    double TargetValue,
    string? WaypointTarget,
    long IssuedAtTick,
    VectorCommandStatus Status,
    string StatusDescription,
    DateTime IssuedAt,
    DateTime? ExecutedAt);
