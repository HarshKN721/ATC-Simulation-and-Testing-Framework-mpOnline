namespace AtcSimulationFramework.Services;

using AtcSimulationFramework.Models;

/// <summary>
/// Reusable kinematic helpers for aircraft position updates.
/// All methods are static so they can be called from the tick service,
/// unit tests, or any future service without instantiation.
/// </summary>
public static class AircraftKinematics
{
    /// <summary>
    /// Knots → degrees-of-latitude per second.
    /// 1 knot ≈ 1.68781 ft/s; 1° latitude ≈ 364,567 ft  (≈ 60 NM).
    /// Combined constant: 1 / (3600 × 60) = 1 / 216_000 ≈ 4.6296×10⁻⁶.
    /// </summary>
    private const double KnotsToDegreesPerSecond = 1.0 / 216_000.0;

    /// <summary>
    /// Advances the aircraft's Latitude and Longitude by the distance it
    /// would cover in <paramref name="deltaTimeSeconds"/> at its current
    /// <see cref="Aircraft.SpeedKts"/> and <see cref="Aircraft.HeadingDeg"/>.
    /// <para>
    /// The calculation uses a flat-Earth approximation with a cosine
    /// correction for longitude (accurate enough for short simulation
    /// ticks and mid-latitude scenarios).
    /// </para>
    /// </summary>
    public static void UpdatePosition(Aircraft aircraft, double deltaTimeSeconds)
    {
        double headingRad = DegreesToRadians(aircraft.HeadingDeg);
        double distanceDeg = aircraft.SpeedKts * KnotsToDegreesPerSecond * deltaTimeSeconds;

        aircraft.Latitude  += distanceDeg * Math.Cos(headingRad);
        aircraft.Longitude += distanceDeg * Math.Sin(headingRad)
                              / CosineLatitudeFactor(aircraft.Latitude);
    }

    /// <summary>
    /// Returns cos(latitude) clamped to a small positive floor to avoid
    /// division-by-zero near the poles.
    /// </summary>
    private static double CosineLatitudeFactor(double latitudeDeg)
    {
        double cos = Math.Cos(DegreesToRadians(latitudeDeg));
        return Math.Max(cos, 1e-10);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
