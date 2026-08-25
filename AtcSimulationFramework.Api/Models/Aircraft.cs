using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtcSimulationFramework.Api.Models;

[Table("Aircraft")]
public class Aircraft
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Callsign { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ModelType { get; set; } = "B738";

    // Telemetry - Position & Motion
    public double CurrentLatitude { get; set; }

    public double CurrentLongitude { get; set; }

    public double CurrentAltitudeFeet { get; set; }

    public double CurrentSpeedKnots { get; set; }

    public double CurrentHeadingDegrees { get; set; }

    // Target ATC clearances
    public double TargetAltitudeFeet { get; set; }

    public double TargetSpeedKnots { get; set; }

    public double TargetHeadingDegrees { get; set; }

    public double VerticalSpeedFpm { get; set; } = 0.0;

    public AircraftStatus Status { get; set; } = AircraftStatus.Airborne;

    [MaxLength(10)]
    public string SquawkCode { get; set; } = "1200";

    [MaxLength(50)]
    public string AssignedSector { get; set; } = "Sector-A";

    [MaxLength(500)]
    public string FlightPlanRoute { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SimulationRunId))]
    public virtual SimulationRun SimulationRun { get; set; } = null!;

    public virtual ICollection<PositionLog> PositionLogs { get; set; } = new List<PositionLog>();
    public virtual ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
