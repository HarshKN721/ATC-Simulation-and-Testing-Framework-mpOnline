using System.ComponentModel.DataAnnotations;

namespace AtcSimulationFramework.Api.Entities;

public class ConflictEvent
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    public int AircraftAId { get; set; }

    public int AircraftBId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public double Distance { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolutionTimestamp { get; set; }

    // Navigation Properties
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual Aircraft? AircraftA { get; set; }
    public virtual Aircraft? AircraftB { get; set; }
    public virtual ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
