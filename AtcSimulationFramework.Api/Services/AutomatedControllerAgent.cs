using AtcSimulationFramework.Api.Entities;
using AtcSimulationFramework.Api.Interfaces;

namespace AtcSimulationFramework.Api.Services;

public class AutomatedControllerAgent : IControllerAgent
{
    public VectorCommand ResolveConflict(ConflictEvent conflict)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        return BuildResolutionCommand(conflict);
    }

    private VectorCommand BuildResolutionCommand(ConflictEvent conflict)
    {
        double currentHeading = conflict.AircraftA?.Heading ?? 0.0;
        double headingDelta = 30.0;
        double newHeading = (currentHeading + headingDelta) % 360.0;
        if (newHeading < 0)
        {
            newHeading += 360.0;
        }

        return new VectorCommand
        {
            SimulationRunId = conflict.SimulationRunId,
            ConflictEventId = conflict.Id > 0 ? conflict.Id : null,
            AircraftId = conflict.AircraftAId,
            CommandType = "HeadingChange",
            HeadingDelta = headingDelta,
            NewHeading = newHeading,
            CreatedAt = DateTime.UtcNow,
            IsExecuted = false
        };
    }
}
