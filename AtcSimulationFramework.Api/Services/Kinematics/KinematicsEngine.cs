using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Services.Kinematics;

public class KinematicsEngine : IKinematicsEngine
{
    public void AdvanceAircraftPhysics(Aircraft aircraft, double dtSeconds)
    {
        if (aircraft.Status == AircraftStatus.Landed)
        {
            return;
        }

        UpdateHeading(aircraft, dtSeconds);
        UpdateAltitude(aircraft, dtSeconds);
        UpdateSpeed(aircraft, dtSeconds);
        UpdatePosition(aircraft, dtSeconds);

        aircraft.UpdatedAt = DateTime.UtcNow;
    }

    private static void UpdateHeading(Aircraft aircraft, double dtSeconds)
    {
        aircraft.CurrentHeadingDegrees = AviationMath.CalculateTurnStep(
            aircraft.CurrentHeadingDegrees,
            aircraft.TargetHeadingDegrees,
            AviationMath.StandardTurnRateDegPerSec,
            dtSeconds);
    }

    private static void UpdateAltitude(Aircraft aircraft, double dtSeconds)
    {
        var (newAltitude, effectiveVerticalSpeed) = AviationMath.CalculateAltitudeStep(
            aircraft.CurrentAltitudeFeet,
            aircraft.TargetAltitudeFeet,
            aircraft.VerticalSpeedFpm,
            dtSeconds);

        aircraft.CurrentAltitudeFeet = newAltitude;
        aircraft.VerticalSpeedFpm = effectiveVerticalSpeed;
    }

    private static void UpdateSpeed(Aircraft aircraft, double dtSeconds)
    {
        aircraft.CurrentSpeedKnots = AviationMath.CalculateSpeedStep(
            aircraft.CurrentSpeedKnots,
            aircraft.TargetSpeedKnots,
            AviationMath.StandardAccelerationKtsPerSec,
            dtSeconds);
    }

    private static void UpdatePosition(Aircraft aircraft, double dtSeconds)
    {
        var distanceNm = AviationMath.KnotsToNmPerSecond(aircraft.CurrentSpeedKnots) * dtSeconds;
        var (newLat, newLon) = AviationMath.ProjectPosition(
            aircraft.CurrentLatitude,
            aircraft.CurrentLongitude,
            aircraft.CurrentHeadingDegrees,
            distanceNm);

        aircraft.CurrentLatitude = newLat;
        aircraft.CurrentLongitude = newLon;
    }
}
