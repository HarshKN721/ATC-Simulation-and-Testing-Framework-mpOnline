namespace AtcSimulationFramework.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PositionLog
{
    [Key]
    public long PositionId { get; set; }
    public int AircraftId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int AltitudeFt { get; set; }
    public decimal HeadingDeg { get; set; }
    public int SpeedKts { get; set; }

    // Navigation properties
    [ForeignKey("AircraftId")]
    public Aircraft? Aircraft { get; set; }
}
