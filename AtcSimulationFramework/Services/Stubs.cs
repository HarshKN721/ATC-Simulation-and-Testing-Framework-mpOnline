namespace AtcSimulationFramework.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using AtcSimulationFramework.Data;
using AtcSimulationFramework.Models;

// =================================================================
//  IControllerAgent — interface for conflict-resolution strategies
// =================================================================

public interface IControllerAgent
{
    /// <summary>
    /// Examines detected conflicts and returns VectorCommands that
    /// resolve them.  Implementations also mutate the in-memory
    /// <paramref name="aircraft"/> state (heading / altitude) so the
    /// changes take effect on the very next tick.
    /// </summary>
    List<VectorCommand> ResolveConflicts(
        List<ConflictEvent> conflicts,
        List<Aircraft> aircraft);
}

// =================================================================
//  AutomatedControllerAgent — rule-based resolution
//
//  Strategy:
//    1. Heading divergence:  A turns +30° right, B turns −30° left
//    2. Altitude separation: if vertical gap < 500 ft, A climbs
//       +1000 ft, B descends −1000 ft
// =================================================================

public class AutomatedControllerAgent : IControllerAgent
{
    private const double HeadingDeltaDeg = 30.0;
    private const int    AltitudeStepFt  = 1000;
    private const int    AltitudeThresholdFt = 500;

    public List<VectorCommand> ResolveConflicts(
        List<ConflictEvent> conflicts,
        List<Aircraft> aircraft)
    {
        var commands = new List<VectorCommand>();

        if (conflicts.Count == 0 || aircraft.Count < 2)
            return commands;

        // Index aircraft by ID for O(1) lookups
        var byId = IndexAircraftById(aircraft);

        foreach (var conflict in conflicts)
        {
            if (!byId.TryGetValue(conflict.AircraftAId, out var acA) ||
                !byId.TryGetValue(conflict.AircraftBId, out var acB))
                continue;

            commands.AddRange(ResolveSingleConflict(conflict, acA, acB));
        }

        return commands;
    }

    // -----------------------------------------------------------------
    //  Small helper methods – each does one thing
    // -----------------------------------------------------------------

    /// <summary>
    /// Resolves a single conflict between two aircraft by issuing
    /// heading and (optionally) altitude commands.
    /// </summary>
    private static List<VectorCommand> ResolveSingleConflict(
        ConflictEvent conflict, Aircraft acA, Aircraft acB)
    {
        var commands = new List<VectorCommand>();
        var now = DateTime.UtcNow;

        // 1. Heading divergence — always applied
        acA.HeadingDeg = NormalizeHeading(acA.HeadingDeg + HeadingDeltaDeg);
        acB.HeadingDeg = NormalizeHeading(acB.HeadingDeg - HeadingDeltaDeg);

        commands.Add(CreateCommand(conflict.ConflictId, acA.AircraftId, "Heading", (decimal)acA.HeadingDeg, now));
        commands.Add(CreateCommand(conflict.ConflictId, acB.AircraftId, "Heading", (decimal)acB.HeadingDeg, now));

        // 2. Altitude separation — only if vertical gap is critically small.
        //    VectorCommand.Value is DECIMAL(6,2), so altitude is stored as
        //    flight level (feet / 100), which fits the column.
        if (conflict.VerticalDistFt < AltitudeThresholdFt)
        {
            acA.AltitudeFt += AltitudeStepFt;
            acB.AltitudeFt = Math.Max(1000, acB.AltitudeFt - AltitudeStepFt);

            commands.Add(CreateCommand(conflict.ConflictId, acA.AircraftId, "Altitude", acA.AltitudeFt / 100m, now));
            commands.Add(CreateCommand(conflict.ConflictId, acB.AircraftId, "Altitude", acB.AltitudeFt / 100m, now));
        }

        // Mark the conflict as resolved
        conflict.ResolvedAt = now;
        conflict.ResolutionAction = conflict.VerticalDistFt < AltitudeThresholdFt
            ? "Heading divergence + altitude separation"
            : "Heading divergence";

        return commands;
    }

    /// <summary>Wraps heading into the [0, 360) range.</summary>
    private static double NormalizeHeading(double heading)
    {
        return ((heading % 360.0) + 360.0) % 360.0;
    }

    /// <summary>Builds a single <see cref="VectorCommand"/>.</summary>
    private static VectorCommand CreateCommand(
        int conflictId, int aircraftId, string type, decimal value, DateTime issuedAt)
    {
        return new VectorCommand
        {
            ConflictId  = conflictId,
            AircraftId  = aircraftId,
            CommandType = type,
            Value       = value,
            IssuedAt    = issuedAt
        };
    }

    /// <summary>Indexes a list of aircraft by AircraftId for fast lookup.</summary>
    private static Dictionary<int, Aircraft> IndexAircraftById(List<Aircraft> aircraft)
    {
        var dict = new Dictionary<int, Aircraft>(aircraft.Count);
        foreach (var ac in aircraft)
            dict[ac.AircraftId] = ac;
        return dict;
    }
}

