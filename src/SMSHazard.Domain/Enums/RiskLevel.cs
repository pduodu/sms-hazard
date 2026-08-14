namespace SMSHazard.Domain.Enums;

/// <summary>Banded risk level derived from the 5x5 matrix score.</summary>
public enum RiskLevel
{
    Low = 0,       // score 1-4
    Medium = 1,    // score 5-9
    High = 2,      // score 10-15
    Extreme = 3    // score 16-25
}
