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

        // ---------------------------------------------------------
        //  10 aircraft — Indian airlines flying routes across India.
        //  Real ICAO callsigns: AIC (Air India), IGO (IndiGo),
        //  SEJ (SpiceJet), VTI (Vistara), QP (Akasa), AXB (AI Express).
        // ---------------------------------------------------------
        var newAircraft = new List<Aircraft>
        {
            new Aircraft { Callsign = "AIC101", Icao24 = "800A01", RunId = run.RunId }, // Air India  DEL → BOM
            new Aircraft { Callsign = "IGO202", Icao24 = "800B02", RunId = run.RunId }, // IndiGo     BOM → DEL  (converges with AIC101)
            new Aircraft { Callsign = "SEJ305", Icao24 = "800C03", RunId = run.RunId }, // SpiceJet   BLR → MAA
            new Aircraft { Callsign = "VTI847", Icao24 = "800D04", RunId = run.RunId }, // Vistara    HYD → CCU
            new Aircraft { Callsign = "AIC412", Icao24 = "800E05", RunId = run.RunId }, // Air India  MAA → BOM
            new Aircraft { Callsign = "IGO655", Icao24 = "800F06", RunId = run.RunId }, // IndiGo     CCU → DEL
            new Aircraft { Callsign = "SEJ118", Icao24 = "800A07", RunId = run.RunId }, // SpiceJet   JAI → BLR  (converges with AIC789)
            new Aircraft { Callsign = "AIC789", Icao24 = "800B08", RunId = run.RunId }, // Air India  AMD → HYD  (converges with SEJ118)
            new Aircraft { Callsign = "AXB923", Icao24 = "800C09", RunId = run.RunId }, // AI Express GOA → MAA
            new Aircraft { Callsign = "QPA301", Icao24 = "800D10", RunId = run.RunId }, // Akasa Air  LKO → BOM
        };
        _db.Aircraft.AddRange(newAircraft);
        await _db.SaveChangesAsync();

        // Routes across India — DEL, BOM, BLR, MAA, CCU, HYD, JAI, AMD, GOA, LKO.
        // Pairs (0,1) and (6,7) have converging headings at same altitude
        // to guarantee conflicts within the first ~30 seconds.
        var initialPositions = new List<PositionLog>
        {
            //                                                                         lat          lon         alt    hdg   spd
            new PositionLog { AircraftId = newAircraft[0].AircraftId, Timestamp = now, Latitude = 28.5600m, Longitude = 77.1000m, AltitudeFt = 35000, HeadingDeg = 225, SpeedKts = 460 }, // AIC101: Delhi area, heading SW → Mumbai
            new PositionLog { AircraftId = newAircraft[1].AircraftId, Timestamp = now, Latitude = 19.9000m, Longitude = 73.8000m, AltitudeFt = 35000, HeadingDeg =  30, SpeedKts = 450 }, // IGO202: near Nashik, heading NE → Delhi (converging!)
            new PositionLog { AircraftId = newAircraft[2].AircraftId, Timestamp = now, Latitude = 13.2000m, Longitude = 77.7000m, AltitudeFt = 32000, HeadingDeg =  80, SpeedKts = 420 }, // SEJ305: Bangalore, heading E → Chennai
            new PositionLog { AircraftId = newAircraft[3].AircraftId, Timestamp = now, Latitude = 17.2400m, Longitude = 78.4300m, AltitudeFt = 36000, HeadingDeg =  55, SpeedKts = 470 }, // VTI847: Hyderabad, heading NE → Kolkata
            new PositionLog { AircraftId = newAircraft[4].AircraftId, Timestamp = now, Latitude = 13.0000m, Longitude = 80.1700m, AltitudeFt = 34000, HeadingDeg = 295, SpeedKts = 440 }, // AIC412: Chennai, heading WNW → Mumbai
            new PositionLog { AircraftId = newAircraft[5].AircraftId, Timestamp = now, Latitude = 22.6500m, Longitude = 88.4500m, AltitudeFt = 33000, HeadingDeg = 285, SpeedKts = 455 }, // IGO655: Kolkata, heading W → Delhi
            new PositionLog { AircraftId = newAircraft[6].AircraftId, Timestamp = now, Latitude = 26.8200m, Longitude = 75.8100m, AltitudeFt = 31000, HeadingDeg = 175, SpeedKts = 430 }, // SEJ118: Jaipur, heading S → Bangalore (converging!)
            new PositionLog { AircraftId = newAircraft[7].AircraftId, Timestamp = now, Latitude = 23.0200m, Longitude = 72.5700m, AltitudeFt = 31000, HeadingDeg = 140, SpeedKts = 440 }, // AIC789: Ahmedabad, heading SE → Hyderabad (converging!)
            new PositionLog { AircraftId = newAircraft[8].AircraftId, Timestamp = now, Latitude = 15.3800m, Longitude = 73.8300m, AltitudeFt = 29000, HeadingDeg = 105, SpeedKts = 400 }, // AXB923: Goa, heading ESE → Chennai
            new PositionLog { AircraftId = newAircraft[9].AircraftId, Timestamp = now, Latitude = 26.7600m, Longitude = 80.8900m, AltitudeFt = 30000, HeadingDeg = 230, SpeedKts = 410 }, // QPA301: Lucknow, heading SW → Mumbai
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
