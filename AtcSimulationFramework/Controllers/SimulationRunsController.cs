namespace AtcSimulationFramework.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Data;
using AtcSimulationFramework.Models;
using AtcSimulationFramework.Services;

[ApiController]
[Route("api/[controller]")]
public class SimulationRunsController : ControllerBase
{
    private readonly AtcDbContext _db;
    private readonly ConflictDetector _conflictDetector;

    public SimulationRunsController(AtcDbContext db, ConflictDetector conflictDetector)
    {
        _db = db;
        _conflictDetector = conflictDetector;
    }

    // -----------------------------------------------------------------
    //  POST /api/simulationruns
    //  Creates a new run with Status = "Running".
    // -----------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> CreateRun(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateRunRequest? request)
    {
        var userId = await ResolveCreatedByUserIdAsync(request?.CreatedByUserId ?? 0);
        if (userId is null)
            return BadRequest(new { error = "No AppUser exists. Seed ATC_DB from ATC_Database_Schema.sql." });

        var now = DateTime.UtcNow;
        var running = await _db.SimulationRuns
            .Where(r => r.Status == "Running")
            .ToListAsync();
        foreach (var existing in running)
        {
            existing.Status  = "Aborted";
            existing.EndTime = now;
        }

        var run = new SimulationRun
        {
            StartTime       = now,
            Status          = "Running",
            CreatedByUserId = userId.Value
        };

        _db.SimulationRuns.Add(run);
        await _db.SaveChangesAsync();

        var newAircraft = new List<Aircraft>
        {
            new Aircraft { Callsign = "AA123", Icao24 = "A00001", RunId = run.RunId },
            new Aircraft { Callsign = "BA456", Icao24 = "B00002", RunId = run.RunId },
            new Aircraft { Callsign = "UA789", Icao24 = "C00003", RunId = run.RunId }
        };
        _db.Aircraft.AddRange(newAircraft);
        await _db.SaveChangesAsync();

        var initialPositions = new List<PositionLog>
        {
            new PositionLog { AircraftId = newAircraft[0].AircraftId, Timestamp = now, Latitude = 40.7128m, Longitude = -74.0060m, AltitudeFt = 30000, HeadingDeg = 90, SpeedKts = 450 },
            new PositionLog { AircraftId = newAircraft[1].AircraftId, Timestamp = now, Latitude = 40.7200m, Longitude = -74.0100m, AltitudeFt = 30000, HeadingDeg = 270, SpeedKts = 400 },
            new PositionLog { AircraftId = newAircraft[2].AircraftId, Timestamp = now, Latitude = 40.8000m, Longitude = -73.9000m, AltitudeFt = 32000, HeadingDeg = 180, SpeedKts = 420 }
        };
        _db.PositionLogs.AddRange(initialPositions);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRun), new { id = run.RunId }, run);
    }

    // -----------------------------------------------------------------
    //  GET /api/simulationruns
    //  Lists all simulation runs.
    // -----------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetAllRuns()
    {
        var runs = await _db.SimulationRuns
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

        return Ok(runs);
    }

    // -----------------------------------------------------------------
    //  GET /api/simulationruns/{id}
    //  Returns a single run by ID.
    // -----------------------------------------------------------------
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRun(int id)
    {
        var run = await FindRunAsync(id);
        if (run is null)
            return NotFound();

        return Ok(run);
    }

    // -----------------------------------------------------------------
    //  GET /api/simulationruns/{id}/aircraft
    //  Returns aircraft for the run enriched with latest position data.
    //  JSON shape matches the dashboard expectation:
    //    { callsign, latitude, longitude, altitude, heading, speed, icao24 }
    // -----------------------------------------------------------------
    [HttpGet("{id}/aircraft")]
    public async Task<IActionResult> GetAircraft(int id)
    {
        if (!await RunExistsAsync(id))
            return NotFound();

        var aircraftWithPositions = await GetLatestPositionsAsync(id);
        return Ok(aircraftWithPositions);
    }

    // -----------------------------------------------------------------
    //  GET /api/simulationruns/{id}/conflicts
    //  Detects conflicts among the run's aircraft using their latest
    //  positions.  Read-only — does not persist to the database.
    // -----------------------------------------------------------------
    [HttpGet("{id}/conflicts")]
    public async Task<IActionResult> GetConflicts(int id)
    {
        if (!await RunExistsAsync(id))
            return NotFound();

        var aircraft = await _db.GetAircraftWithLatestPositionsAsync(id);
        var byId = aircraft.ToDictionary(a => a.AircraftId);
        var conflicts = _conflictDetector.DetectConflicts(aircraft);

        return Ok(conflicts.Select(c => new
        {
            c.ConflictId,
            c.RunId,
            c.AircraftAId,
            c.AircraftBId,
            callsignA = byId.TryGetValue(c.AircraftAId, out var a) ? a.Callsign : null,
            callsignB = byId.TryGetValue(c.AircraftBId, out var b) ? b.Callsign : null,
            c.DetectedAt,
            c.HorizontalDistNm,
            c.VerticalDistFt,
            c.ResolvedAt,
            c.ResolutionAction
        }));
    }

    // -----------------------------------------------------------------
    //  GET /api/simulationruns/{id}/commands
    //  Vector commands issued for this run (via conflict → command).
    // -----------------------------------------------------------------
    [HttpGet("{id}/commands")]
    public async Task<IActionResult> GetCommands(int id)
    {
        if (!await RunExistsAsync(id))
            return NotFound();

        var commands = await (
                from v in _db.VectorCommands.AsNoTracking()
                join c in _db.ConflictEvents.AsNoTracking() on v.ConflictId equals c.ConflictId
                join a in _db.Aircraft.AsNoTracking() on v.AircraftId equals a.AircraftId
                where c.RunId == id
                orderby v.IssuedAt descending
                select new
                {
                    v.CommandId,
                    v.ConflictId,
                    v.AircraftId,
                    callsign = a.Callsign,
                    v.CommandType,
                    v.Value,
                    v.IssuedAt
                })
            .Take(50)
            .ToListAsync();

        return Ok(commands);
    }

    // -----------------------------------------------------------------
    //  PATCH /api/simulationruns/{id}/stop
    //  Sets Status = "Completed" and EndTime = now.
    // -----------------------------------------------------------------
    [HttpPatch("{id}/stop")]
    public async Task<IActionResult> StopRun(int id)
    {
        var run = await FindRunAsync(id);
        if (run is null)
            return NotFound();

        if (run.Status != "Running")
            return BadRequest(new { error = $"Run is already '{run.Status}', cannot stop." });

        run.Status  = "Completed";
        run.EndTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(run);
    }

    // =================================================================
    //  Reusable helpers — each does one thing
    // =================================================================

    private async Task<int?> ResolveCreatedByUserIdAsync(int requestedId)
    {
        if (requestedId > 0 && await _db.AppUsers.AnyAsync(u => u.UserId == requestedId))
            return requestedId;

        return await _db.AppUsers
            .OrderBy(u => u.UserId)
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync();
    }

    /// <summary>Returns the SimulationRun with the given ID, or null.</summary>
    private async Task<SimulationRun?> FindRunAsync(int id)
    {
        return await _db.SimulationRuns.FindAsync(id);
    }

    /// <summary>Quick existence check without loading the full entity.</summary>
    private async Task<bool> RunExistsAsync(int id)
    {
        return await _db.SimulationRuns.AnyAsync(r => r.RunId == id);
    }

    /// <summary>
    /// Returns a flat projection of each aircraft joined with its most
    /// recent <see cref="PositionLog"/> row.  The JSON shape matches the
    /// frontend dashboard expectation.
    /// Used by both the aircraft endpoint and (indirectly) the conflicts
    /// endpoint, so the query lives in one place.
    /// </summary>
    private async Task<List<object>> GetLatestPositionsAsync(int runId)
    {
        var aircraft = await _db.GetAircraftWithLatestPositionsAsync(runId);

        return aircraft.Select(ac => (object)new
        {
            aircraftId = ac.AircraftId,
            callsign   = ac.Callsign,
            icao24     = ac.Icao24,
            runId      = ac.RunId,
            latitude   = ac.Latitude,
            longitude  = ac.Longitude,
            altitude   = ac.AltitudeFt,
            heading    = ac.HeadingDeg,
            speed      = ac.SpeedKts
        }).ToList();
    }
}

// -----------------------------------------------------------------
//  Minimal request DTO — kept in the same file for discoverability.
//  Move to a Models/Dtos/ folder if the project grows.
// -----------------------------------------------------------------
public class CreateRunRequest
{
    public int CreatedByUserId { get; set; } = 1;
}
