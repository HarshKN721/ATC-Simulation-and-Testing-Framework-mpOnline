using System.ComponentModel.DataAnnotations;

namespace AtcSimulationFramework.Api.Entities;

public class VectorCommand
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    public int? ConflictEventId { get; set; }

    public int AircraftId { get; set; }

    [Required]
    [MaxLength(50)]
    public string CommandType { get; set; } = "HeadingChange";

    public double HeadingDelta { get; set; }

    public double NewHeading { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExecuted { get; set; }

    // Navigation Properties
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual ConflictEvent? ConflictEvent { get; set; }
    public virtual Aircraft? Aircraft { get; set; }
}
