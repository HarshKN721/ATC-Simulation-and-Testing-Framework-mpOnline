namespace AtcSimulationFramework.Services;

using Microsoft.Extensions.Hosting;
using AtcSimulationFramework.Models;

public interface IControllerAgent
{
}

public class AutomatedControllerAgent : IControllerAgent
{
}

public class SimulationTickService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;
    }
}
