using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/simulations/{simulationId:int}/commands")]
[Route("api/simulations/{simulationId:int}/[controller]")]
public class VectorCommandController : ControllerBase
{
    private readonly AtcDbContext _dbContext;

    public VectorCommandController(AtcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VectorCommandDetailDto>>> GetCommands(
        int simulationId,
        [FromQuery] VectorCommandStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.VectorCommands
            .AsNoTracking()
            .Include(c => c.Aircraft)
            .Where(c => c.SimulationRunId == simulationId);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(MappingExtensions.ToDetailDto));
    }

    [HttpPost]
    public async Task<ActionResult<VectorCommandDetailDto>> IssueCommand(
        int simulationId,
        [FromBody] IssueVectorCommandRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _dbContext.SimulationRuns.FindAsync([simulationId], cancellationToken);
        if (run == null)
        {
            return NotFound(new { message = $"Simulation {simulationId} not found." });
        }

        var aircraft = await _dbContext.Aircraft
            .FirstOrDefaultAsync(a => a.SimulationRunId == simulationId && a.Id == request.AircraftId, cancellationToken);

        if (aircraft == null)
        {
            return NotFound(new { message = $"Aircraft {request.AircraftId} not found in simulation {simulationId}." });
        }

        var command = new VectorCommand
        {
            SimulationRunId = simulationId,
            AircraftId = request.AircraftId,
            IssuedByUserId = request.IssuedByUserId,
            CommandType = request.CommandType,
            TargetValue = request.TargetValue,
            WaypointTarget = request.WaypointTarget,
            IssuedAtTick = run.CurrentTick,
            Status = VectorCommandStatus.Pending,
            IssuedAt = DateTime.UtcNow
        };

        _dbContext.VectorCommands.Add(command);
        await _dbContext.SaveChangesAsync(cancellationToken);

        command.Aircraft = aircraft;
        return CreatedAtAction(
            nameof(GetCommands),
            new { simulationId },
            MappingExtensions.ToDetailDto(command));
    }

    [HttpDelete("{commandId:int}")]
    public async Task<IActionResult> CancelCommand(
        int simulationId,
        int commandId,
        CancellationToken cancellationToken)
    {
        var command = await _dbContext.VectorCommands
            .FirstOrDefaultAsync(c => c.SimulationRunId == simulationId && c.Id == commandId, cancellationToken);

        if (command == null)
        {
            return NotFound(new { message = $"Command {commandId} not found." });
        }

        if (command.Status == VectorCommandStatus.Completed)
        {
            return BadRequest(new { message = "Cannot cancel already completed command." });
        }

        command.Status = VectorCommandStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"Command {commandId} cancelled." });
    }
}
