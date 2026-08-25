using AtcSimulationFramework.Api.Common;
using AtcSimulationFramework.Api.Models;
using Xunit;

namespace AtcSimulationFramework.Tests;

public class SeparationStandardsTests
{
    [Theory]
    [InlineData(4.9, 900, true)]
    [InlineData(5.1, 900, false)] // Lateral safe
    [InlineData(4.0, 1100, false)] // Vertical safe
    [InlineData(10.0, 2000, false)]
    public void IsLossOfSeparation_EvaluatesMinimaCorrectly(double lateralNm, double verticalFt, bool expected)
    {
        var result = SeparationStandards.IsLossOfSeparation(lateralNm, verticalFt);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1.5, 400, ConflictSeverity.Critical)]
    [InlineData(3.0, 700, ConflictSeverity.Medium)]
    [InlineData(4.5, 950, ConflictSeverity.Low)]
    public void DetermineSeverity_EvaluatesSeverityCorrectly(double lateralNm, double verticalFt, ConflictSeverity expected)
    {
        var result = SeparationStandards.DetermineSeverity(lateralNm, verticalFt);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5.3, 500, true)]
    [InlineData(2.0, 1200, true)]
    [InlineData(3.0, 600, false)]
    public void IsConflictResolved_EvaluatesHysteresisBuffer(double lateralNm, double verticalFt, bool expected)
    {
        var result = SeparationStandards.IsConflictResolved(lateralNm, verticalFt);
        Assert.Equal(expected, result);
    }
}
