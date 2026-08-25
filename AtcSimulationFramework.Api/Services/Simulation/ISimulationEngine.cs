using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Services.Simulation;

public interface ISimulationEngine
{
    Task ExecuteSimulationTickAsync(int simulationRunId, double dtSeconds, CancellationToken cancellationToken = default);
    Task<SimulationRun> CreateSimulationAsync(string name, int? userId = null, CancellationToken cancellationToken = default);
    Task<bool> StartSimulationAsync(int simulationRunId, CancellationToken cancellationToken = default);
    Task<bool> PauseSimulationAsync(int simulationRunId, CancellationToken cancellationToken = default);
    Task<bool> ResetSimulationAsync(int simulationRunId, CancellationToken cancellationToken = default);
}
