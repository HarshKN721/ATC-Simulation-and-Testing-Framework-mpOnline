namespace AtcSimulationFramework.Models;

using System.ComponentModel.DataAnnotations;

public class AppUser
{
    [Key]
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public string Role { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<SimulationRun> SimulationRuns { get; set; } = new List<SimulationRun>();
}
