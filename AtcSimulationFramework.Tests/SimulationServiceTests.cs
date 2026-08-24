using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Entities;
using AtcSimulationFramework.Api.Services;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class SimulationServiceTests
{
    private AtcDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AtcDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AtcDbContext(options);
    }

    [Fact]
    public async Task StartSimulationRunAsync_CreatesRunAndSeedsAircraft()
    {
        // Arrange
        using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var conflictDetectionService = new ConflictDetectionService();
        var agent = new AutomatedControllerAgent();
        var service = new SimulationService(db, conflictDetectionService, agent);

        // Act
        var run = await service.StartSimulationRunAsync();

        // Assert
        Assert.NotNull(run);
        Assert.True(run.Id > 0);
        Assert.Equal("Running", run.Status);

        var aircraft = await db.Aircraft.Where(a => a.SimulationRunId == run.Id).ToListAsync();
        Assert.Equal(2, aircraft.Count);
        Assert.Contains(aircraft, a => a.Name == "AircraftA");
        Assert.Contains(aircraft, a => a.Name == "AircraftB");
    }

    [Fact]
    public async Task StopSimulationRunAsync_StopsActiveRun()
    {
        // Arrange
        using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var conflictDetectionService = new ConflictDetectionService();
        var agent = new AutomatedControllerAgent();
        var service = new SimulationService(db, conflictDetectionService, agent);

        var run = await service.StartSimulationRunAsync();

        // Act
        bool stopped = await service.StopSimulationRunAsync(run.Id);

        // Assert
        Assert.True(stopped);
        var updatedRun = await db.SimulationRuns.FindAsync(run.Id);
        Assert.NotNull(updatedRun);
        Assert.Equal("Stopped", updatedRun.Status);
        Assert.NotNull(updatedRun.EndTime);
    }

    [Fact]
    public async Task AdvanceSimulationRunAsync_DetectsConflict_InvokesAgent_AppliesPlus30HeadingChange()
    {
        // Arrange
        using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var conflictDetectionService = new ConflictDetectionService();
        var agent = new AutomatedControllerAgent();
        var service = new SimulationService(db, conflictDetectionService, agent);

        // Create a run with two aircraft in close proximity heading towards each other
        var run = new SimulationRun { Status = "Running", StartTime = DateTime.UtcNow };
        db.SimulationRuns.Add(run);
        await db.SaveChangesAsync();

        var aircraftA = new Aircraft
        {
            SimulationRunId = run.Id,
            Name = "AircraftA",
            Latitude = 23.2500,
            Longitude = 77.4100,
            Altitude = 30000,
            Heading = 90.0, // Moving East
            Speed = 450.0
        };

        var aircraftB = new Aircraft
        {
            SimulationRunId = run.Id,
            Name = "AircraftB",
            Latitude = 23.2501,
            Longitude = 77.4101, // Very close -> triggers conflict
            Altitude = 30000,
            Heading = 270.0,
            Speed = 450.0
        };

        db.Aircraft.AddRange(aircraftA, aircraftB);
        await db.SaveChangesAsync();

        // Act
        await service.AdvanceSimulationRunAsync(run.Id);

        // Assert
        var updatedA = await db.Aircraft.FindAsync(aircraftA.Id);
        Assert.NotNull(updatedA);
        Assert.Equal(120.0, updatedA.Heading); // Initial 90.0 + 30.0 = 120.0 heading change!

        var conflict = await db.ConflictEvents.FirstOrDefaultAsync(c => c.SimulationRunId == run.Id);
        Assert.NotNull(conflict);
        Assert.True(conflict.IsResolved);

        var command = await db.VectorCommands.FirstOrDefaultAsync(v => v.SimulationRunId == run.Id);
        Assert.NotNull(command);
        Assert.Equal(aircraftA.Id, command.AircraftId);
        Assert.Equal(30.0, command.HeadingDelta);
        Assert.Equal(120.0, command.NewHeading);
        Assert.True(command.IsExecuted);
    }
}
