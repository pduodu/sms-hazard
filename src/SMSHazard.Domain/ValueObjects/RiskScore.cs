using SMSHazard.Domain.Enums;

namespace SMSHazard.Domain.ValueObjects;

/// <summary>
/// Immutable value object encapsulating the 5x5 risk matrix.
/// Risk Score = Likelihood (1-5) x Severity (1-5), range 1-25, banded into a <see cref="RiskLevel"/>.
/// Pure, deterministic, dependency-free — the primary unit-test target.
/// </summary>
public sealed record RiskScore
{
    public const int Min = 1;
    public const int Max = 5;

    public int Likelihood { get; }
    public int Severity { get; }
    public int Score => Likelihood * Severity;
    public RiskLevel Level => BandFor(Score);

    public RiskScore(int likelihood, int severity)
    {
        if (likelihood is < Min or > Max)
            throw new ArgumentOutOfRangeException(nameof(likelihood), likelihood,
                $"Likelihood must be between {Min} and {Max}.");
        if (severity is < Min or > Max)
            throw new ArgumentOutOfRangeException(nameof(severity), severity,
                $"Severity must be between {Min} and {Max}.");

        Likelihood = likelihood;
        Severity = severity;
    }

    /// <summary>Bands a raw score (1-25) into a risk level per the SMS 5x5 matrix.</summary>
    public static RiskLevel BandFor(int score) => score switch
    {
        >= 1 and <= 4 => RiskLevel.Low,
        >= 5 and <= 9 => RiskLevel.Medium,
        >= 10 and <= 15 => RiskLevel.High,
        >= 16 and <= 25 => RiskLevel.Extreme,
        _ => throw new ArgumentOutOfRangeException(nameof(score), score, "Score must be between 1 and 25.")
    };

    /// <summary>Bootstrap/CSS colour hint used consistently across the UI.</summary>
    public static string ColourFor(RiskLevel level) => level switch
    {
        RiskLevel.Low => "success",
        RiskLevel.Medium => "warning",
        RiskLevel.High => "orange",
        RiskLevel.Extreme => "danger",
        _ => "secondary"
    };

    public override string ToString() => $"{Score} ({Level})";
}
