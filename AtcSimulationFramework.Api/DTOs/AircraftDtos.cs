using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.DTOs;

public record CreateAircraftRequest(
    string Callsign,
    string ModelType,
    double InitialLatitude,
    double InitialLongitude,
    double InitialAltitudeFeet,
    double InitialSpeedKnots,
    double InitialHeadingDegrees,
    double? TargetAltitudeFeet = null,
    double? TargetSpeedKnots = null,
    double? TargetHeadingDegrees = null,
    string? SquawkCode = null,
    string? AssignedSector = null,
    string? FlightPlanRoute = null);

public record AircraftDetailDto(
    int Id,
    int SimulationRunId,
    string Callsign,
    string ModelType,
    double CurrentLatitude,
    double CurrentLongitude,
    double CurrentAltitudeFeet,
    double CurrentSpeedKnots,
    double CurrentHeadingDegrees,
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

public record PositionBreadcrumbDto(
    long Id,
    double Latitude,
    double Longitude,
    double AltitudeFeet,
    double SpeedKnots,
    double HeadingDegrees,
    long TickNumber,
    DateTime RecordedAt);
