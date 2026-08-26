namespace AtcSimulationFramework.Tests;

using Xunit;
using AtcSimulationFramework.Models;
using AtcSimulationFramework.Services;

public class ConflictDetectorTests
{
    private readonly ConflictDetector _detector = new();

    [Fact]
    public void DetectConflicts_AircraftViolatingHorizontalAndVerticalLimits_ReturnsConflict()
    {
        var aircraftList = new List<Aircraft>
        {
            new() { AircraftId = 1, RunId = 10, Callsign = "AI101", Latitude = 28.6139, Longitude = 77.2090, AltitudeFt = 30000 },
            new() { AircraftId = 2, RunId = 10, Callsign = "6E202", Latitude = 28.6140, Longitude = 77.2091, AltitudeFt = 30500 }
        };

        var conflicts = _detector.DetectConflicts(aircraftList);

        Assert.Single(conflicts);
        Assert.Equal(10, conflicts[0].RunId);
        Assert.Equal(1, conflicts[0].AircraftAId);
        Assert.Equal(2, conflicts[0].AircraftBId);
        Assert.True(conflicts[0].HorizontalDistNm < 5.0m);
        Assert.True(conflicts[0].VerticalDistFt < 1000);
    }

    [Fact]
    public void DetectConflicts_AircraftSeparatedVertically_ReturnsNoConflict()
    {
        var aircraftList = new List<Aircraft>
        {
            new() { AircraftId = 1, RunId = 10, Callsign = "AI101", Latitude = 28.6139, Longitude = 77.2090, AltitudeFt = 30000 },
            new() { AircraftId = 2, RunId = 10, Callsign = "6E202", Latitude = 28.6140, Longitude = 77.2091, AltitudeFt = 32000 }
        };

        var conflicts = _detector.DetectConflicts(aircraftList);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void DetectConflicts_AircraftSeparatedHorizontally_ReturnsNoConflict()
    {
        var aircraftList = new List<Aircraft>
        {
            new() { AircraftId = 1, RunId = 10, Callsign = "AI101", Latitude = 28.6139, Longitude = 77.2090, AltitudeFt = 30000 },
            new() { AircraftId = 2, RunId = 10, Callsign = "6E202", Latitude = 29.5000, Longitude = 78.5000, AltitudeFt = 30000 }
        };

        var conflicts = _detector.DetectConflicts(aircraftList);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void HaversineDistanceNm_SameCoordinates_ReturnsZero()
    {
        double distance = ConflictDetector.HaversineDistanceNm(28.6139, 77.2090, 28.6139, 77.2090);
        Assert.Equal(0.0, distance, 3);
    }
}
