using Microsoft.AspNetCore.Mvc;
using Moq;
using AtcSimulationFramework.Api.Controllers;
using AtcSimulationFramework.Api.DTOs;
using AtcSimulationFramework.Api.Entities;
using AtcSimulationFramework.Api.Interfaces;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class SimulationRunsControllerTests
{
    private readonly Mock<ISimulationService> _simulationServiceMock;
    private readonly SimulationRunsController _controller;

    public SimulationRunsControllerTests()
    {
        _simulationServiceMock = new Mock<ISimulationService>();
        _controller = new SimulationRunsController(_simulationServiceMock.Object);
    }

    [Fact]
    public async Task StartSimulationRun_Returns201Created_WithRunDetails()
    {
        // Arrange
        var createdRun = new SimulationRun
        {
            Id = 123,
            Status = "Running",
            StartTime = DateTime.UtcNow
        };

        _simulationServiceMock
            .Setup(s => s.StartSimulationRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRun);

        // Act
        var result = await _controller.StartSimulationRun(CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        var response = Assert.IsType<StartSimulationRunResponse>(createdResult.Value);
        Assert.Equal(123, response.SimulationRunId);
        Assert.Equal("Running", response.Status);
    }

    [Fact]
    public async Task StopSimulationRun_ExistingRun_Returns200OK()
    {
        // Arrange
        int runId = 123;
        var existingRun = new SimulationRun { Id = runId, Status = "Running" };
        var stoppedRun = new SimulationRun { Id = runId, Status = "Stopped", EndTime = DateTime.UtcNow };

        _simulationServiceMock
            .SetupSequence(s => s.GetSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRun)
            .ReturnsAsync(stoppedRun);

        _simulationServiceMock
            .Setup(s => s.StopSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.StopSimulationRun(runId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<StopSimulationRunResponse>(okResult.Value);
        Assert.Equal(runId, response.SimulationRunId);
        Assert.Equal("Stopped", response.Status);
    }

    [Fact]
    public async Task StopSimulationRun_NonExistentRun_Returns404NotFound_WithConsistentErrorJson()
    {
        // Arrange
        int runId = 999;
        _simulationServiceMock
            .Setup(s => s.GetSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRun?)null);

        // Act
        var result = await _controller.StopSimulationRun(runId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("SimulationRunNotFound", errorResponse.Error);
        Assert.Contains("999", errorResponse.Message);
    }

    [Fact]
    public async Task GetAircraft_ExistingRun_ReturnsAircraftList()
    {
        // Arrange
        int runId = 123;
        var existingRun = new SimulationRun { Id = runId, Status = "Running" };
        var aircraftList = new List<Aircraft>
        {
            new Aircraft { Id = 1, SimulationRunId = runId, Name = "AircraftA", Latitude = 23.25, Longitude = 77.41, Altitude = 30000, Heading = 120, Speed = 450 }
        };

        _simulationServiceMock
            .Setup(s => s.GetSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRun);
        _simulationServiceMock
            .Setup(s => s.GetAircraftForRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aircraftList);

        // Act
        var result = await _controller.GetAircraft(runId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<AircraftPositionsResponse>(okResult.Value);
        Assert.Equal(runId, response.SimulationRunId);
        Assert.Single(response.Aircraft);
        Assert.Equal("AircraftA", response.Aircraft.First().Name);
    }

    [Fact]
    public async Task GetAircraft_NonExistentRun_Returns404NotFound_WithConsistentErrorJson()
    {
        // Arrange
        int runId = 999;
        _simulationServiceMock
            .Setup(s => s.GetSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRun?)null);

        // Act
        var result = await _controller.GetAircraft(runId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("SimulationRunNotFound", errorResponse.Error);
    }

    [Fact]
    public async Task GetConflicts_ExistingRun_ReturnsConflictsList()
    {
        // Arrange
        int runId = 123;
        var existingRun = new SimulationRun { Id = runId, Status = "Running" };
        var conflictsList = new List<ConflictEvent>
        {
            new ConflictEvent
            {
                Id = 10,
                SimulationRunId = runId,
                AircraftAId = 1,
                AircraftBId = 2,
                AircraftA = new Aircraft { Id = 1, Name = "AircraftA" },
                AircraftB = new Aircraft { Id = 2, Name = "AircraftB" },
                Distance = 0.02,
                IsResolved = true,
                Timestamp = DateTime.UtcNow
            }
        };

        _simulationServiceMock
            .Setup(s => s.GetSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRun);
        _simulationServiceMock
            .Setup(s => s.GetConflictsForRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictsList);

        // Act
        var result = await _controller.GetConflicts(runId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<ConflictEventsResponse>(okResult.Value);
        Assert.Equal(runId, response.SimulationRunId);
        Assert.Single(response.Conflicts);
        Assert.Equal("AircraftA", response.Conflicts.First().AircraftAName);
    }

    [Fact]
    public async Task GetConflicts_NonExistentRun_Returns404NotFound_WithConsistentErrorJson()
    {
        // Arrange
        int runId = 999;
        _simulationServiceMock
            .Setup(s => s.GetSimulationRunAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRun?)null);

        // Act
        var result = await _controller.GetConflicts(runId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("SimulationRunNotFound", errorResponse.Error);
    }
}
