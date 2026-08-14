using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(-30);

        // One projection per hazard with its current (latest) risk level + L/S — avoids N+1.
        var rows = await _db.HazardReports.AsNoTracking()
            .Select(h => new
            {
                h.Status,
                Latest = h.Assessments
                    .OrderByDescending(a => a.AssessedDate)
                    .Select(a => new { a.RiskLevel, a.Likelihood, a.Severity })
                    .FirstOrDefault(),
                ClosedRecently = h.Status == HazardStatus.Closed && (h.UpdatedAt ?? h.CreatedAt) >= cutoff
            })
            .ToListAsync(ct);

        var overdue = await _db.CorrectiveActions.AsNoTracking()
            .CountAsync(c => c.DueDate.Date < today &&
                             c.Status != CapaStatus.Completed && c.Status != CapaStatus.Verified, ct);

        var dto = new DashboardDto
        {
            TotalHazards = rows.Count,
            OpenHazards = rows.Count(r => r.Status != HazardStatus.Closed && r.Status != HazardStatus.Rejected),
            ClosedThisPeriod = rows.Count(r => r.ClosedRecently),
            OverdueActions = overdue,
            Low = rows.Count(r => r.Latest != null && r.Latest.RiskLevel == RiskLevel.Low),
            Medium = rows.Count(r => r.Latest != null && r.Latest.RiskLevel == RiskLevel.Medium),
            High = rows.Count(r => r.Latest != null && r.Latest.RiskLevel == RiskLevel.High),
            Extreme = rows.Count(r => r.Latest != null && r.Latest.RiskLevel == RiskLevel.Extreme),
            NotAssessed = rows.Count(r => r.Latest == null)
        };
        dto.HighRiskHazards = dto.High + dto.Extreme;

        // 5×5 heat-map: count hazards by their latest (likelihood, severity).
        foreach (var r in rows.Where(r => r.Latest != null))
        {
            var l = r.Latest!.Likelihood;
            var s = r.Latest!.Severity;
            if (l is >= 1 and <= 5 && s is >= 1 and <= 5)
                dto.Heat[l - 1][s - 1]++;
        }
        return dto;
    }
}
