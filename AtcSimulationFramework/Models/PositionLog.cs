namespace AtcSimulationFramework.Models;

public class PositionLog
{
    public long PositionId { get; set; }
    public int AircraftId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int AltitudeFt { get; set; }
    public decimal HeadingDeg { get; set; }
    public int SpeedKts { get; set; }
}
