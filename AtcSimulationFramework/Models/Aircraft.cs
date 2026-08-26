namespace AtcSimulationFramework.Models;

using System.ComponentModel.DataAnnotations.Schema;

public class Aircraft
{
    public int AircraftId { get; set; }
    public string Callsign { get; set; } = string.Empty;
    public string Icao24 { get; set; } = string.Empty;
    public int RunId { get; set; }

    // In-memory / dynamic simulation state properties
    [NotMapped] public double Latitude { get; set; }
    [NotMapped] public double Longitude { get; set; }
    [NotMapped] public int AltitudeFt { get; set; }
    [NotMapped] public double HeadingDeg { get; set; }
    [NotMapped] public int SpeedKts { get; set; }

    // Navigation properties
    [ForeignKey("RunId")]
    public SimulationRun? Run { get; set; }

    public ICollection<PositionLog> PositionLogs { get; set; } = new List<PositionLog>();
    public ICollection<VectorCommand> VectorCommands { get; set; } = new List<VectorCommand>();
}
