using FluentAssertions;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.ValueObjects;
using Xunit;

namespace SMSHazard.Tests;

public class RiskScoreTests
{
    [Theory]
    [InlineData(1, 1, 1, RiskLevel.Low)]      // U-R1 min
    [InlineData(4, 1, 4, RiskLevel.Low)]      // U-R2 Low upper boundary
    [InlineData(1, 5, 5, RiskLevel.Medium)]   // U-R3 Medium lower boundary
    [InlineData(3, 3, 9, RiskLevel.Medium)]   // U-R4 Medium upper boundary
    [InlineData(2, 5, 10, RiskLevel.High)]    // U-R5 High lower boundary
    [InlineData(5, 3, 15, RiskLevel.High)]    // U-R6 High upper boundary
    [InlineData(4, 4, 16, RiskLevel.Extreme)] // U-R7 Extreme lower boundary
    [InlineData(5, 5, 25, RiskLevel.Extreme)] // U-R8 max
    public void Computes_score_and_bands_level(int likelihood, int severity, int expectedScore, RiskLevel expectedLevel)
    {
        var rs = new RiskScore(likelihood, severity);
        rs.Score.Should().Be(expectedScore);
        rs.Level.Should().Be(expectedLevel);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(6, 3)]
    [InlineData(3, 0)]
    [InlineData(3, 6)]
    public void Rejects_out_of_range_inputs(int likelihood, int severity) // U-R9
    {
        var act = () => new RiskScore(likelihood, severity);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1, RiskLevel.Low)]
    [InlineData(4, RiskLevel.Low)]
    [InlineData(5, RiskLevel.Medium)]
    [InlineData(9, RiskLevel.Medium)]
    [InlineData(10, RiskLevel.High)]
    [InlineData(15, RiskLevel.High)]
    [InlineData(16, RiskLevel.Extreme)]
    [InlineData(25, RiskLevel.Extreme)]
    public void BandFor_maps_score_to_level(int score, RiskLevel expected)
        => RiskScore.BandFor(score).Should().Be(expected);
}
