namespace AtcSimulationFramework.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ConflictEvent
{
    [Key]
    public int ConflictId { get; set; }
    public int RunId { get; set; }
    public int AircraftAId { get; set; }
    public int AircraftBId { get; set; }
    public DateTime DetectedAt { get; set; }
    public decimal HorizontalDistNm { get; set; }
    public int VerticalDistFt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionAction { get; set; }

    // Navigation properties
    [ForeignKey("RunId")]
    public SimulationRun? Run { get; set; }

    // AircraftA and AircraftB are configured via Fluent API in
    // AtcDbContext.OnModelCreating because EF Core cannot auto-discover
    // two FKs pointing at the same table from data annotations alone.
    public Aircraft? AircraftA { get; set; }
    public Aircraft? AircraftB { get; set; }

    public ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
