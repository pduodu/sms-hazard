using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

public interface IAuditService
{
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(string? entityName = null, int take = 200, CancellationToken ct = default);
}
