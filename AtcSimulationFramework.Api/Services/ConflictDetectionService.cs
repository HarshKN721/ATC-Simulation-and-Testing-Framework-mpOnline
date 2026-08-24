using AtcSimulationFramework.Api.Entities;
using AtcSimulationFramework.Api.Interfaces;

namespace AtcSimulationFramework.Api.Services;

public class ConflictDetectionService : IConflictDetectionService
{
    // Threshold distance in degrees (approx ~ 3 NM) and altitude in feet
    private const double DistanceThreshold = 0.05;
    private const double AltitudeThreshold = 1000.0;

    public IEnumerable<ConflictEvent> DetectConflicts(SimulationRun run)
    {
        if (run == null || run.Aircraft == null || run.Aircraft.Count < 2)
        {
            return Enumerable.Empty<ConflictEvent>();
        }

        var newConflicts = new List<ConflictEvent>();
        var aircraftList = run.Aircraft.ToList();

        for (int i = 0; i < aircraftList.Count; i++)
        {
            for (int j = i + 1; j < aircraftList.Count; j++)
            {
                var a1 = aircraftList[i];
                var a2 = aircraftList[j];

                double dLat = a1.Latitude - a2.Latitude;
                double dLon = a1.Longitude - a2.Longitude;
                double distance = Math.Sqrt((dLat * dLat) + (dLon * dLon));
                double altDiff = Math.Abs(a1.Altitude - a2.Altitude);

                if (distance <= DistanceThreshold && altDiff <= AltitudeThreshold)
                {
                    // Check if an unresolved conflict event already exists for these aircraft in this run
                    bool existingUnresolved = run.ConflictEvents.Any(c =>
                        !c.IsResolved &&
                        ((c.AircraftAId == a1.Id && c.AircraftBId == a2.Id) ||
                         (c.AircraftAId == a2.Id && c.AircraftBId == a1.Id)));

                    if (!existingUnresolved)
                    {
                        newConflicts.Add(new ConflictEvent
                        {
                            SimulationRunId = run.Id,
                            AircraftAId = a1.Id,
                            AircraftBId = a2.Id,
                            AircraftA = a1,
                            AircraftB = a2,
                            Timestamp = DateTime.UtcNow,
                            Distance = Math.Round(distance, 4),
                            IsResolved = false
                        });
                    }
                }
            }
        }

        return newConflicts;
    }
}
