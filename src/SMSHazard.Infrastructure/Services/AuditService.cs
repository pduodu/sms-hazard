using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        string? entityName = null, int take = 200, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
            q = q.Where(a => a.EntityName == entityName);

        var list = await q.OrderByDescending(a => a.Timestamp)
            .Take(take)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                Timestamp = a.Timestamp,
                ChangeSummary = a.ChangeSummary,
                ChangedByName = _db.Users.Where(u => u.Id == a.ChangedById).Select(u => u.FullName).FirstOrDefault()
                    ?? a.ChangedById
            })
            .ToListAsync(ct);

        foreach (var dto in list.Where(d => d.ChangedByName == "system"))
            dto.ChangedByName = "System";
        return list;
    }
}
