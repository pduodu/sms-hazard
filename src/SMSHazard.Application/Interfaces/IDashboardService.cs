using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct = default);
}
