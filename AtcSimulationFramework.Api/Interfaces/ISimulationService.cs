using AtcSimulationFramework.Api.Entities;

namespace AtcSimulationFramework.Api.Interfaces;

public interface ISimulationService
{
    Task<SimulationRun> StartSimulationRunAsync(CancellationToken cancellationToken = default);
    Task<bool> StopSimulationRunAsync(int id, CancellationToken cancellationToken = default);
    Task<SimulationRun?> GetSimulationRunAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SimulationRun>> GetActiveSimulationRunsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Aircraft>> GetAircraftForRunAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConflictEvent>> GetConflictsForRunAsync(int id, CancellationToken cancellationToken = default);
    Task AdvanceSimulationRunAsync(int simulationRunId, CancellationToken cancellationToken = default);
}
