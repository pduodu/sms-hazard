using FluentAssertions;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.Hazards;
using SMSHazard.Domain.ValueObjects;
using Xunit;

namespace SMSHazard.Tests;

public class RiskScoreColourTests
{
    [Theory]
    [InlineData(RiskLevel.Low, "success")]
    [InlineData(RiskLevel.Medium, "warning")]
    [InlineData(RiskLevel.High, "orange")]
    [InlineData(RiskLevel.Extreme, "danger")]
    public void ColourFor_maps_level_to_css(RiskLevel level, string expected)
        => RiskScore.ColourFor(level).Should().Be(expected);

    [Fact]
    public void ToString_shows_score_and_level()
        => new RiskScore(5, 5).ToString().Should().Be("25 (Extreme)");
}

public class RiskAssessmentTests
{
    [Fact]
    public void ApplyScore_computes_persisted_values()
    {
        var a = new RiskAssessment { Likelihood = 4, Severity = 4 };
        a.ApplyScore();
        a.RiskScoreValue.Should().Be(16);
        a.RiskLevel.Should().Be(RiskLevel.Extreme);
    }
}

public class HazardLifecycleTests
{
    [Fact]
    public void Happy_path_runs_report_to_closed()
    {
        var h = new HazardReport(); // Reported
        h.TransitionTo(HazardStatus.UnderAssessment);
        h.TransitionTo(HazardStatus.ActionRequired);
        h.TransitionTo(HazardStatus.InProgress);
        h.TransitionTo(HazardStatus.UnderVerification);
        h.TransitionTo(HazardStatus.Closed);
        h.Status.Should().Be(HazardStatus.Closed);
    }

    [Fact]
    public void Closed_can_be_reopened_to_action_required()
    {
        var h = new HazardReport();
        h.TransitionTo(HazardStatus.UnderAssessment);
        h.TransitionTo(HazardStatus.ActionRequired);
        h.TransitionTo(HazardStatus.InProgress);
        h.TransitionTo(HazardStatus.UnderVerification);
        h.TransitionTo(HazardStatus.Closed);
        h.TransitionTo(HazardStatus.ActionRequired); // reopen
        h.Status.Should().Be(HazardStatus.ActionRequired);
    }

    [Fact]
    public void InProgress_cannot_jump_to_closed()
    {
        var h = new HazardReport();
        h.TransitionTo(HazardStatus.UnderAssessment);
        h.TransitionTo(HazardStatus.ActionRequired);
        h.TransitionTo(HazardStatus.InProgress);
        var act = () => h.TransitionTo(HazardStatus.Closed);
        act.Should().Throw<InvalidHazardTransitionException>();
    }

    [Theory]
    [InlineData(HazardStatus.Reported, HazardStatus.UnderAssessment)]
    [InlineData(HazardStatus.Reported, HazardStatus.Rejected)]
    [InlineData(HazardStatus.UnderVerification, HazardStatus.Closed)]
    [InlineData(HazardStatus.UnderVerification, HazardStatus.ActionRequired)]
    public void NextStates_contains_allowed_targets(HazardStatus from, HazardStatus target)
        => HazardStateMachine.NextStates(from).Should().Contain(target);

    [Fact]
    public void Rejected_is_terminal()
        => HazardStateMachine.NextStates(HazardStatus.Rejected).Should().BeEmpty();
}

public class OverdueBoundaryTests
{
    private static readonly DateTime Today = new(2026, 8, 14);

    [Fact]
    public void Due_today_is_not_overdue()
    {
        var capa = new CorrectiveAction { DueDate = Today, Status = CapaStatus.InProgress };
        capa.IsOverdue(Today).Should().BeFalse();
    }

    [Fact]
    public void Verified_action_is_never_overdue()
    {
        var capa = new CorrectiveAction { DueDate = Today.AddDays(-10), Status = CapaStatus.Verified };
        capa.IsOverdue(Today).Should().BeFalse();
    }
}
