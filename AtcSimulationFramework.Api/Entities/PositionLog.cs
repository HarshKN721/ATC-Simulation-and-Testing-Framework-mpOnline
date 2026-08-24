using System.ComponentModel.DataAnnotations;

namespace AtcSimulationFramework.Api.Entities;

public class PositionLog
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    public int AircraftId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Altitude { get; set; }

    public double Heading { get; set; }

    public double Speed { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual Aircraft? Aircraft { get; set; }
}
