using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtcSimulationFramework.Api.Services.ConflictDetection;

public class ConflictDetectionService : IConflictDetectionService
{
    private readonly AtcDbContext _dbContext;

    public ConflictDetectionService(AtcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ProcessConflictsAsync(
        SimulationRun run,
        IList<Aircraft> aircraftList,
        long currentTick,
        CancellationToken cancellationToken = default)
    {
        if (aircraftList.Count < 2)
        {
            return;
        }

        var activeConflicts = await _dbContext.ConflictEvents
            .Where(c => c.SimulationRunId == run.Id && !c.IsResolved)
            .ToListAsync(cancellationToken);

        var conflictedAircraftIds = new HashSet<int>();

        for (var i = 0; i < aircraftList.Count; i++)
        {
            var primary = aircraftList[i];
            if (!IsEligibleForConflict(primary))
            {
                continue;
            }

            for (var j = i + 1; j < aircraftList.Count; j++)
            {
                var secondary = aircraftList[j];
                if (!IsEligibleForConflict(secondary))
                {
                    continue;
                }

                ProcessPair(run.Id, primary, secondary, currentTick, activeConflicts, conflictedAircraftIds);
            }
        }

        CheckResolutions(aircraftList, currentTick, activeConflicts, conflictedAircraftIds);
        UpdateAircraftStatuses(aircraftList, conflictedAircraftIds);
    }

    private static bool IsEligibleForConflict(Aircraft aircraft)
    {
        return aircraft.Status != AircraftStatus.Landed;
    }

    private void ProcessPair(
        int runId,
        Aircraft primary,
        Aircraft secondary,
        long currentTick,
        List<ConflictEvent> activeConflicts,
        HashSet<int> conflictedAircraftIds)
    {
        var lateralDistNm = AviationMath.CalculateHaversineDistanceNm(
            primary.CurrentLatitude, primary.CurrentLongitude,
            secondary.CurrentLatitude, secondary.CurrentLongitude);

        var verticalDiffFt = AviationMath.CalculateAltitudeDifference(
            primary.CurrentAltitudeFeet, secondary.CurrentAltitudeFeet);

        var isLos = SeparationStandards.IsLossOfSeparation(lateralDistNm, verticalDiffFt);
        var existingConflict = FindActiveConflictForPair(activeConflicts, primary.Id, secondary.Id);

        if (isLos)
        {
            conflictedAircraftIds.Add(primary.Id);
            conflictedAircraftIds.Add(secondary.Id);

            if (existingConflict != null)
            {
                UpdateExistingConflict(existingConflict, lateralDistNm, verticalDiffFt);
            }
            else
            {
                CreateNewConflict(runId, primary.Id, secondary.Id, lateralDistNm, verticalDiffFt, currentTick, activeConflicts);
            }
        }
    }

    private static ConflictEvent? FindActiveConflictForPair(
        IEnumerable<ConflictEvent> activeConflicts,
        int idA,
        int idB)
    {
        return activeConflicts.FirstOrDefault(c =>
            (c.PrimaryAircraftId == idA && c.SecondaryAircraftId == idB) ||
            (c.PrimaryAircraftId == idB && c.SecondaryAircraftId == idA));
    }

    private static void UpdateExistingConflict(
        ConflictEvent conflict,
        double lateralDistNm,
        double verticalDiffFt)
    {
        conflict.DistanceNauticalMiles = lateralDistNm;
        conflict.AltitudeDifferenceFeet = verticalDiffFt;
        conflict.Severity = SeparationStandards.DetermineSeverity(lateralDistNm, verticalDiffFt);
    }

    private void CreateNewConflict(
        int runId,
        int primaryId,
        int secondaryId,
        double lateralDistNm,
        double verticalDiffFt,
        long currentTick,
        List<ConflictEvent> activeConflicts)
    {
        var newConflict = new ConflictEvent
        {
            SimulationRunId = runId,
            PrimaryAircraftId = primaryId,
            SecondaryAircraftId = secondaryId,
            ConflictType = ConflictType.LossOfSeparation,
            Severity = SeparationStandards.DetermineSeverity(lateralDistNm, verticalDiffFt),
            DistanceNauticalMiles = lateralDistNm,
            AltitudeDifferenceFeet = verticalDiffFt,
            DetectedAtTick = currentTick,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ConflictEvents.Add(newConflict);
        activeConflicts.Add(newConflict);
    }

    private static void CheckResolutions(
        IList<Aircraft> aircraftList,
        long currentTick,
        IEnumerable<ConflictEvent> activeConflicts,
        HashSet<int> conflictedAircraftIds)
    {
        var aircraftMap = aircraftList.ToDictionary(a => a.Id);

        foreach (var conflict in activeConflicts.Where(c => !c.IsResolved))
        {
            if (!aircraftMap.TryGetValue(conflict.PrimaryAircraftId, out var primary) ||
                !aircraftMap.TryGetValue(conflict.SecondaryAircraftId, out var secondary))
            {
                continue;
            }

            var lateralDistNm = AviationMath.CalculateHaversineDistanceNm(
                primary.CurrentLatitude, primary.CurrentLongitude,
                secondary.CurrentLatitude, secondary.CurrentLongitude);

            var verticalDiffFt = AviationMath.CalculateAltitudeDifference(
                primary.CurrentAltitudeFeet, secondary.CurrentAltitudeFeet);

            if (SeparationStandards.IsConflictResolved(lateralDistNm, verticalDiffFt))
            {
                ResolveConflict(conflict, currentTick);
            }
        }
    }

    private static void ResolveConflict(ConflictEvent conflict, long currentTick)
    {
        conflict.IsResolved = true;
        conflict.ResolvedAtTick = currentTick;
        conflict.ResolvedAt = DateTime.UtcNow;
    }

    private static void UpdateAircraftStatuses(
        IEnumerable<Aircraft> aircraftList,
        HashSet<int> conflictedAircraftIds)
    {
        foreach (var aircraft in aircraftList)
        {
            if (aircraft.Status == AircraftStatus.Landed || aircraft.Status == AircraftStatus.Emergency)
            {
                continue;
            }

            aircraft.Status = conflictedAircraftIds.Contains(aircraft.Id)
                ? AircraftStatus.Conflict
                : AircraftStatus.Airborne;
        }
    }
}
