namespace AtcSimulationFramework.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using AtcSimulationFramework.Data;
using AtcSimulationFramework.Models;

public interface IControllerAgent
{
}

public class AutomatedControllerAgent : IControllerAgent
{
}

public class SimulationTickService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationTickService> _logger;

    public SimulationTickService(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulationTickService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SimulationTickService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during simulation tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("SimulationTickService stopped.");
    }

    /// <summary>
    /// Executes a single simulation tick: loads the active run's aircraft,
    /// advances every aircraft's position by one tick, records position
    /// logs, and persists changes to the database.
    /// </summary>
    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtcDbContext>();

        var activeRun = await FindActiveRunAsync(db, ct);
        if (activeRun is null)
            return; // nothing to simulate right now

        var aircraft = await LoadAircraftForRunAsync(db, activeRun.RunId, ct);
        if (aircraft.Count == 0)
            return;

        var now = DateTime.UtcNow;
        const double deltaTime = 1.0; // seconds per tick

        foreach (var ac in aircraft)
        {
            AircraftKinematics.UpdatePosition(ac, deltaTime);
            RecordPositionLog(db, ac, now);
        }

        await db.SaveChangesAsync(ct);
    }

    // -----------------------------------------------------------------
    //  Small helper methods – each does one thing
    // -----------------------------------------------------------------

    /// <summary>Returns the first SimulationRun with Status "Running", or null.</summary>
    private static async Task<SimulationRun?> FindActiveRunAsync(
        AtcDbContext db, CancellationToken ct)
    {
        return await db.SimulationRuns
            .FirstOrDefaultAsync(r => r.Status == "Running", ct);
    }

    /// <summary>Returns all Aircraft belonging to the specified run.</summary>
    private static async Task<List<Aircraft>> LoadAircraftForRunAsync(
        AtcDbContext db, int runId, CancellationToken ct)
    {
        return await db.Aircraft
            .Where(a => a.RunId == runId)
            .ToListAsync(ct);
    }

    /// <summary>Appends a <see cref="PositionLog"/> snapshot for one aircraft.</summary>
    private static void RecordPositionLog(AtcDbContext db, Aircraft ac, DateTime timestamp)
    {
        db.PositionLogs.Add(new PositionLog
        {
            AircraftId = ac.AircraftId,
            Timestamp  = timestamp,
            Latitude   = (decimal)ac.Latitude,
            Longitude  = (decimal)ac.Longitude,
            AltitudeFt = ac.AltitudeFt,
            HeadingDeg = (decimal)ac.HeadingDeg,
            SpeedKts   = ac.SpeedKts
        });
    }
}
