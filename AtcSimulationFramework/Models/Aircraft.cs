namespace AtcSimulationFramework.Models;

public class Aircraft
{
    public int AircraftId { get; set; }
    public string Callsign { get; set; } = string.Empty;
    public string Icao24 { get; set; } = string.Empty;
    public int RunId { get; set; }

    // In-memory / dynamic simulation state properties
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int AltitudeFt { get; set; }
    public double HeadingDeg { get; set; }
    public int SpeedKts { get; set; }
}
