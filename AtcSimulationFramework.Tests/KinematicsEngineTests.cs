using AtcSimulationFramework.Api.Models;
using AtcSimulationFramework.Api.Services.Kinematics;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class KinematicsEngineTests
{
    private readonly KinematicsEngine _engine = new();

    [Fact]
    public void AdvanceAircraftPhysics_UpdatesHeadingTowardsTarget()
    {
        var aircraft = new Aircraft
        {
            CurrentLatitude = 40.0,
            CurrentLongitude = -74.0,
            CurrentHeadingDegrees = 0.0,
            TargetHeadingDegrees = 90.0,
            CurrentAltitudeFeet = 10000,
            TargetAltitudeFeet = 10000,
            CurrentSpeedKnots = 300,
            TargetSpeedKnots = 300
        };

        _engine.AdvanceAircraftPhysics(aircraft, 1.0);

        Assert.Equal(3.0, aircraft.CurrentHeadingDegrees, 2);
    }

    [Fact]
    public void AdvanceAircraftPhysics_UpdatesAltitudeTowardsTarget()
    {
        var aircraft = new Aircraft
        {
            CurrentLatitude = 40.0,
            CurrentLongitude = -74.0,
            CurrentHeadingDegrees = 90.0,
            TargetHeadingDegrees = 90.0,
            CurrentAltitudeFeet = 10000,
            TargetAltitudeFeet = 12000,
            VerticalSpeedFpm = 1200,
            CurrentSpeedKnots = 300,
            TargetSpeedKnots = 300
        };

        // 1 second at 1200 fpm = 20 ft climb
        _engine.AdvanceAircraftPhysics(aircraft, 1.0);

        Assert.Equal(10020.0, aircraft.CurrentAltitudeFeet, 1);
        Assert.Equal(1200.0, aircraft.VerticalSpeedFpm, 1);
    }

    [Fact]
    public void AdvanceAircraftPhysics_UpdatesPositionAlongHeading()
    {
        var aircraft = new Aircraft
        {
            CurrentLatitude = 0.0,
            CurrentLongitude = 0.0,
            CurrentHeadingDegrees = 90.0, // Flying East
            TargetHeadingDegrees = 90.0,
            CurrentAltitudeFeet = 10000,
            TargetAltitudeFeet = 10000,
            CurrentSpeedKnots = 360, // 360 kts = 0.1 NM/sec
            TargetSpeedKnots = 360
        };

        _engine.AdvanceAircraftPhysics(aircraft, 10.0); // 10 sec = 1 NM East

        Assert.Equal(0.0, aircraft.CurrentLatitude, 2);
        Assert.True(aircraft.CurrentLongitude > 0.0);
    }
}
