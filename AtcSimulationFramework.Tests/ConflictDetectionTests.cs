using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Models;
using AtcSimulationFramework.Api.Services.ConflictDetection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class ConflictDetectionTests
{
    private static AtcDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AtcDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AtcDbContext(options);
    }

    [Fact]
    public async Task ProcessConflictsAsync_DetectsLossOfSeparation()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ConflictDetectionService(context);

        var run = new SimulationRun { Id = 1, Name = "Test Run", Status = SimulationStatus.Running };
        context.SimulationRuns.Add(run);

        // Aircraft A and B are very close (0.01 deg lat ~ 0.6 NM, same altitude)
        var a1 = new Aircraft
        {
            Id = 1,
            SimulationRunId = 1,
            Callsign = "AC1",
            CurrentLatitude = 40.00,
            CurrentLongitude = -74.00,
            CurrentAltitudeFeet = 10000,
            Status = AircraftStatus.Airborne
        };
        var a2 = new Aircraft
        {
            Id = 2,
            SimulationRunId = 1,
            Callsign = "AC2",
            CurrentLatitude = 40.01,
            CurrentLongitude = -74.00,
            CurrentAltitudeFeet = 10000,
            Status = AircraftStatus.Airborne
        };

        context.Aircraft.AddRange(a1, a2);
        await context.SaveChangesAsync();

        var list = new List<Aircraft> { a1, a2 };
        await service.ProcessConflictsAsync(run, list, currentTick: 1);
        await context.SaveChangesAsync();

        Assert.Equal(AircraftStatus.Conflict, a1.Status);
        Assert.Equal(AircraftStatus.Conflict, a2.Status);

        var conflicts = await context.ConflictEvents.ToListAsync();
        Assert.Single(conflicts);
        Assert.False(conflicts[0].IsResolved);
    }

    [Fact]
    public async Task ProcessConflictsAsync_ResolvesConflict_WhenAircraftDiverge()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ConflictDetectionService(context);

        var run = new SimulationRun { Id = 1, Name = "Test Run", Status = SimulationStatus.Running };
        context.SimulationRuns.Add(run);

        var a1 = new Aircraft
        {
            Id = 1,
            SimulationRunId = 1,
            Callsign = "AC1",
            CurrentLatitude = 40.00,
            CurrentLongitude = -74.00,
            CurrentAltitudeFeet = 10000,
            Status = AircraftStatus.Conflict
        };
        var a2 = new Aircraft
        {
            Id = 2,
            SimulationRunId = 1,
            Callsign = "AC2",
            CurrentLatitude = 40.00,
            CurrentLongitude = -74.00,
            CurrentAltitudeFeet = 13000, // 3000 ft vertical separation
            Status = AircraftStatus.Conflict
        };

        var existingConflict = new ConflictEvent
        {
            Id = 1,
            SimulationRunId = 1,
            PrimaryAircraftId = 1,
            SecondaryAircraftId = 2,
            IsResolved = false,
            DetectedAtTick = 1
        };

        context.Aircraft.AddRange(a1, a2);
        context.ConflictEvents.Add(existingConflict);
        await context.SaveChangesAsync();

        var list = new List<Aircraft> { a1, a2 };
        await service.ProcessConflictsAsync(run, list, currentTick: 10);
        await context.SaveChangesAsync();

        Assert.True(existingConflict.IsResolved);
        Assert.Equal(10, existingConflict.ResolvedAtTick);
        Assert.Equal(AircraftStatus.Airborne, a1.Status);
        Assert.Equal(AircraftStatus.Airborne, a2.Status);
    }
}
