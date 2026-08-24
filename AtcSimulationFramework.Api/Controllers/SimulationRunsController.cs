using Microsoft.AspNetCore.Mvc;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Interfaces;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationRunsController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    public SimulationRunsController(ISimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    /// <summary>
    /// Start a new simulation run.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StartSimulationRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartSimulationRun(CancellationToken cancellationToken)
    {
        var run = await _simulationService.StartSimulationRunAsync(cancellationToken);

        var response = new StartSimulationRunResponse
        {
            SimulationRunId = run.Id,
            Status = run.Status,
            StartTime = run.StartTime
        };

        return CreatedAtAction(nameof(GetAircraft), new { id = run.Id }, response);
    }

    /// <summary>
    /// Stop an active simulation run.
    /// </summary>
    [HttpPatch("{id}/stop")]
    [ProducesResponseType(typeof(StopSimulationRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopSimulationRun(int id, CancellationToken cancellationToken)
    {
        var existingRun = await _simulationService.GetSimulationRunAsync(id, cancellationToken);
        if (existingRun == null)
        {
            return CreateErrorResponse(
                StatusCodes.Status404NotFound,
                "SimulationRunNotFound",
                $"Simulation run {id} was not found.");
        }

        await _simulationService.StopSimulationRunAsync(id, cancellationToken);
        var updatedRun = await _simulationService.GetSimulationRunAsync(id, cancellationToken);

        var response = new StopSimulationRunResponse
        {
            SimulationRunId = id,
            Status = updatedRun?.Status ?? "Stopped",
            EndTime = updatedRun?.EndTime
        };

        return Ok(response);
    }

    /// <summary>
    /// Get current aircraft positions for a simulation run (Frontend Polling Endpoint).
    /// </summary>
    [HttpGet("{id}/aircraft")]
    [ProducesResponseType(typeof(AircraftPositionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAircraft(int id, CancellationToken cancellationToken)
    {
        var existingRun = await _simulationService.GetSimulationRunAsync(id, cancellationToken);
        if (existingRun == null)
        {
            return CreateErrorResponse(
                StatusCodes.Status404NotFound,
                "SimulationRunNotFound",
                $"Simulation run {id} was not found.");
        }

        var aircraftList = await _simulationService.GetAircraftForRunAsync(id, cancellationToken);

        var response = new AircraftPositionsResponse
        {
            SimulationRunId = id,
            Aircraft = aircraftList.Select(a => new AircraftPositionDto
            {
                Id = a.Id,
                Name = a.Name,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Altitude = a.Altitude,
                Heading = a.Heading,
                Speed = a.Speed
            })
        };

        return Ok(response);
    }

    /// <summary>
    /// Get conflict history for a simulation run.
    /// </summary>
    [HttpGet("{id}/conflicts")]
    [ProducesResponseType(typeof(ConflictEventsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConflicts(int id, CancellationToken cancellationToken)
    {
        var existingRun = await _simulationService.GetSimulationRunAsync(id, cancellationToken);
        if (existingRun == null)
        {
            return CreateErrorResponse(
                StatusCodes.Status404NotFound,
                "SimulationRunNotFound",
                $"Simulation run {id} was not found.");
        }

        var conflictsList = await _simulationService.GetConflictsForRunAsync(id, cancellationToken);

        var response = new ConflictEventsResponse
        {
            SimulationRunId = id,
            Conflicts = conflictsList.Select(c => new ConflictEventDto
            {
                Id = c.Id,
                SimulationRunId = c.SimulationRunId,
                AircraftAId = c.AircraftAId,
                AircraftAName = c.AircraftA?.Name ?? $"Aircraft {c.AircraftAId}",
                AircraftBId = c.AircraftBId,
                AircraftBName = c.AircraftB?.Name ?? $"Aircraft {c.AircraftBId}",
                Timestamp = c.Timestamp,
                Distance = c.Distance,
                IsResolved = c.IsResolved,
                ResolutionTimestamp = c.ResolutionTimestamp
            })
        };

        return Ok(response);
    }

    /// <summary>
    /// Single reusable helper method for consistent JSON error responses across all actions.
    /// </summary>
    private ActionResult CreateErrorResponse(int statusCode, string error, string message)
    {
        return StatusCode(statusCode, new ErrorResponse
        {
            Error = error,
            Message = message
        });
    }
}
