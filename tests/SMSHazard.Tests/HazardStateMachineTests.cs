using FluentAssertions;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.Hazards;
using Xunit;

namespace SMSHazard.Tests;

public class HazardStateMachineTests
{
    [Theory]
    [InlineData(HazardStatus.Reported, HazardStatus.UnderAssessment, true)]   // U-S1
    [InlineData(HazardStatus.Reported, HazardStatus.Closed, false)]           // U-S2 illegal skip
    [InlineData(HazardStatus.UnderVerification, HazardStatus.Closed, true)]   // U-S3
    [InlineData(HazardStatus.UnderVerification, HazardStatus.ActionRequired, true)] // U-S4 residual not acceptable
    [InlineData(HazardStatus.Closed, HazardStatus.ActionRequired, true)]      // U-S5 reopen
    [InlineData(HazardStatus.Reported, HazardStatus.Rejected, true)]          // U-S6
    [InlineData(HazardStatus.Rejected, HazardStatus.UnderAssessment, false)]  // terminal
    public void CanTransition_enforces_rules(HazardStatus from, HazardStatus to, bool expected)
        => HazardStateMachine.CanTransition(from, to).Should().Be(expected);

    [Fact]
    public void TransitionTo_throws_on_illegal_move()
    {
        var hazard = new HazardReport(); // starts Reported
        var act = () => hazard.TransitionTo(HazardStatus.Closed);
        act.Should().Throw<InvalidHazardTransitionException>();
        hazard.Status.Should().Be(HazardStatus.Reported); // unchanged
    }

    [Fact]
    public void TransitionTo_allows_legal_move()
    {
        var hazard = new HazardReport();
        hazard.TransitionTo(HazardStatus.UnderAssessment);
        hazard.Status.Should().Be(HazardStatus.UnderAssessment);
    }
}
