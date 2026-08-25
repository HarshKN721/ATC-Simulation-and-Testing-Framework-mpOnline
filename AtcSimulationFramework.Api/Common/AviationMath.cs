namespace AtcSimulationFramework.Api.Common;

public static class AviationMath
{
    private const double EarthRadiusNm = 3440.065; // Earth radius in Nautical Miles
    public const double StandardTurnRateDegPerSec = 3.0; // Standard Rate One turn (3 deg/sec)
    public const double StandardClimbRateFpm = 1500.0;
    public const double StandardDescentRateFpm = 1500.0;
    public const double StandardAccelerationKtsPerSec = 2.5;

    public static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    public static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    public static double KnotsToNmPerSecond(double knots) => knots / 3600.0;

    public static double FeetPerMinuteToFeetPerSecond(double fpm) => fpm / 60.0;

    public static double NormalizeHeading(double heading)
    {
        var normalized = heading % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }
        return normalized;
    }

    public static double CalculateHeadingDifference(double currentHeading, double targetHeading)
    {
        var diff = (targetHeading - currentHeading + 180.0) % 360.0 - 180.0;
        return diff < -180.0 ? diff + 360.0 : diff;
    }

    public static double CalculateHaversineDistanceNm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var rLat1 = DegreesToRadians(lat1);
        var rLat2 = DegreesToRadians(lat2);

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0) * Math.Cos(rLat1) * Math.Cos(rLat2);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return EarthRadiusNm * c;
    }

    public static double CalculateBearingDegrees(double lat1, double lon1, double lat2, double lon2)
    {
        var rLat1 = DegreesToRadians(lat1);
        var rLat2 = DegreesToRadians(lat2);
        var dLon = DegreesToRadians(lon2 - lon1);

        var y = Math.Sin(dLon) * Math.Cos(rLat2);
        var x = Math.Cos(rLat1) * Math.Sin(rLat2) -
                Math.Sin(rLat1) * Math.Cos(rLat2) * Math.Cos(dLon);

        var initialBearing = Math.Atan2(y, x);
        return NormalizeHeading(RadiansToDegrees(initialBearing));
    }

    public static (double Latitude, double Longitude) ProjectPosition(
        double lat,
        double lon,
        double headingDeg,
        double distanceNm)
    {
        var angularDistance = distanceNm / EarthRadiusNm;
        var rHeading = DegreesToRadians(headingDeg);
        var rLat = DegreesToRadians(lat);
        var rLon = DegreesToRadians(lon);

        var newLat = Math.Asin(
            Math.Sin(rLat) * Math.Cos(angularDistance) +
            Math.Cos(rLat) * Math.Sin(angularDistance) * Math.Cos(rHeading));

        var newLon = rLon + Math.Atan2(
            Math.Sin(rHeading) * Math.Sin(angularDistance) * Math.Cos(rLat),
            Math.Cos(angularDistance) - Math.Sin(rLat) * Math.Sin(newLat));

        return (RadiansToDegrees(newLat), NormalizeLongitude(RadiansToDegrees(newLon)));
    }

    public static double NormalizeLongitude(double longitude)
    {
        var lon = (longitude + 180.0) % 360.0;
        if (lon < 0)
        {
            lon += 360.0;
        }
        return lon - 180.0;
    }

    public static double CalculateTurnStep(
        double currentHeading,
        double targetHeading,
        double maxTurnRateDegPerSec,
        double dtSeconds)
    {
        var diff = CalculateHeadingDifference(currentHeading, targetHeading);
        var maxTurn = maxTurnRateDegPerSec * dtSeconds;

        if (Math.Abs(diff) <= maxTurn)
        {
            return NormalizeHeading(targetHeading);
        }

        var turnDirection = Math.Sign(diff);
        return NormalizeHeading(currentHeading + turnDirection * maxTurn);
    }

    public static (double NewAltitude, double EffectiveVerticalSpeedFpm) CalculateAltitudeStep(
        double currentAltFt,
        double targetAltFt,
        double desiredVerticalSpeedFpm,
        double dtSeconds)
    {
        var altDiff = targetAltFt - currentAltFt;
        if (Math.Abs(altDiff) < 1.0)
        {
            return (targetAltFt, 0.0);
        }

        var configuredRate = desiredVerticalSpeedFpm > 0 ? desiredVerticalSpeedFpm : StandardClimbRateFpm;
        var direction = Math.Sign(altDiff);
        var climbRatePerSec = FeetPerMinuteToFeetPerSecond(configuredRate);
        var step = direction * climbRatePerSec * dtSeconds;

        if (Math.Abs(step) >= Math.Abs(altDiff))
        {
            return (targetAltFt, 0.0);
        }

        return (currentAltFt + step, direction * configuredRate);
    }

    public static double CalculateSpeedStep(
        double currentSpeedKts,
        double targetSpeedKts,
        double accelKtsPerSec,
        double dtSeconds)
    {
        var speedDiff = targetSpeedKts - currentSpeedKts;
        if (Math.Abs(speedDiff) < 0.5)
        {
            return targetSpeedKts;
        }

        var maxDelta = (accelKtsPerSec > 0 ? accelKtsPerSec : StandardAccelerationKtsPerSec) * dtSeconds;
        var direction = Math.Sign(speedDiff);

        if (Math.Abs(speedDiff) <= maxDelta)
        {
            return targetSpeedKts;
        }

        return currentSpeedKts + direction * maxDelta;
    }

    public static double CalculateAltitudeDifference(double alt1, double alt2)
    {
        return Math.Abs(alt1 - alt2);
    }
}
