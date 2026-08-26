namespace AtcSimulationFramework.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SimulationRun
{
    [Key]
    public int RunId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("CreatedByUserId")]
    public AppUser? CreatedByUser { get; set; }

    public ICollection<Aircraft> Aircraft { get; set; } = new List<Aircraft>();
    public ICollection<ConflictEvent> ConflictEvents { get; set; } = new List<ConflictEvent>();
}
