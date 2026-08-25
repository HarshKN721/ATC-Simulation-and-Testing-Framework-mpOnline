namespace AtcSimulationFramework.Models;

public class ConflictEvent
{
    public int ConflictId { get; set; }
    public int RunId { get; set; }
    public int AircraftAId { get; set; }
    public int AircraftBId { get; set; }
    public DateTime DetectedAt { get; set; }
    public double HorizontalDistNm { get; set; }
    public int VerticalDistFt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionAction { get; set; }
}