// =================================================================
//  SimulationTickService — BackgroundService tick loop
// =================================================================

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
    /// Executes a single simulation tick:
    ///   1. Load active run + aircraft with latest positions
    ///   2. Detect / persist new conflicts, resolve unresolved ones
    ///      (heading/altitude mutations must happen before kinematics
    ///      or they are overwritten on the next hydrate-from-DB)
    ///   3. Advance positions via kinematics
    ///   4. Record new position logs and prune old ones
    /// </summary>
    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<AtcDbContext>();
        var agent = scope.ServiceProvider.GetRequiredService<IControllerAgent>();

        var activeRun = await FindActiveRunAsync(db, ct);
        if (activeRun is null)
            return;

        var aircraft = await db.GetAircraftWithLatestPositionsAsync(activeRun.RunId, ct);
        if (aircraft.Count == 0)
            return;

        var detector = scope.ServiceProvider.GetRequiredService<ConflictDetector>();
        var rawConflicts = detector.DetectConflicts(aircraft);
        var newConflicts = await FilterNewConflictsAsync(db, activeRun.RunId, rawConflicts, ct);

        if (newConflicts.Count > 0)
        {
            db.ConflictEvents.AddRange(newConflicts);
            await db.SaveChangesAsync(ct);
        }

        var unresolved = await db.ConflictEvents
            .Where(c => c.RunId == activeRun.RunId && c.ResolvedAt == null)
            .ToListAsync(ct);

        if (unresolved.Count > 0)
        {
            var commands = agent.ResolveConflicts(unresolved, aircraft);
            if (commands.Count > 0)
            {
                db.VectorCommands.AddRange(commands);
                _logger.LogInformation(
                    "Resolved {ConflictCount} conflict(s), issued {CommandCount} command(s).",
                    unresolved.Count, commands.Count);
            }
        }

        var now = DateTime.UtcNow;
        const double deltaTime = 1.0;

        foreach (var ac in aircraft)
        {
            AircraftKinematics.UpdatePosition(ac, deltaTime);
            RecordPositionLog(db, ac, now);
        }

        await PruneOldPositionLogsAsync(db, aircraft, now, ct);
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

    /// <summary>Appends a <see cref="PositionLog"/> snapshot for one aircraft.</summary>
    private static void RecordPositionLog(AtcDbContext db, Aircraft ac, DateTime timestamp)
    {
        var heading = Math.Round((decimal)ac.HeadingDeg, 2);
        if (heading >= 360m) heading = 0m;

        db.PositionLogs.Add(new PositionLog
        {
            AircraftId = ac.AircraftId,
            Timestamp  = timestamp,
            Latitude   = Math.Clamp(Math.Round((decimal)ac.Latitude, 6), -90m, 90m),
            Longitude  = Math.Clamp(Math.Round((decimal)ac.Longitude, 6), -180m, 180m),
            AltitudeFt = ac.AltitudeFt,
            HeadingDeg = heading,
            SpeedKts   = ac.SpeedKts
        });
    }

    /// <summary>
    /// Drops PositionLog rows older than two minutes so latest-position
    /// queries do not scan an unbounded history each tick.
    /// </summary>
    private static async Task PruneOldPositionLogsAsync(
        AtcDbContext db, List<Aircraft> aircraft, DateTime now, CancellationToken ct)
    {
        var ids = aircraft.Select(a => a.AircraftId).ToList();
        var cutoff = now.AddMinutes(-2);

        await db.PositionLogs
            .Where(p => ids.Contains(p.AircraftId) && p.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Filters out conflicts where the same aircraft pair already has an
    /// unresolved (ResolvedAt == null) ConflictEvent, avoiding duplicate
    /// rows every tick.
    /// </summary>
    private static async Task<List<ConflictEvent>> FilterNewConflictsAsync(
        AtcDbContext db, int runId, List<ConflictEvent> candidates, CancellationToken ct)
    {
        if (candidates.Count == 0)
            return candidates;

        var activeConflicts = await db.ConflictEvents
            .Where(c => c.RunId == runId && c.ResolvedAt == null)
            .ToListAsync(ct);

        var activePairs = BuildActivePairSet(activeConflicts);

        return candidates
            .Where(c => !activePairs.Contains(MakePairKey(c.AircraftAId, c.AircraftBId)))
            .ToList();
    }

    /// <summary>
    /// Builds a HashSet of canonical "min,max" pair keys from existing
    /// unresolved conflicts for fast duplicate checking.
    /// </summary>
    private static HashSet<string> BuildActivePairSet(List<ConflictEvent> activeConflicts)
    {
        var set = new HashSet<string>(activeConflicts.Count);
        foreach (var c in activeConflicts)
            set.Add(MakePairKey(c.AircraftAId, c.AircraftBId));
        return set;
    }

    /// <summary>
    /// Creates a canonical key for an aircraft pair so (A,B) and (B,A)
    /// map to the same string.
    /// </summary>
    private static string MakePairKey(int idA, int idB)
    {
        int lo = Math.Min(idA, idB);
        int hi = Math.Max(idA, idB);
        return $"{lo},{hi}";
    }
}
