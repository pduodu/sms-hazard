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
    private readonly INotificationService _notify;

    public RiskService(AppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

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

        if (!isResidual)
            await _notify.NotifyUserAsync(hazard.ReportedById,
                $"Hazard {hazard.ReferenceNo} assessed",
                $"Your reported hazard was assessed: risk {assessment.RiskScoreValue} ({assessment.RiskLevel}).",
                $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return (assessment.RiskScoreValue, assessment.RiskLevel);
    }
}
