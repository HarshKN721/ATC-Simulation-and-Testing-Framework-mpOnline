using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtcSimulationFramework.Api.Models;

[Table("VectorCommands")]
public class VectorCommand
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    public int AircraftId { get; set; }

    public int? IssuedByUserId { get; set; }

    public VectorCommandType CommandType { get; set; }

    public double TargetValue { get; set; }

    [MaxLength(50)]
    public string? WaypointTarget { get; set; }

    public long IssuedAtTick { get; set; }

    public VectorCommandStatus Status { get; set; } = VectorCommandStatus.Pending;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExecutedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SimulationRunId))]
    public virtual SimulationRun SimulationRun { get; set; } = null!;

    [ForeignKey(nameof(AircraftId))]
    public virtual Aircraft Aircraft { get; set; } = null!;

    [ForeignKey(nameof(IssuedByUserId))]
    public virtual AppUser? IssuedByUser { get; set; }
}
