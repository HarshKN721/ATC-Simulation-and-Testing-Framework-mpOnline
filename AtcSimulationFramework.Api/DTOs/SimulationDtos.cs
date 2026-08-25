using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.DTOs;

public record CreateSimulationRequest(
    string Name,
    int? CreatedByUserId,
    int TickIntervalMs = 1000);

public record SimulationResponse(
    int Id,
    string Name,
    SimulationStatus Status,
    string StatusDescription,
    DateTime? StartTime,
    DateTime? EndTime,
    int TickIntervalMs,
    long CurrentTick,
    double ElapsedSeconds,
    DateTime CreatedAt,
    int AircraftCount,
    int ActiveConflictCount);

public record LoadScenarioRequest(
    string ScenarioType, // "converging", "head_on", "sector_busy"
    string? CustomName);
