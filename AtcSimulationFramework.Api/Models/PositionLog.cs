using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtcSimulationFramework.Api.Models;

[Table("PositionLogs")]
public class PositionLog
{
    [Key]
    public long Id { get; set; }

    public int SimulationRunId { get; set; }

    public int AircraftId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double AltitudeFeet { get; set; }

    public double SpeedKnots { get; set; }

    public double HeadingDegrees { get; set; }

    public double VerticalSpeedFpm { get; set; }

    public long TickNumber { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SimulationRunId))]
    public virtual SimulationRun SimulationRun { get; set; } = null!;

    [ForeignKey(nameof(AircraftId))]
    public virtual Aircraft Aircraft { get; set; } = null!;
}
