using SMSHazard.Domain.Enums;

namespace SMSHazard.Domain.Hazards;

/// <summary>
/// The single, authoritative definition of allowed hazard lifecycle transitions.
/// Enforced server-side so a crafted request cannot reach an illegal state.
/// </summary>
public static class HazardStateMachine
{
    private static readonly IReadOnlyDictionary<HazardStatus, HazardStatus[]> Allowed =
        new Dictionary<HazardStatus, HazardStatus[]>
        {
            [HazardStatus.Reported] = new[] { HazardStatus.UnderAssessment, HazardStatus.Rejected },
            [HazardStatus.UnderAssessment] = new[] { HazardStatus.ActionRequired, HazardStatus.Rejected },
            [HazardStatus.ActionRequired] = new[] { HazardStatus.InProgress },
            [HazardStatus.InProgress] = new[] { HazardStatus.UnderVerification, HazardStatus.ActionRequired },
            [HazardStatus.UnderVerification] = new[] { HazardStatus.Closed, HazardStatus.ActionRequired },
            [HazardStatus.Closed] = new[] { HazardStatus.ActionRequired }, // reopen on recurrence
            [HazardStatus.Rejected] = Array.Empty<HazardStatus>()
        };

    public static bool CanTransition(HazardStatus from, HazardStatus to) =>
        Allowed.TryGetValue(from, out var targets) && Array.IndexOf(targets, to) >= 0;

    public static IReadOnlyCollection<HazardStatus> NextStates(HazardStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<HazardStatus>();
}
