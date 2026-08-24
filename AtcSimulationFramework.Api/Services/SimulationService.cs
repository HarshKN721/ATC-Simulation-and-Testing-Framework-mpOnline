using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Entities;
using AtcSimulationFramework.Api.Interfaces;

namespace AtcSimulationFramework.Api.Services;

public class SimulationService : ISimulationService
{
    private readonly AtcDbContext _dbContext;
    private readonly IConflictDetectionService _conflictDetectionService;
    private readonly IControllerAgent _controllerAgent;

    public SimulationService(
        AtcDbContext dbContext,
        IConflictDetectionService conflictDetectionService,
        IControllerAgent controllerAgent)
    {
        _dbContext = dbContext;
        _conflictDetectionService = conflictDetectionService;
        _controllerAgent = controllerAgent;
    }

    public async Task<SimulationRun> StartSimulationRunAsync(CancellationToken cancellationToken = default)
    {
        var run = new SimulationRun
        {
            Status = "Running",
            StartTime = DateTime.UtcNow
        };

        _dbContext.SimulationRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Seed initial aircraft positioning into a heading conflict course
        var aircraftA = new Aircraft
        {
            SimulationRunId = run.Id,
            Name = "AircraftA",
            Latitude = 23.2500,
            Longitude = 77.4100,
            Altitude = 30000.0,
            Heading = 90.0, // Moving East
            Speed = 450.0
        };

        var aircraftB = new Aircraft
        {
            SimulationRunId = run.Id,
            Name = "AircraftB",
            Latitude = 23.2500,
            Longitude = 77.4200,
            Altitude = 30000.0,
            Heading = 270.0, // Moving West towards AircraftA
            Speed = 450.0
        };

        _dbContext.Aircraft.AddRange(aircraftA, aircraftB);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return run;
    }

    public async Task<bool> StopSimulationRunAsync(int id, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SimulationRuns.FindAsync(new object[] { id }, cancellationToken);
        if (run == null)
        {
            return false;
        }

        if (run.Status != "Stopped")
        {
            run.Status = "Stopped";
            run.EndTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<SimulationRun?> GetSimulationRunAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SimulationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<SimulationRun>> GetActiveSimulationRunsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SimulationRuns
            .Include(s => s.Aircraft)
            .Include(s => s.ConflictEvents)
            .Where(s => s.Status == "Running")
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Aircraft>> GetAircraftForRunAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Aircraft
            .AsNoTracking()
            .Where(a => a.SimulationRunId == id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConflictEvent>> GetConflictsForRunAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConflictEvents
            .AsNoTracking()
            .Include(c => c.AircraftA)
            .Include(c => c.AircraftB)
            .Where(c => c.SimulationRunId == id)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task AdvanceSimulationRunAsync(int simulationRunId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.SimulationRuns
            .Include(s => s.Aircraft)
            .Include(s => s.ConflictEvents)
            .FirstOrDefaultAsync(s => s.Id == simulationRunId, cancellationToken);

        if (run == null || run.Status != "Running")
        {
            return;
        }

        // 1. Advance aircraft positions based on current speed and heading
        foreach (var aircraft in run.Aircraft)
        {
            double headingRad = aircraft.Heading * Math.PI / 180.0;
            double stepMultiplier = 0.00005; // Position step per tick
            
            aircraft.Latitude += aircraft.Speed * stepMultiplier * Math.Cos(headingRad);
            aircraft.Longitude += aircraft.Speed * stepMultiplier * Math.Sin(headingRad);

            // Log position
            _dbContext.PositionLogs.Add(new PositionLog
            {
                SimulationRunId = run.Id,
                AircraftId = aircraft.Id,
                Latitude = aircraft.Latitude,
                Longitude = aircraft.Longitude,
                Altitude = aircraft.Altitude,
                Heading = aircraft.Heading,
                Speed = aircraft.Speed,
                Timestamp = DateTime.UtcNow
            });
        }

        // 2. Detect conflicts
        var detectedConflicts = _conflictDetectionService.DetectConflicts(run).ToList();

        foreach (var conflict in detectedConflicts)
        {
            _dbContext.ConflictEvents.Add(conflict);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 3. Invoke controller agent to resolve conflict
            var command = _controllerAgent.ResolveConflict(conflict);
            command.ConflictEventId = conflict.Id;

            _dbContext.VectorCommands.Add(command);

            // 4. Apply vector command to target aircraft
            var targetAircraft = run.Aircraft.FirstOrDefault(a => a.Id == command.AircraftId);
            if (targetAircraft != null)
            {
                targetAircraft.Heading = command.NewHeading;
                command.IsExecuted = true;
                conflict.IsResolved = true;
                conflict.ResolutionTimestamp = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
