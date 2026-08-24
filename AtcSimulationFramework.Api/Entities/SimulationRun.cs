using System.ComponentModel.DataAnnotations;

namespace AtcSimulationFramework.Api.Entities;

public class SimulationRun
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Running";

    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public int? AppUserId { get; set; }

    // Navigation Properties
    public virtual AppUser? AppUser { get; set; }
    public virtual ICollection<Aircraft> Aircraft { get; set; } = new List<Aircraft>();
    public virtual ICollection<ConflictEvent> ConflictEvents { get; set; } = new List<ConflictEvent>();
    public virtual ICollection<PositionLog> PositionLogs { get; set; } = new List<PositionLog>();
    public virtual ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
