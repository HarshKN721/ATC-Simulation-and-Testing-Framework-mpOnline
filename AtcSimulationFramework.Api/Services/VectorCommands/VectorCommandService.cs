using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Services.VectorCommands;

public class VectorCommandService : IVectorCommandService
{
    private readonly AtcDbContext _dbContext;

    public VectorCommandService(AtcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ProcessPendingCommandsAsync(
        SimulationRun run,
        IList<Aircraft> aircraftList,
        long currentTick,
        CancellationToken cancellationToken = default)
    {
        var pendingCommands = await _dbContext.VectorCommands
            .Where(c => c.SimulationRunId == run.Id && c.Status == VectorCommandStatus.Pending)
            .OrderBy(c => c.IssuedAt)
            .ToListAsync(cancellationToken);

        if (!pendingCommands.Any())
        {
            return;
        }

        var aircraftMap = aircraftList.ToDictionary(a => a.Id);

        foreach (var cmd in pendingCommands)
        {
            if (aircraftMap.TryGetValue(cmd.AircraftId, out var aircraft))
            {
                ApplyCommandToAircraft(aircraft, cmd);
                cmd.Status = VectorCommandStatus.Active;
                cmd.ExecutedAt = DateTime.UtcNow;
            }
            else
            {
                cmd.Status = VectorCommandStatus.Cancelled;
            }
        }
    }

    public void CheckCommandCompletions(
        IList<Aircraft> aircraftList,
        IEnumerable<VectorCommand> activeCommands)
    {
        var aircraftMap = aircraftList.ToDictionary(a => a.Id);

        foreach (var cmd in activeCommands.Where(c => c.Status == VectorCommandStatus.Active))
        {
            if (!aircraftMap.TryGetValue(cmd.AircraftId, out var aircraft))
            {
                continue;
            }

            if (IsCommandSatisfied(aircraft, cmd))
            {
                cmd.Status = VectorCommandStatus.Completed;
            }
        }
    }

    private static void ApplyCommandToAircraft(Aircraft aircraft, VectorCommand cmd)
    {
        switch (cmd.CommandType)
        {
            case VectorCommandType.Heading:
                ApplyHeadingCommand(aircraft, cmd.TargetValue);
                break;
            case VectorCommandType.Altitude:
                ApplyAltitudeCommand(aircraft, cmd.TargetValue);
                break;
            case VectorCommandType.Speed:
                ApplySpeedCommand(aircraft, cmd.TargetValue);
                break;
            case VectorCommandType.Squawk:
                ApplySquawkCommand(aircraft, cmd.TargetValue);
                break;
            case VectorCommandType.DirectToWaypoint:
                ApplyWaypointCommand(aircraft, cmd.WaypointTarget);
                break;
        }
    }

    private static void ApplyHeadingCommand(Aircraft aircraft, double targetHeading)
    {
        aircraft.TargetHeadingDegrees = AviationMath.NormalizeHeading(targetHeading);
    }

    private static void ApplyAltitudeCommand(Aircraft aircraft, double targetAltitude)
    {
        aircraft.TargetAltitudeFeet = Math.Max(0, targetAltitude);
        var isClimbing = aircraft.TargetAltitudeFeet > aircraft.CurrentAltitudeFeet;
        aircraft.VerticalSpeedFpm = isClimbing
            ? AviationMath.StandardClimbRateFpm
            : AviationMath.StandardDescentRateFpm;
    }

    private static void ApplySpeedCommand(Aircraft aircraft, double targetSpeed)
    {
        aircraft.TargetSpeedKnots = Math.Clamp(targetSpeed, 60.0, 600.0);
    }

    private static void ApplySquawkCommand(Aircraft aircraft, double squawkValue)
    {
        var code = (int)squawkValue;
        aircraft.SquawkCode = code.ToString("D4");
    }

    private static void ApplyWaypointCommand(Aircraft aircraft, string? waypoint)
    {
        if (!string.IsNullOrWhiteSpace(waypoint))
        {
            aircraft.FlightPlanRoute = waypoint;
        }
    }

    private static bool IsCommandSatisfied(Aircraft aircraft, VectorCommand cmd)
    {
        return cmd.CommandType switch
        {
            VectorCommandType.Heading => Math.Abs(AviationMath.CalculateHeadingDifference(aircraft.CurrentHeadingDegrees, cmd.TargetValue)) < 1.5,
            VectorCommandType.Altitude => Math.Abs(aircraft.CurrentAltitudeFeet - cmd.TargetValue) < 20.0,
            VectorCommandType.Speed => Math.Abs(aircraft.CurrentSpeedKnots - cmd.TargetValue) < 1.0,
            VectorCommandType.Squawk => true,
            VectorCommandType.DirectToWaypoint => true,
            _ => false
        };
    }
}
