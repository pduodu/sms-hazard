using SMSHazard.Domain.Enums;

namespace SMSHazard.Domain.Hazards;

/// <summary>Raised when a hazard is driven into an illegal lifecycle state.</summary>
public sealed class InvalidHazardTransitionException : Exception
{
    public HazardStatus From { get; }
    public HazardStatus To { get; }

    public InvalidHazardTransitionException(HazardStatus from, HazardStatus to)
        : base($"Illegal hazard transition: {from} -> {to}.")
    {
        From = from;
        To = to;
    }
}
