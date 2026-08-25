using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtcSimulationFramework.Api.Models;

[Table("SimulationRuns")]
public class SimulationRun
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public SimulationStatus Status { get; set; } = SimulationStatus.Created;

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int TickIntervalMs { get; set; } = 1000;

    public long CurrentTick { get; set; } = 0;

    public double ElapsedSeconds { get; set; } = 0.0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CreatedByUserId))]
    public virtual AppUser? CreatedByUser { get; set; }

    public virtual ICollection<Aircraft> AircraftList { get; set; } = new List<Aircraft>();
    public virtual ICollection<PositionLog> PositionLogs { get; set; } = new List<PositionLog>();
    public virtual ICollection<ConflictEvent> ConflictEvents { get; set; } = new List<ConflictEvent>();
    public virtual ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
