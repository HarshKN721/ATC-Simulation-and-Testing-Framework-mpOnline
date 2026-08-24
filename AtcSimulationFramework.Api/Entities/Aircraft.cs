using System.ComponentModel.DataAnnotations;

namespace AtcSimulationFramework.Api.Entities;

public class Aircraft
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Altitude { get; set; }

    public double Heading { get; set; }

    public double Speed { get; set; }

    // Navigation Properties
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual ICollection<PositionLog> PositionLogs { get; set; } = new List<PositionLog>();
    public virtual ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
