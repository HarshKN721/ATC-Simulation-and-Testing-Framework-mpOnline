using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Models;
using AtcSimulationFramework.Api.Services.Simulation;
using Microsoft.AspNetCore.Mvc;

namespace AtcSimulationFramework.Api.Controllers;

[ApiController]
[Route("api/scenarios")]
[Route("api/scenario")]
public class ScenarioController : ControllerBase
{
    private readonly AtcDbContext _dbContext;
    private readonly ISimulationEngine _simulationEngine;

    public ScenarioController(AtcDbContext dbContext, ISimulationEngine simulationEngine)
    {
        _dbContext = dbContext;
        _simulationEngine = simulationEngine;
    }

    [HttpPost("seed")]
    public async Task<ActionResult<SimulationResponse>> SeedScenario(
        [FromBody] LoadScenarioRequest request,
        CancellationToken cancellationToken)
    {
        var scenarioName = request.CustomName ?? $"Scenario - {request.ScenarioType.ToUpperInvariant()} - {DateTime.UtcNow:HH:mm:ss}";
        var run = await _simulationEngine.CreateSimulationAsync(scenarioName, null, cancellationToken);

        var aircraft = CreateScenarioAircraft(request.ScenarioType.ToLowerInvariant(), run.Id);
        _dbContext.Aircraft.AddRange(aircraft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        run.AircraftList = aircraft;
        return CreatedAtAction(
            "GetSimulationById",
            "Simulation",
            new { id = run.Id },
            MappingExtensions.ToResponse(run));
    }

    private static List<Aircraft> CreateScenarioAircraft(string scenarioType, int simulationRunId)
    {
        return scenarioType switch
        {
            "head_on" => CreateHeadOnScenario(simulationRunId),
            "converging" => CreateConvergingScenario(simulationRunId),
            "sector_busy" => CreateBusySectorScenario(simulationRunId),
            _ => CreateDefaultPairScenario(simulationRunId)
        };
    }

    private static List<Aircraft> CreateHeadOnScenario(int runId)
    {
        return new List<Aircraft>
        {
            new()
            {
                SimulationRunId = runId,
                Callsign = "AAL101",
                ModelType = "B772",
                CurrentLatitude = 40.0,
                CurrentLongitude = -74.5,
                CurrentAltitudeFeet = 33000,
                CurrentSpeedKnots = 450,
                CurrentHeadingDegrees = 90,
                TargetAltitudeFeet = 33000,
                TargetSpeedKnots = 450,
                TargetHeadingDegrees = 90,
                SquawkCode = "4512",
                AssignedSector = "Sector-East",
                FlightPlanRoute = "KJFK-KORD"
            },
            new()
            {
                SimulationRunId = runId,
                Callsign = "UAL202",
                ModelType = "A321",
                CurrentLatitude = 40.0,
                CurrentLongitude = -74.1,
                CurrentAltitudeFeet = 33000,
                CurrentSpeedKnots = 440,
                CurrentHeadingDegrees = 270,
                TargetAltitudeFeet = 33000,
                TargetSpeedKnots = 440,
                TargetHeadingDegrees = 270,
                SquawkCode = "1724",
                AssignedSector = "Sector-East",
                FlightPlanRoute = "KORD-KJFK"
            }
        };
    }

    private static List<Aircraft> CreateConvergingScenario(int runId)
    {
        return new List<Aircraft>
        {
            new()
            {
                SimulationRunId = runId,
                Callsign = "DLH450",
                ModelType = "A359",
                CurrentLatitude = 28.50,
                CurrentLongitude = 77.00,
                CurrentAltitudeFeet = 28000,
                CurrentSpeedKnots = 420,
                CurrentHeadingDegrees = 45,
                TargetAltitudeFeet = 28000,
                TargetSpeedKnots = 420,
                TargetHeadingDegrees = 45,
                SquawkCode = "2105",
                AssignedSector = "DELHI-TMA",
                FlightPlanRoute = "VIDP-VABB"
            },
            new()
            {
                SimulationRunId = runId,
                Callsign = "AIC884",
                ModelType = "B788",
                CurrentLatitude = 28.70,
                CurrentLongitude = 77.00,
                CurrentAltitudeFeet = 28000,
                CurrentSpeedKnots = 420,
                CurrentHeadingDegrees = 135,
                TargetAltitudeFeet = 28000,
                TargetSpeedKnots = 420,
                TargetHeadingDegrees = 135,
                SquawkCode = "3421",
                AssignedSector = "DELHI-TMA",
                FlightPlanRoute = "VIAR-VIDP"
            }
        };
    }

    private static List<Aircraft> CreateBusySectorScenario(int runId)
    {
        return new List<Aircraft>
        {
            new()
            {
                SimulationRunId = runId,
                Callsign = "BAW123",
                ModelType = "B77W",
                CurrentLatitude = 51.47,
                CurrentLongitude = -0.45,
                CurrentAltitudeFeet = 15000,
                CurrentSpeedKnots = 320,
                CurrentHeadingDegrees = 90,
                TargetAltitudeFeet = 8000,
                TargetSpeedKnots = 250,
                TargetHeadingDegrees = 90,
                SquawkCode = "7120",
                AssignedSector = "LON-APP"
            },
            new()
            {
                SimulationRunId = runId,
                Callsign = "AFR678",
                ModelType = "A320",
                CurrentLatitude = 51.40,
                CurrentLongitude = -0.30,
                CurrentAltitudeFeet = 9000,
                CurrentSpeedKnots = 260,
                CurrentHeadingDegrees = 360,
                TargetAltitudeFeet = 4000,
                TargetSpeedKnots = 210,
                TargetHeadingDegrees = 360,
                SquawkCode = "3340",
                AssignedSector = "LON-APP"
            },
            new()
            {
                SimulationRunId = runId,
                Callsign = "EZY991",
                ModelType = "A320",
                CurrentLatitude = 51.55,
                CurrentLongitude = -0.20,
                CurrentAltitudeFeet = 18000,
                CurrentSpeedKnots = 380,
                CurrentHeadingDegrees = 240,
                TargetAltitudeFeet = 24000,
                TargetSpeedKnots = 420,
                TargetHeadingDegrees = 240,
                SquawkCode = "5512",
                AssignedSector = "LON-ENR"
            }
        };
    }

    private static List<Aircraft> CreateDefaultPairScenario(int runId) => CreateHeadOnScenario(runId);
}
