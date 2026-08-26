namespace AtcSimulationFramework.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class VectorCommand
{
    [Key]
    public int CommandId { get; set; }
    public int ConflictId { get; set; }
    public int AircraftId { get; set; }
    public string CommandType { get; set; } = string.Empty;  // "Heading" or "Altitude"
    public decimal Value { get; set; }
    public DateTime IssuedAt { get; set; }

    // Navigation properties
    [ForeignKey("ConflictId")]
    public ConflictEvent? Conflict { get; set; }

    [ForeignKey("AircraftId")]
    public Aircraft? Aircraft { get; set; }
}
