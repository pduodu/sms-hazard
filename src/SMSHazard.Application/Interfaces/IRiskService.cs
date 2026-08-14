using SMSHazard.Domain.Enums;

namespace SMSHazard.Application.Interfaces;

public interface IRiskService
{
    /// <summary>
    /// Records a risk assessment (initial or residual) for a hazard, advancing the lifecycle
    /// on an initial assessment (Reported → Under Assessment → Action Required).
    /// Returns the computed score/level, or null if the hazard does not exist.
    /// </summary>
    Task<(int Score, RiskLevel Level)?> AssessAsync(
        int hazardId, int likelihood, int severity, string rationale,
        string assessorId, bool isResidual, CancellationToken ct = default);
}
