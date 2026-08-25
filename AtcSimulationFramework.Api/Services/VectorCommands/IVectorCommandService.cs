using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Services.VectorCommands;

public interface IVectorCommandService
{
    Task ProcessPendingCommandsAsync(
        SimulationRun run,
        IList<Aircraft> aircraftList,
        long currentTick,
        CancellationToken cancellationToken = default);

    void CheckCommandCompletions(
        IList<Aircraft> aircraftList,
        IEnumerable<VectorCommand> activeCommands);
}
