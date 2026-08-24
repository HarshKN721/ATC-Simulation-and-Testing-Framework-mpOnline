using AtcSimulationFramework.Api.Entities;
using AtcSimulationFramework.Api.Services;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class AutomatedControllerAgentTests
{
    private readonly AutomatedControllerAgent _agent;

    public AutomatedControllerAgentTests()
    {
        _agent = new AutomatedControllerAgent();
    }

    [Fact]
    public void ResolveConflict_ReturnsVectorCommand_TargetingAircraftA_WithPlus30HeadingChange()
    {
        // Arrange
        var aircraftA = new Aircraft { Id = 1, Name = "AircraftA", Heading = 120.0 };
        var aircraftB = new Aircraft { Id = 2, Name = "AircraftB", Heading = 270.0 };
        var conflict = new ConflictEvent
        {
            Id = 10,
            SimulationRunId = 1,
            AircraftAId = 1,
            AircraftBId = 2,
            AircraftA = aircraftA,
            AircraftB = aircraftB,
            Distance = 0.02,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var command = _agent.ResolveConflict(conflict);

        // Assert
        Assert.NotNull(command);
        Assert.Equal(1, command.SimulationRunId);
        Assert.Equal(10, command.ConflictEventId);
        Assert.Equal(1, command.AircraftId); // Targets AircraftA
        Assert.Equal("HeadingChange", command.CommandType);
        Assert.Equal(30.0, command.HeadingDelta); // +30 degrees heading change
        Assert.Equal(150.0, command.NewHeading); // 120 + 30 = 150
        Assert.False(command.IsExecuted);
    }

    [Fact]
    public void ResolveConflict_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _agent.ResolveConflict(null!));
    }

    [Fact]
    public void BuildResolutionCommand_NormalizesHeading_WhenCrossing360Degrees()
    {
        // Arrange
        var aircraftA = new Aircraft { Id = 1, Name = "AircraftA", Heading = 350.0 };
        var conflict = new ConflictEvent
        {
            Id = 11,
            SimulationRunId = 1,
            AircraftAId = 1,
            AircraftBId = 2,
            AircraftA = aircraftA,
            Distance = 0.01,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var command = _agent.ResolveConflict(conflict);

        // Assert
        Assert.NotNull(command);
        Assert.Equal(30.0, command.HeadingDelta);
        Assert.Equal(20.0, command.NewHeading); // (350 + 30) % 360 = 20
    }
}
