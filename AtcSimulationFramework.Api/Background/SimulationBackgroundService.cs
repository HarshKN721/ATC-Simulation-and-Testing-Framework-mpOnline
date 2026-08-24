using AtcSimulationFramework.Api.Interfaces;

namespace AtcSimulationFramework.Api.Background;

public class SimulationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationBackgroundService> _logger;

    public SimulationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SimulationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var simulationService = scope.ServiceProvider.GetRequiredService<ISimulationService>();
                    var activeRuns = await simulationService.GetActiveSimulationRunsAsync(stoppingToken);

                    foreach (var run in activeRuns)
                    {
                        await simulationService.AdvanceSimulationRunAsync(run.Id, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing simulation tick.");
            }

            // Tick loop delay (e.g. 1 second interval)
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("SimulationBackgroundService stopping.");
    }
}
