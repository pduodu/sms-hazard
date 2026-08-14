using SMSHazard.Domain.Common;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.ValueObjects;

namespace SMSHazard.Domain.Entities;

/// <summary>An initial or residual risk assessment for a hazard.</summary>
public class RiskAssessment : BaseEntity
{
    public int HazardReportId { get; set; }
    public HazardReport? HazardReport { get; set; }

    public int Likelihood { get; set; }   // 1-5
    public int Severity { get; set; }     // 1-5
    public int RiskScoreValue { get; set; } // persisted computed score (1-25)
    public RiskLevel RiskLevel { get; set; }

    public string Rationale { get; set; } = string.Empty;
    public string AssessedById { get; set; } = string.Empty;
    public DateTime AssessedDate { get; set; }

    /// <summary>False = initial assessment; True = residual (post-mitigation) re-assessment.</summary>
    public bool IsResidual { get; set; }

    /// <summary>Recompute score/level from likelihood &amp; severity via the domain value object.</summary>
    public void ApplyScore()
    {
        var rs = new RiskScore(Likelihood, Severity);
        RiskScoreValue = rs.Score;
        RiskLevel = rs.Level;
    }
}
