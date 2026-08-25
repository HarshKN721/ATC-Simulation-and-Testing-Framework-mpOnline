using AtcSimulationFramework.Api.Common;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class AviationMathTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(360, 0)]
    [InlineData(370, 10)]
    [InlineData(-10, 350)]
    [InlineData(-370, 350)]
    [InlineData(720, 0)]
    public void NormalizeHeading_ShouldReturnDegreesInRange0To360(double input, double expected)
    {
        var result = AviationMath.NormalizeHeading(input);
        Assert.Equal(expected, result, 2);
    }

    [Theory]
    [InlineData(0, 90, 90)]
    [InlineData(90, 0, -90)]
    [InlineData(10, 350, -20)]
    [InlineData(350, 10, 20)]
    [InlineData(180, 180, 0)]
    public void CalculateHeadingDifference_ShouldReturnShortestTurnAngle(double current, double target, double expected)
    {
        var result = AviationMath.CalculateHeadingDifference(current, target);
        Assert.Equal(expected, result, 2);
    }

    [Fact]
    public void CalculateHaversineDistanceNm_SameCoordinates_ReturnsZero()
    {
        var distance = AviationMath.CalculateHaversineDistanceNm(40.7128, -74.0060, 40.7128, -74.0060);
        Assert.Equal(0.0, distance, 3);
    }

    [Fact]
    public void CalculateHaversineDistanceNm_KnownDistance_ReturnsExpectedNauticalMiles()
    {
        // New York (40.7128, -74.0060) to London (51.5074, -0.1278) ~ 3000 NM
        var distance = AviationMath.CalculateHaversineDistanceNm(40.7128, -74.0060, 51.5074, -0.1278);
        Assert.InRange(distance, 2900, 3100);
    }

    [Fact]
    public void ProjectPosition_MovesAircraftInCorrectDirection()
    {
        var (newLat, newLon) = AviationMath.ProjectPosition(0.0, 0.0, 90.0, 60.0); // 60 NM East along equator
        Assert.Equal(0.0, newLat, 2);
        Assert.True(newLon > 0.0);
    }

    [Fact]
    public void CalculateTurnStep_TurnsAtStandardRate()
    {
        // Turning from 0 to 90 at 3 deg/sec for 2 seconds -> heading should be 6
        var heading = AviationMath.CalculateTurnStep(0.0, 90.0, 3.0, 2.0);
        Assert.Equal(6.0, heading, 2);
    }

    [Fact]
    public void CalculateAltitudeStep_ClimbsAtExpectedRate()
    {
        // Climb from 10,000 to 20,000 ft at 1500 fpm for 2 seconds (50 ft)
        var (newAlt, vspeed) = AviationMath.CalculateAltitudeStep(10000.0, 20000.0, 1500.0, 2.0);
        Assert.Equal(10050.0, newAlt, 1);
        Assert.Equal(1500.0, vspeed, 1);
    }

    [Fact]
    public void CalculateSpeedStep_AcceleratesTowardsTarget()
    {
        // Accel from 250 to 300 at 2.5 kts/sec for 4 seconds (10 kts delta) -> 260
        var speed = AviationMath.CalculateSpeedStep(250.0, 300.0, 2.5, 4.0);
        Assert.Equal(260.0, speed, 1);
    }
}
