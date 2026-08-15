using SMSHazard.Domain.Common;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.Hazards;

namespace SMSHazard.Domain.Entities;

/// <summary>
/// Aggregate root for the safety loop. Owns its assessments, corrective actions and attachments,
/// and enforces its own lifecycle transitions (illegal transitions are rejected here, not in the UI).
/// </summary>
public class HazardReport : BaseEntity
{
    public string ReferenceNo { get; set; } = string.Empty; // e.g. HZ-2026-0001
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int HazardCategoryId { get; set; }
    public HazardCategory? HazardCategory { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string ReportedById { get; set; } = string.Empty;
    public DateTime ReportedDate { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public string? ImmediateActionTaken { get; set; }

    /// <summary>True when submitted through the public (unauthenticated) channel. ReportedById is then empty.</summary>
    public bool IsAnonymous { get; set; }

    /// <summary>Opaque code an anonymous reporter uses to track status. Null for authenticated reports.</summary>
    public string? TrackingCode { get; set; }

    public HazardStatus Status { get; private set; } = HazardStatus.Reported;

    public ICollection<RiskAssessment> Assessments { get; set; } = new List<RiskAssessment>();
    public ICollection<CorrectiveAction> CorrectiveActions { get; set; } = new List<CorrectiveAction>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    /// <summary>
    /// Transition to a new status, enforcing the domain state machine.
    /// Throws <see cref="InvalidHazardTransitionException"/> if the move is not permitted.
    /// </summary>
    public void TransitionTo(HazardStatus target)
    {
        if (!HazardStateMachine.CanTransition(Status, target))
            throw new InvalidHazardTransitionException(Status, target);
        Status = target;
    }

    /// <summary>Allows the seeder/persistence to set an initial status without transition checks.</summary>
    public void SetInitialStatus(HazardStatus status) => Status = status;
}
