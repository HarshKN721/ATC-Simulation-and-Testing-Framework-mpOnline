using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/simulations/{simulationId:int}/[controller]")]
public class RadarController : ControllerBase
{
    private readonly AtcDbContext _dbContext;

    public RadarController(AtcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<RadarSnapshotDto>> GetRadarSnapshot(int simulationId, CancellationToken cancellationToken)
    {
        var run = await _dbContext.SimulationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == simulationId, cancellationToken);

        if (run == null)
        {
            return NotFound(new { message = $"Simulation {simulationId} not found." });
        }

        var aircraft = await _dbContext.Aircraft
            .AsNoTracking()
            .Where(a => a.SimulationRunId == simulationId && a.Status != AircraftStatus.Landed)
            .OrderBy(a => a.Callsign)
            .ToListAsync(cancellationToken);

        var activeConflicts = await _dbContext.ConflictEvents
            .AsNoTracking()
            .Include(c => c.PrimaryAircraft)
            .Include(c => c.SecondaryAircraft)
            .Where(c => c.SimulationRunId == simulationId && !c.IsResolved)
            .ToListAsync(cancellationToken);

        var activeCommands = await _dbContext.VectorCommands
            .AsNoTracking()
            .Include(v => v.Aircraft)
            .Where(v => v.SimulationRunId == simulationId && (v.Status == VectorCommandStatus.Pending || v.Status == VectorCommandStatus.Active))
            .OrderByDescending(v => v.IssuedAt)
            .ToListAsync(cancellationToken);

        var snapshot = new RadarSnapshotDto(
            run.Id,
            run.Name,
            run.Status,
            run.Status.ToString(),
            run.CurrentTick,
            run.ElapsedSeconds,
            DateTime.UtcNow,
            aircraft.Select(MappingExtensions.ToRadarDto).ToList(),
            activeConflicts.Select(MappingExtensions.ToRadarDto).ToList(),
            activeCommands.Select(MappingExtensions.ToRadarDto).ToList());

        return Ok(snapshot);
    }
}
