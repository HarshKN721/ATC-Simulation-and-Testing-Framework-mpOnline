using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Common;

public static class MappingExtensions
{
    public static SimulationResponse ToResponse(SimulationRun run)
    {
        return new SimulationResponse(
            run.Id,
            run.Name,
            run.Status,
            run.Status.ToString(),
            run.StartTime,
            run.EndTime,
            run.TickIntervalMs,
            run.CurrentTick,
            run.ElapsedSeconds,
            run.CreatedAt,
            run.AircraftList?.Count ?? 0,
            run.ConflictEvents?.Count(c => !c.IsResolved) ?? 0);
    }

    public static AircraftRadarDto ToRadarDto(Aircraft a)
    {
        return new AircraftRadarDto(
            a.Id,
            a.Callsign,
            a.ModelType,
            a.CurrentLatitude,
            a.CurrentLongitude,
            a.CurrentAltitudeFeet,
            a.CurrentSpeedKnots,
            a.CurrentHeadingDegrees,
            a.TargetAltitudeFeet,
            a.TargetSpeedKnots,
            a.TargetHeadingDegrees,
            a.VerticalSpeedFpm,
            a.Status,
            a.Status.ToString(),
            a.SquawkCode,
            a.AssignedSector,
            a.FlightPlanRoute,
            a.UpdatedAt);
    }

    public static AircraftDetailDto ToDetailDto(Aircraft a)
    {
        return new AircraftDetailDto(
            a.Id,
            a.SimulationRunId,
            a.Callsign,
            a.ModelType,
            a.CurrentLatitude,
            a.CurrentLongitude,
            a.CurrentAltitudeFeet,
            a.CurrentSpeedKnots,
            a.CurrentHeadingDegrees,
            a.TargetAltitudeFeet,
            a.TargetSpeedKnots,
            a.TargetHeadingDegrees,
            a.VerticalSpeedFpm,
            a.Status,
            a.Status.ToString(),
            a.SquawkCode,
            a.AssignedSector,
            a.FlightPlanRoute,
            a.UpdatedAt);
    }

    public static ConflictRadarDto ToRadarDto(ConflictEvent c)
    {
        return new ConflictRadarDto(
            c.Id,
            c.PrimaryAircraftId,
            c.PrimaryAircraft?.Callsign ?? $"AC-{c.PrimaryAircraftId}",
            c.SecondaryAircraftId,
            c.SecondaryAircraft?.Callsign ?? $"AC-{c.SecondaryAircraftId}",
            c.ConflictType,
            c.Severity,
            c.Severity.ToString(),
            c.DistanceNauticalMiles,
            c.AltitudeDifferenceFeet,
            c.DetectedAtTick,
            c.CreatedAt);
    }

    public static ConflictDetailDto ToDetailDto(ConflictEvent c)
    {
        return new ConflictDetailDto(
            c.Id,
            c.SimulationRunId,
            c.PrimaryAircraftId,
            c.PrimaryAircraft?.Callsign ?? $"AC-{c.PrimaryAircraftId}",
            c.SecondaryAircraftId,
            c.SecondaryAircraft?.Callsign ?? $"AC-{c.SecondaryAircraftId}",
            c.ConflictType,
            c.Severity,
            c.Severity.ToString(),
            c.DistanceNauticalMiles,
            c.AltitudeDifferenceFeet,
            c.DetectedAtTick,
            c.ResolvedAtTick,
            c.IsResolved,
            c.CreatedAt,
            c.ResolvedAt);
    }

    public static VectorCommandRadarDto ToRadarDto(VectorCommand v)
    {
        return new VectorCommandRadarDto(
            v.Id,
            v.AircraftId,
            v.Aircraft?.Callsign ?? $"AC-{v.AircraftId}",
            v.CommandType,
            v.CommandType.ToString(),
            v.TargetValue,
            v.WaypointTarget,
            v.Status,
            v.IssuedAt);
    }

    public static VectorCommandDetailDto ToDetailDto(VectorCommand v)
    {
        return new VectorCommandDetailDto(
            v.Id,
            v.SimulationRunId,
            v.AircraftId,
            v.Aircraft?.Callsign ?? $"AC-{v.AircraftId}",
            v.CommandType,
            v.CommandType.ToString(),
            v.TargetValue,
            v.WaypointTarget,
            v.IssuedAtTick,
            v.Status,
            v.Status.ToString(),
            v.IssuedAt,
            v.ExecutedAt);
    }

    public static PositionBreadcrumbDto ToBreadcrumbDto(PositionLog p)
    {
        return new PositionBreadcrumbDto(
            p.Id,
            p.Latitude,
            p.Longitude,
            p.AltitudeFeet,
            p.SpeedKnots,
            p.HeadingDegrees,
            p.TickNumber,
            p.RecordedAt);
    }
}
