namespace AtcSimulationFramework.Models;

public class SimulationRun
{
    public int RunId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
}
