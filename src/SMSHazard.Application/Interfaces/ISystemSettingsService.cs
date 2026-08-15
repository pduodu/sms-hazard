using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

public interface ISystemSettingsService
{
    /// <summary>Returns the current settings, creating the singleton row on first access. Cached.</summary>
    Task<SystemSettingsDto> GetAsync(CancellationToken ct = default);

    /// <summary>Persists organisation name, support email and the anonymous-reporting toggle.</summary>
    Task UpdateAsync(SystemSettingsUpdate update, CancellationToken ct = default);

    /// <summary>Sets (or clears, when null) the branding logo path.</summary>
    Task SetLogoAsync(string? logoPath, CancellationToken ct = default);
}
