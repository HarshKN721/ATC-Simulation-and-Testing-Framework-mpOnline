using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/simulations/{simulationId:int}/conflicts")]
[Route("api/simulations/{simulationId:int}/[controller]")]
public class ConflictController : ControllerBase
{
    private readonly AtcDbContext _dbContext;

    public ConflictController(AtcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConflictDetailDto>>> GetConflicts(
        int simulationId,
        [FromQuery] bool? isResolved,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ConflictEvents
            .AsNoTracking()
            .Include(c => c.PrimaryAircraft)
            .Include(c => c.SecondaryAircraft)
            .Where(c => c.SimulationRunId == simulationId);

        if (isResolved.HasValue)
        {
            query = query.Where(c => c.IsResolved == isResolved.Value);
        }

        var list = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(MappingExtensions.ToDetailDto));
    }

    [HttpGet("{conflictId:int}")]
    public async Task<ActionResult<ConflictDetailDto>> GetConflictById(
        int simulationId,
        int conflictId,
        CancellationToken cancellationToken)
    {
        var conflict = await _dbContext.ConflictEvents
            .AsNoTracking()
            .Include(c => c.PrimaryAircraft)
            .Include(c => c.SecondaryAircraft)
            .FirstOrDefaultAsync(c => c.SimulationRunId == simulationId && c.Id == conflictId, cancellationToken);

        if (conflict == null)
        {
            return NotFound(new { message = $"Conflict {conflictId} not found in simulation {simulationId}." });
        }

        return Ok(MappingExtensions.ToDetailDto(conflict));
    }
}
