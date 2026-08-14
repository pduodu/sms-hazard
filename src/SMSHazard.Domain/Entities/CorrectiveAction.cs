using SMSHazard.Domain.Common;
using SMSHazard.Domain.Enums;

namespace SMSHazard.Domain.Entities;

public class CorrectiveAction : BaseEntity
{
    public int HazardReportId { get; set; }
    public HazardReport? HazardReport { get; set; }

    public string Description { get; set; } = string.Empty;
    public CapaType Type { get; set; }
    public string AssignedToId { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public CapaStatus Status { get; set; } = CapaStatus.Open;

    public string? ProgressNote { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? VerifiedById { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public string? EffectivenessNote { get; set; }

    /// <summary>Idempotency guard for the reminder job (never double-remind).</summary>
    public DateTime? LastRemindedAt { get; set; }

    /// <summary>Derived: overdue when past due and not yet completed/verified.</summary>
    public bool IsOverdue(DateTime asOf) =>
        DueDate.Date < asOf.Date && Status is not (CapaStatus.Completed or CapaStatus.Verified);
}
