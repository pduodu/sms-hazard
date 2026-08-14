using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using SMSHazard.Domain.ValueObjects;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

public sealed class CapaService : ICapaService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    public CapaService(AppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public async Task<IReadOnlyList<UserOption>> GetAssignableUsersAsync(CancellationToken ct = default) =>
        await _db.Users.Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new UserOption(u.Id, u.FullName))
            .ToListAsync(ct);

    public async Task<bool> CreateAsync(CreateCapaRequest request, string officerId, CancellationToken ct = default)
    {
        var hazard = await _db.HazardReports
            .Include(h => h.CorrectiveActions)
            .FirstOrDefaultAsync(h => h.Id == request.HazardId, ct);
        if (hazard is null) return false;
        if (hazard.Status is not (HazardStatus.ActionRequired or HazardStatus.InProgress))
            return false;

        var now = DateTime.UtcNow;
        hazard.CorrectiveActions.Add(new CorrectiveAction
        {
            HazardReportId = hazard.Id,
            Description = request.Description.Trim(),
            Type = request.Type,
            AssignedToId = request.AssignedToId,
            DueDate = DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc),
            Status = CapaStatus.Open,
            CreatedAt = now
        });

        if (hazard.Status == HazardStatus.ActionRequired)
            hazard.TransitionTo(HazardStatus.InProgress);

        await _db.SaveChangesAsync(ct);

        await _notify.NotifyUserAsync(request.AssignedToId,
            $"Action assigned on {hazard.ReferenceNo}",
            $"You have been assigned a {request.Type} action, due {DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc):dd MMM yyyy}.",
            $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return true;
    }

    public async Task<IReadOnlyList<MyActionDto>> MyActionsAsync(string ownerId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.CorrectiveActions.AsNoTracking()
            .Where(c => c.AssignedToId == ownerId)
            .OrderBy(c => c.DueDate)
            .Select(c => new MyActionDto
            {
                CapaId = c.Id,
                HazardId = c.HazardReportId,
                HazardRef = c.HazardReport!.ReferenceNo,
                HazardTitle = c.HazardReport!.Title,
                Description = c.Description,
                Type = c.Type,
                DueDate = c.DueDate,
                Status = c.Status,
                IsOverdue = c.DueDate < today && c.Status != CapaStatus.Completed && c.Status != CapaStatus.Verified
            })
            .ToListAsync(ct);
    }

    public async Task<CapaEditDto?> GetForUpdateAsync(int capaId, CancellationToken ct = default) =>
        await _db.CorrectiveActions.AsNoTracking()
            .Where(c => c.Id == capaId)
            .Select(c => new CapaEditDto
            {
                CapaId = c.Id,
                HazardId = c.HazardReportId,
                HazardRef = c.HazardReport!.ReferenceNo,
                HazardTitle = c.HazardReport!.Title,
                Description = c.Description,
                AssignedToId = c.AssignedToId,
                Status = c.Status,
                ProgressNote = c.ProgressNote
            })
            .FirstOrDefaultAsync(ct);

    public async Task<bool> UpdateProgressAsync(int capaId, CapaStatus newStatus, string? note,
        string userId, bool isStaff, CancellationToken ct = default)
    {
        var capa = await _db.CorrectiveActions
            .Include(c => c.HazardReport!).ThenInclude(h => h.CorrectiveActions)
            .FirstOrDefaultAsync(c => c.Id == capaId, ct);
        if (capa is null) return false;
        if (!isStaff && capa.AssignedToId != userId) return false;

        // Owners may progress Open → In Progress → Completed (not Verified — that's the officer's step).
        if (newStatus == CapaStatus.Verified) return false;

        capa.Status = newStatus;
        capa.ProgressNote = note?.Trim();
        capa.UpdatedAt = DateTime.UtcNow;
        if (newStatus == CapaStatus.Completed)
            capa.CompletedDate = DateTime.UtcNow;

        var hazard = capa.HazardReport!;
        var allDone = hazard.CorrectiveActions.All(c =>
            c.Status == CapaStatus.Completed || c.Status == CapaStatus.Verified);
        var movedToVerification = allDone && hazard.Status == HazardStatus.InProgress;
        if (movedToVerification)
            hazard.TransitionTo(HazardStatus.UnderVerification);

        await _db.SaveChangesAsync(ct);

        if (movedToVerification)
            await _notify.NotifyRoleAsync("SafetyOfficer",
                $"Hazard {hazard.ReferenceNo} ready for verification",
                "All corrective actions are complete. Verify effectiveness and residual risk to close.",
                $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return true;
    }

    public async Task<VerifyOutcome?> VerifyAndCloseAsync(int hazardId, int likelihood, int severity,
        string effectivenessNote, string officerId, CancellationToken ct = default)
    {
        var hazard = await _db.HazardReports
            .Include(h => h.CorrectiveActions)
            .Include(h => h.Assessments)
            .FirstOrDefaultAsync(h => h.Id == hazardId, ct);
        if (hazard is null) return null;
        if (hazard.Status != HazardStatus.UnderVerification) return null;

        var residual = new RiskScore(likelihood, severity);
        var now = DateTime.UtcNow;

        hazard.Assessments.Add(new RiskAssessment
        {
            HazardReportId = hazard.Id,
            Likelihood = likelihood,
            Severity = severity,
            RiskScoreValue = residual.Score,
            RiskLevel = residual.Level,
            Rationale = "Residual risk after corrective actions. " + effectivenessNote.Trim(),
            AssessedById = officerId,
            AssessedDate = now,
            IsResidual = true,
            CreatedAt = now
        });

        // Mark completed actions as verified.
        foreach (var c in hazard.CorrectiveActions.Where(c => c.Status == CapaStatus.Completed))
        {
            c.Status = CapaStatus.Verified;
            c.VerifiedById = officerId;
            c.VerifiedDate = now;
            c.EffectivenessNote = effectivenessNote.Trim();
            c.UpdatedAt = now;
        }

        // Policy: residual is acceptable when Low or Medium.
        var acceptable = residual.Level is RiskLevel.Low or RiskLevel.Medium;
        hazard.TransitionTo(acceptable ? HazardStatus.Closed : HazardStatus.ActionRequired);

        await _db.SaveChangesAsync(ct);

        await _notify.NotifyUserAsync(hazard.ReportedById,
            acceptable ? $"Hazard {hazard.ReferenceNo} closed" : $"Hazard {hazard.ReferenceNo} needs more mitigation",
            acceptable
                ? $"Verified and closed. Residual risk {residual.Score} ({residual.Level})."
                : $"Residual risk {residual.Score} ({residual.Level}) is not acceptable; further corrective action is required.",
            $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return new VerifyOutcome(acceptable, residual.Level, residual.Score);
    }

    public async Task<bool> RejectAsync(int hazardId, string reason, string officerId, CancellationToken ct = default)
    {
        var hazard = await _db.HazardReports.FirstOrDefaultAsync(h => h.Id == hazardId, ct);
        if (hazard is null || hazard.Status != HazardStatus.Reported) return false;
        hazard.TransitionTo(HazardStatus.Rejected);
        hazard.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _notify.NotifyUserAsync(hazard.ReportedById,
            $"Hazard {hazard.ReferenceNo} rejected",
            string.IsNullOrWhiteSpace(reason) ? "Your reported hazard was rejected." : $"Rejected: {reason}",
            $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return true;
    }

    public async Task<bool> ReopenAsync(int hazardId, string officerId, CancellationToken ct = default)
    {
        var hazard = await _db.HazardReports.FirstOrDefaultAsync(h => h.Id == hazardId, ct);
        if (hazard is null || hazard.Status != HazardStatus.Closed) return false;
        hazard.TransitionTo(HazardStatus.ActionRequired);
        hazard.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
