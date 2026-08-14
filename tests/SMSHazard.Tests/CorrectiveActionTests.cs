using FluentAssertions;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using Xunit;

namespace SMSHazard.Tests;

public class CorrectiveActionTests
{
    private static readonly DateTime Today = new(2026, 8, 14);

    [Fact]
    public void Overdue_when_past_due_and_not_completed() // U-O1
    {
        var capa = new CorrectiveAction { DueDate = Today.AddDays(-1), Status = CapaStatus.InProgress };
        capa.IsOverdue(Today).Should().BeTrue();
    }

    [Fact]
    public void Not_overdue_when_due_in_future() // U-O2
    {
        var capa = new CorrectiveAction { DueDate = Today.AddDays(1), Status = CapaStatus.Open };
        capa.IsOverdue(Today).Should().BeFalse();
    }

    [Fact]
    public void Not_overdue_when_completed() // U-O3
    {
        var capa = new CorrectiveAction { DueDate = Today.AddDays(-1), Status = CapaStatus.Completed };
        capa.IsOverdue(Today).Should().BeFalse();
    }
}
