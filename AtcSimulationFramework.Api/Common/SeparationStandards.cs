using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Common;

public static class SeparationStandards
{
    public const double StandardLateralSeparationNm = 5.0;
    public const double StandardVerticalSeparationFt = 1000.0;

    // Hysteresis buffers to avoid rapid conflict flipping
    public const double ResolutionLateralBufferNm = 5.2;
    public const double ResolutionVerticalBufferFt = 1050.0;

    public static bool IsLossOfSeparation(double lateralDistanceNm, double verticalDistanceFt)
    {
        return lateralDistanceNm < StandardLateralSeparationNm &&
               verticalDistanceFt < StandardVerticalSeparationFt;
    }

    public static bool IsConflictResolved(double lateralDistanceNm, double verticalDistanceFt)
    {
        return lateralDistanceNm >= ResolutionLateralBufferNm ||
               verticalDistanceFt >= ResolutionVerticalBufferFt;
    }

    public static ConflictSeverity DetermineSeverity(double lateralDistanceNm, double verticalDistanceFt)
    {
        if (lateralDistanceNm < 2.0 && verticalDistanceFt < 500.0)
        {
            return ConflictSeverity.Critical;
        }

        if (lateralDistanceNm < 3.5 && verticalDistanceFt < 800.0)
        {
            return ConflictSeverity.Medium;
        }

        return ConflictSeverity.Low;
    }
}
