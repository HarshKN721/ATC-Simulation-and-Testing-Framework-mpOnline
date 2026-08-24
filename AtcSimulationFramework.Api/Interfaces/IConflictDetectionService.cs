using AtcSimulationFramework.Api.Entities;

namespace AtcSimulationFramework.Api.Interfaces;

public interface IConflictDetectionService
{
    IEnumerable<ConflictEvent> DetectConflicts(SimulationRun run);
}
