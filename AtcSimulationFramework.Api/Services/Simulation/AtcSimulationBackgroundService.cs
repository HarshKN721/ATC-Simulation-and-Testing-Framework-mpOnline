using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Services.Simulation;

public class AtcSimulationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AtcSimulationBackgroundService> _logger;
    private const int DefaultTickDelayMs = 1000;

    public AtcSimulationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AtcSimulationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ATC Simulation Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                await ProcessActiveSimulationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during ATC simulation tick loop.");
            }

            var elapsedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var delayMs = Math.Max(50, DefaultTickDelayMs - elapsedMs);

            await Task.Delay(delayMs, stoppingToken);
        }

        _logger.LogInformation("ATC Simulation Background Service is stopping.");
    }

    private async Task ProcessActiveSimulationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AtcDbContext>();
        var simulationEngine = scope.ServiceProvider.GetRequiredService<ISimulationEngine>();

        var activeRunIds = await dbContext.SimulationRuns
            .Where(r => r.Status == SimulationStatus.Running)
            .Select(r => new { r.Id, r.TickIntervalMs })
            .ToListAsync(stoppingToken);

        foreach (var run in activeRunIds)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var dtSeconds = run.TickIntervalMs > 0 ? run.TickIntervalMs / 1000.0 : 1.0;
            await simulationEngine.ExecuteSimulationTickAsync(run.Id, dtSeconds, stoppingToken);
        }
    }
}
