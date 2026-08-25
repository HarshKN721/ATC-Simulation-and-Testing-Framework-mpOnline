using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Models;
using AtcSimulationFramework.Api.Services.VectorCommands;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class VectorCommandServiceTests
{
    private static AtcDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AtcDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AtcDbContext(options);
    }

    [Fact]
    public async Task ProcessPendingCommandsAsync_AppliesHeadingAndAltitudeCommands()
    {
        using var context = CreateInMemoryDbContext();
        var service = new VectorCommandService(context);

        var run = new SimulationRun { Id = 1, Name = "Test Run", Status = SimulationStatus.Running };
        var aircraft = new Aircraft
        {
            Id = 1,
            SimulationRunId = 1,
            Callsign = "BAW123",
            CurrentHeadingDegrees = 0,
            TargetHeadingDegrees = 0,
            CurrentAltitudeFeet = 10000,
            TargetAltitudeFeet = 10000,
            CurrentSpeedKnots = 250,
            TargetSpeedKnots = 250
        };

        var cmdHeading = new VectorCommand
        {
            Id = 1,
            SimulationRunId = 1,
            AircraftId = 1,
            CommandType = VectorCommandType.Heading,
            TargetValue = 270,
            Status = VectorCommandStatus.Pending
        };

        var cmdAlt = new VectorCommand
        {
            Id = 2,
            SimulationRunId = 1,
            AircraftId = 1,
            CommandType = VectorCommandType.Altitude,
            TargetValue = 15000,
            Status = VectorCommandStatus.Pending
        };

        context.SimulationRuns.Add(run);
        context.Aircraft.Add(aircraft);
        context.VectorCommands.AddRange(cmdHeading, cmdAlt);
        await context.SaveChangesAsync();

        var aircraftList = new List<Aircraft> { aircraft };
        await service.ProcessPendingCommandsAsync(run, aircraftList, currentTick: 1);

        Assert.Equal(270, aircraft.TargetHeadingDegrees);
        Assert.Equal(15000, aircraft.TargetAltitudeFeet);
        Assert.True(aircraft.VerticalSpeedFpm > 0);
        Assert.Equal(VectorCommandStatus.Active, cmdHeading.Status);
        Assert.Equal(VectorCommandStatus.Active, cmdAlt.Status);
    }
}
