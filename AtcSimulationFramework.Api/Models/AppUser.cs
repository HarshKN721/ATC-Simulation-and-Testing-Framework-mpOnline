using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtcSimulationFramework.Api.Models;

[Table("AppUsers")]
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

    public UserRole Role { get; set; } = UserRole.Controller;

    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<SimulationRun> CreatedSimulations { get; set; } = new List<SimulationRun>();
    public virtual ICollection<VectorCommand> IssuedCommands { get; set; } = new List<VectorCommand>();
}
