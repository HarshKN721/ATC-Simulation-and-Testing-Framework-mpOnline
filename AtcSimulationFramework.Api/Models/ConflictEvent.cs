using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtcSimulationFramework.Api.Models;

[Table("ConflictEvents")]
public class ConflictEvent
{
    [Key]
    public int Id { get; set; }

    public int SimulationRunId { get; set; }

    public int PrimaryAircraftId { get; set; }

    public int SecondaryAircraftId { get; set; }

    public ConflictType ConflictType { get; set; } = ConflictType.LossOfSeparation;

    public ConflictSeverity Severity { get; set; } = ConflictSeverity.Medium;

    public double DistanceNauticalMiles { get; set; }

    public double AltitudeDifferenceFeet { get; set; }

    public long DetectedAtTick { get; set; }

    public long? ResolvedAtTick { get; set; }

    public bool IsResolved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SimulationRunId))]
    public virtual SimulationRun SimulationRun { get; set; } = null!;

    [ForeignKey(nameof(PrimaryAircraftId))]
    public virtual Aircraft PrimaryAircraft { get; set; } = null!;

    [ForeignKey(nameof(SecondaryAircraftId))]
    public virtual Aircraft SecondaryAircraft { get; set; } = null!;
}
