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

        // One projection per hazard with its current (latest) risk level — avoids N+1.
        var rows = await _db.HazardReports.AsNoTracking()
            .Select(h => new
            {
                h.Status,
                Level = h.Assessments
                    .OrderByDescending(a => a.AssessedDate)
                    .Select(a => (RiskLevel?)a.RiskLevel)
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
            Low = rows.Count(r => r.Level == RiskLevel.Low),
            Medium = rows.Count(r => r.Level == RiskLevel.Medium),
            High = rows.Count(r => r.Level == RiskLevel.High),
            Extreme = rows.Count(r => r.Level == RiskLevel.Extreme),
            NotAssessed = rows.Count(r => r.Level == null)
        };
        dto.HighRiskHazards = dto.High + dto.Extreme;
        return dto;
    }
}
