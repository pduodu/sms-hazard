namespace SMSHazard.Domain.Enums;

/// <summary>Lifecycle states for a hazard report. Transitions are enforced in the Domain.</summary>
public enum HazardStatus
{
    Reported = 0,
    UnderAssessment = 1,
    ActionRequired = 2,
    InProgress = 3,
    UnderVerification = 4,
    Closed = 5,
    Rejected = 6
}
