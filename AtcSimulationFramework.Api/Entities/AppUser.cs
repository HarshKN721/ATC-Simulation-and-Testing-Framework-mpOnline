using System.ComponentModel.DataAnnotations;

namespace AtcSimulationFramework.Api.Entities;

public class AppUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    // Navigation Properties
    public virtual ICollection<SimulationRun> SimulationRuns { get; set; } = new List<SimulationRun>();
}
