using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.ValueObjects;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

public sealed class RiskService : IRiskService
{
    private readonly AppDbContext _db;

    public RiskService(AppDbContext db) => _db = db;

    public async Task<(int Score, RiskLevel Level)?> AssessAsync(
        int hazardId, int likelihood, int severity, string rationale,
        string assessorId, bool isResidual, CancellationToken ct = default)
    {
        var hazard = await _db.HazardReports
            .Include(h => h.Assessments)
            .FirstOrDefaultAsync(h => h.Id == hazardId, ct);
        if (hazard is null) return null;

        // Domain value object validates the 1–5 inputs and computes score/level.
        var score = new RiskScore(likelihood, severity);

        var now = DateTime.UtcNow;
        var assessment = new RiskAssessment
        {
            HazardReportId = hazard.Id,
            Likelihood = likelihood,
            Severity = severity,
            Rationale = rationale.Trim(),
            AssessedById = assessorId,
            AssessedDate = now,
            IsResidual = isResidual,
            CreatedAt = now
        };
        assessment.ApplyScore();
        hazard.Assessments.Add(assessment);

        // Initial assessment advances the lifecycle through the domain state machine.
        if (!isResidual)
        {
            if (hazard.Status == HazardStatus.Reported)
                hazard.TransitionTo(HazardStatus.UnderAssessment);
            if (hazard.Status == HazardStatus.UnderAssessment)
                hazard.TransitionTo(HazardStatus.ActionRequired);
        }

        await _db.SaveChangesAsync(ct);
        return (assessment.RiskScoreValue, assessment.RiskLevel);
    }
}
