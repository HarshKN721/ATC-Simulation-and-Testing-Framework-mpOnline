using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Services.ConflictDetection;

public interface IConflictDetectionService
{
    Task ProcessConflictsAsync(
        SimulationRun run,
        IList<Aircraft> aircraftList,
        long currentTick,
        CancellationToken cancellationToken = default);
}
