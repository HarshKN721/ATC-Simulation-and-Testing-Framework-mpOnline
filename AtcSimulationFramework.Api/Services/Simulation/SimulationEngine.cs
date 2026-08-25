using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Models;
using AtcSimulationFramework.Api.Services.ConflictDetection;
using AtcSimulationFramework.Api.Services.Kinematics;
using AtcSimulationFramework.Api.Services.VectorCommands;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Services.Simulation;

public class SimulationEngine : ISimulationEngine
{
    private readonly AtcDbContext _dbContext;
    private readonly IKinematicsEngine _kinematicsEngine;
    private readonly IConflictDetectionService _conflictDetectionService;
    private readonly IVectorCommandService _vectorCommandService;
    private readonly ILogger<SimulationEngine> _logger;

    public SimulationEngine(
        AtcDbContext dbContext,
        IKinematicsEngine kinematicsEngine,
        IConflictDetectionService conflictDetectionService,
        IVectorCommandService vectorCommandService,
        ILogger<SimulationEngine> logger)
    {
        _dbContext = dbContext;
        _kinematicsEngine = kinematicsEngine;
        _conflictDetectionService = conflictDetectionService;
        _vectorCommandService = vectorCommandService;
        _logger = logger;
    }

    public async Task ExecuteSimulationTickAsync(
        int simulationRunId,
        double dtSeconds,
        CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SimulationRuns
            .Include(r => r.AircraftList)
            .Include(r => r.VectorCommands)
            .FirstOrDefaultAsync(r => r.Id == simulationRunId, cancellationToken);

        if (run == null || run.Status != SimulationStatus.Running)
        {
            return;
        }

        var nextTick = run.CurrentTick + 1;
        var aircraftList = run.AircraftList.ToList();

        // 1. Process Vector Commands
        await _vectorCommandService.ProcessPendingCommandsAsync(run, aircraftList, nextTick, cancellationToken);

        // 2. Advance Aircraft Physics
        AdvanceAllAircraft(aircraftList, dtSeconds);

        // 3. Evaluate and detect conflicts
        await _conflictDetectionService.ProcessConflictsAsync(run, aircraftList, nextTick, cancellationToken);

        // 4. Verify vector command completions
        _vectorCommandService.CheckCommandCompletions(aircraftList, run.VectorCommands);

        // 5. Append Position Logs for active radar trails
        LogPositions(run.Id, aircraftList, nextTick);

        // 6. Update Run Metrics
        AdvanceRunMetrics(run, dtSeconds);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SimulationRun> CreateSimulationAsync(
        string name,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var run = new SimulationRun
        {
            Name = name,
            Status = SimulationStatus.Created,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            TickIntervalMs = 1000,
            CurrentTick = 0,
            ElapsedSeconds = 0.0
        };

        _dbContext.SimulationRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task<bool> StartSimulationAsync(
        int simulationRunId,
        CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SimulationRuns.FindAsync([simulationRunId], cancellationToken);
        if (run == null)
        {
            return false;
        }

        run.Status = SimulationStatus.Running;
        run.StartTime ??= DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> PauseSimulationAsync(
        int simulationRunId,
        CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SimulationRuns.FindAsync([simulationRunId], cancellationToken);
        if (run == null)
        {
            return false;
        }

        run.Status = SimulationStatus.Paused;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ResetSimulationAsync(
        int simulationRunId,
        CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SimulationRuns
            .Include(r => r.AircraftList)
            .Include(r => r.PositionLogs)
            .Include(r => r.ConflictEvents)
            .Include(r => r.VectorCommands)
            .FirstOrDefaultAsync(r => r.Id == simulationRunId, cancellationToken);

        if (run == null)
        {
            return false;
        }

        run.Status = SimulationStatus.Created;
        run.CurrentTick = 0;
        run.ElapsedSeconds = 0.0;
        run.StartTime = null;
        run.EndTime = null;

        _dbContext.PositionLogs.RemoveRange(run.PositionLogs);
        _dbContext.ConflictEvents.RemoveRange(run.ConflictEvents);
        _dbContext.VectorCommands.RemoveRange(run.VectorCommands);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void AdvanceAllAircraft(IEnumerable<Aircraft> aircraftList, double dtSeconds)
    {
        foreach (var aircraft in aircraftList)
        {
            _kinematicsEngine.AdvanceAircraftPhysics(aircraft, dtSeconds);
        }
    }

    private void LogPositions(int runId, IEnumerable<Aircraft> aircraftList, long tickNumber)
    {
        var now = DateTime.UtcNow;
        var logs = aircraftList.Select(a => CreatePositionLog(runId, a, tickNumber, now));
        _dbContext.PositionLogs.AddRange(logs);
    }

    private static PositionLog CreatePositionLog(int runId, Aircraft a, long tickNumber, DateTime now)
    {
        return new PositionLog
        {
            SimulationRunId = runId,
            AircraftId = a.Id,
            Latitude = a.CurrentLatitude,
            Longitude = a.CurrentLongitude,
            AltitudeFeet = a.CurrentAltitudeFeet,
            SpeedKnots = a.CurrentSpeedKnots,
            HeadingDegrees = a.CurrentHeadingDegrees,
            VerticalSpeedFpm = a.VerticalSpeedFpm,
            TickNumber = tickNumber,
            RecordedAt = now
        };
    }

    private static void AdvanceRunMetrics(SimulationRun run, double dtSeconds)
    {
        run.CurrentTick++;
        run.ElapsedSeconds += dtSeconds;
    }
}
