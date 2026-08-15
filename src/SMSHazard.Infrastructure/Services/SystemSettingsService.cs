using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Entities;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

/// <summary>
/// Reads/writes the singleton <see cref="SystemSettings"/> row. The DTO is memory-cached because
/// the layout reads branding on every request; the cache is cleared on any update.
/// </summary>
public sealed class SystemSettingsService : ISystemSettingsService
{
    private const string CacheKey = "system-settings-v1";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public SystemSettingsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<SystemSettingsDto> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out SystemSettingsDto? cached) && cached is not null)
            return cached;

        var entity = await EnsureAsync(ct);
        var dto = Map(entity);
        _cache.Set(CacheKey, dto, TimeSpan.FromMinutes(10));
        return dto;
    }

    public async Task UpdateAsync(SystemSettingsUpdate update, CancellationToken ct = default)
    {
        var s = await EnsureAsync(ct);
        s.OrganizationName = string.IsNullOrWhiteSpace(update.OrganizationName)
            ? "SMS-Hazard" : update.OrganizationName.Trim();
        s.SupportEmail = string.IsNullOrWhiteSpace(update.SupportEmail) ? null : update.SupportEmail.Trim();
        s.AllowAnonymousReporting = update.AllowAnonymousReporting;
        await _db.SaveChangesAsync(ct);
        _cache.Remove(CacheKey);
    }

    public async Task SetLogoAsync(string? logoPath, CancellationToken ct = default)
    {
        var s = await EnsureAsync(ct);
        s.LogoPath = logoPath;
        await _db.SaveChangesAsync(ct);
        _cache.Remove(CacheKey);
    }

    private async Task<SystemSettings> EnsureAsync(CancellationToken ct)
    {
        var s = await _db.SystemSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new SystemSettings();
            _db.SystemSettings.Add(s);
            await _db.SaveChangesAsync(ct);
        }
        return s;
    }

    private static SystemSettingsDto Map(SystemSettings s) => new()
    {
        OrganizationName = s.OrganizationName,
        LogoPath = s.LogoPath,
        SupportEmail = s.SupportEmail,
        AllowAnonymousReporting = s.AllowAnonymousReporting
    };
}
