namespace AtcSimulationFramework.Services;

using AtcSimulationFramework.Models;

public class ConflictDetector
{
    private const double EarthRadiusNm = 3440.065;
    private const double HorizontalSeparationLimitNm = 5.0;
    private const double VerticalSeparationLimitFt = 1000.0;

    public List<ConflictEvent> DetectConflicts(List<Aircraft> aircraft)
    {
        var conflicts = new List<ConflictEvent>();

        if (aircraft == null || aircraft.Count < 2)
        {
            return conflicts;
        }

        for (int i = 0; i < aircraft.Count; i++)
        {
            for (int j = i + 1; j < aircraft.Count; j++)
            {
                var a = aircraft[i];
                var b = aircraft[j];

                double horizontalDist = HaversineDistanceNm(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
                int verticalDist = Math.Abs(a.AltitudeFt - b.AltitudeFt);

                if (IsConflict(horizontalDist, verticalDist))
                {
                    conflicts.Add(new ConflictEvent
                    {
                        RunId = a.RunId,
                        AircraftAId = a.AircraftId,
                        AircraftBId = b.AircraftId,
                        DetectedAt = DateTime.UtcNow,
                        HorizontalDistNm = (decimal)Math.Round(horizontalDist, 3),
                        VerticalDistFt = verticalDist
                    });
                }
            }
        }

        return conflicts;
    }

    private static bool IsConflict(double horizontalDistNm, int verticalDistFt)
    {
        return horizontalDistNm < HorizontalSeparationLimitNm && verticalDistFt < VerticalSeparationLimitFt;
    }

    public static double HaversineDistanceNm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double rLat1 = DegreesToRadians(lat1);
        double rLat2 = DegreesToRadians(lat2);

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(rLat1) * Math.Cos(rLat2) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return EarthRadiusNm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
