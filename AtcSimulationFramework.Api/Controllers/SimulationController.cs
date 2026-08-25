using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Services.Simulation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/simulations")]
[Route("api/simulation")]
public class SimulationController : ControllerBase
{
    private readonly AtcDbContext _dbContext;
    private readonly ISimulationEngine _simulationEngine;

    public SimulationController(AtcDbContext dbContext, ISimulationEngine simulationEngine)
    {
        _dbContext = dbContext;
        _simulationEngine = simulationEngine;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SimulationResponse>>> GetAllSimulations(CancellationToken cancellationToken)
    {
        var runs = await _dbContext.SimulationRuns
            .Include(r => r.AircraftList)
            .Include(r => r.ConflictEvents)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(runs.Select(MappingExtensions.ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SimulationResponse>> GetSimulationById(int id, CancellationToken cancellationToken)
    {
        var run = await _dbContext.SimulationRuns
            .Include(r => r.AircraftList)
            .Include(r => r.ConflictEvents)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (run == null)
        {
            return NotFound(new { message = $"Simulation {id} not found." });
        }

        return Ok(MappingExtensions.ToResponse(run));
    }

    [HttpPost]
    public async Task<ActionResult<SimulationResponse>> CreateSimulation(
        [FromBody] CreateSimulationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Simulation name is required." });
        }

        var run = await _simulationEngine.CreateSimulationAsync(request.Name, request.CreatedByUserId, cancellationToken);
        return CreatedAtAction(nameof(GetSimulationById), new { id = run.Id }, MappingExtensions.ToResponse(run));
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> StartSimulation(int id, CancellationToken cancellationToken)
    {
        var success = await _simulationEngine.StartSimulationAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Simulation {id} not found." });
        }

        return Ok(new { message = $"Simulation {id} started.", status = "Running" });
    }

    [HttpPost("{id:int}/pause")]
    public async Task<IActionResult> PauseSimulation(int id, CancellationToken cancellationToken)
    {
        var success = await _simulationEngine.PauseSimulationAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Simulation {id} not found." });
        }

        return Ok(new { message = $"Simulation {id} paused.", status = "Paused" });
    }

    [HttpPost("{id:int}/reset")]
    public async Task<IActionResult> ResetSimulation(int id, CancellationToken cancellationToken)
    {
        var success = await _simulationEngine.ResetSimulationAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Simulation {id} not found." });
        }

        return Ok(new { message = $"Simulation {id} reset.", status = "Created" });
    }
}
