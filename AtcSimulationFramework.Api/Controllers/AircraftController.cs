using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/simulations/{simulationId:int}/[controller]")]
public class AircraftController : ControllerBase
{
    private readonly AtcDbContext _dbContext;

    public AircraftController(AtcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AircraftDetailDto>>> GetAllAircraft(
        int simulationId,
        CancellationToken cancellationToken)
    {
        var aircraftList = await _dbContext.Aircraft
            .AsNoTracking()
            .Where(a => a.SimulationRunId == simulationId)
            .OrderBy(a => a.Callsign)
            .ToListAsync(cancellationToken);

        return Ok(aircraftList.Select(MappingExtensions.ToDetailDto));
    }

    [HttpGet("{aircraftId:int}")]
    public async Task<ActionResult<AircraftDetailDto>> GetAircraftById(
        int simulationId,
        int aircraftId,
        CancellationToken cancellationToken)
    {
        var aircraft = await _dbContext.Aircraft
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.SimulationRunId == simulationId && a.Id == aircraftId, cancellationToken);

        if (aircraft == null)
        {
            return NotFound(new { message = $"Aircraft {aircraftId} not found in simulation {simulationId}." });
        }

        return Ok(MappingExtensions.ToDetailDto(aircraft));
    }

    [HttpGet("{aircraftId:int}/trail")]
    public async Task<ActionResult<IEnumerable<PositionBreadcrumbDto>>> GetPositionTrail(
        int simulationId,
        int aircraftId,
        [FromQuery] int limit = 60,
        CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.PositionLogs
            .AsNoTracking()
            .Where(p => p.SimulationRunId == simulationId && p.AircraftId == aircraftId)
            .OrderByDescending(p => p.TickNumber)
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken);

        logs.Reverse();
        return Ok(logs.Select(MappingExtensions.ToBreadcrumbDto));
    }

    [HttpPost]
    public async Task<ActionResult<AircraftDetailDto>> SpawnAircraft(
        int simulationId,
        [FromBody] CreateAircraftRequest request,
        CancellationToken cancellationToken)
    {
        var runExists = await _dbContext.SimulationRuns.AnyAsync(r => r.Id == simulationId, cancellationToken);
        if (!runExists)
        {
            return NotFound(new { message = $"Simulation {simulationId} not found." });
        }

        var aircraft = new Aircraft
        {
            SimulationRunId = simulationId,
            Callsign = request.Callsign.ToUpperInvariant(),
            ModelType = string.IsNullOrWhiteSpace(request.ModelType) ? "B738" : request.ModelType,
            CurrentLatitude = request.InitialLatitude,
            CurrentLongitude = request.InitialLongitude,
            CurrentAltitudeFeet = request.InitialAltitudeFeet,
            CurrentSpeedKnots = request.InitialSpeedKnots,
            CurrentHeadingDegrees = AviationMath.NormalizeHeading(request.InitialHeadingDegrees),
            TargetAltitudeFeet = request.TargetAltitudeFeet ?? request.InitialAltitudeFeet,
            TargetSpeedKnots = request.TargetSpeedKnots ?? request.InitialSpeedKnots,
            TargetHeadingDegrees = AviationMath.NormalizeHeading(request.TargetHeadingDegrees ?? request.InitialHeadingDegrees),
            VerticalSpeedFpm = 0.0,
            Status = AircraftStatus.Airborne,
            SquawkCode = request.SquawkCode ?? "1200",
            AssignedSector = request.AssignedSector ?? "Sector-A",
            FlightPlanRoute = request.FlightPlanRoute ?? string.Empty,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Aircraft.Add(aircraft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetAircraftById),
            new { simulationId, aircraftId = aircraft.Id },
            MappingExtensions.ToDetailDto(aircraft));
    }

    [HttpDelete("{aircraftId:int}")]
    public async Task<IActionResult> RemoveAircraft(
        int simulationId,
        int aircraftId,
        CancellationToken cancellationToken)
    {
        var aircraft = await _dbContext.Aircraft
            .FirstOrDefaultAsync(a => a.SimulationRunId == simulationId && a.Id == aircraftId, cancellationToken);

        if (aircraft == null)
        {
            return NotFound(new { message = $"Aircraft {aircraftId} not found." });
        }

        _dbContext.Aircraft.Remove(aircraft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"Aircraft {aircraft.Callsign} removed." });
    }
}
