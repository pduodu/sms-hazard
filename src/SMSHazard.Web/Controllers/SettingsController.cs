using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Web.Models.Settings;

namespace SMSHazard.Web.Controllers;

/// <summary>Admin system settings hub (ADM-01): branding, feature toggles and a recent-activity log.</summary>
[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private const long MaxLogoBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedLogoExt = { ".png", ".jpg", ".jpeg", ".webp", ".svg" };

    private readonly ISystemSettingsService _settings;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public SettingsController(ISystemSettingsService settings, IAuditService audit, IWebHostEnvironment env)
    {
        _settings = settings;
        _audit = audit;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await BuildVm(new SettingsViewModel(), fromCurrent: true));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel vm)
    {
        // Logo upload (optional)
        if (vm.LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(vm.LogoFile.FileName).ToLowerInvariant();
            if (!AllowedLogoExt.Contains(ext))
                ModelState.AddModelError(nameof(vm.LogoFile), "Logo must be a PNG, JPG, WEBP or SVG image.");
            else if (vm.LogoFile.Length > MaxLogoBytes)
                ModelState.AddModelError(nameof(vm.LogoFile), "Logo must be 2 MB or smaller.");
        }

        if (!ModelState.IsValid)
            return View(await BuildVm(vm, fromCurrent: false));

        if (vm.LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(vm.LogoFile.FileName).ToLowerInvariant();
            var dir = Path.Combine(_env.WebRootPath, "branding");
            Directory.CreateDirectory(dir);
            var fileName = $"logo-{Guid.NewGuid():N}{ext}";
            await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                await vm.LogoFile.CopyToAsync(fs);
            await _settings.SetLogoAsync($"/branding/{fileName}");
        }

        await _settings.UpdateAsync(new SystemSettingsUpdate
        {
            OrganizationName = vm.OrganizationName,
            SupportEmail = vm.SupportEmail,
            AllowAnonymousReporting = vm.AllowAnonymousReporting
        });

        TempData["Success"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogo()
    {
        await _settings.SetLogoAsync(null);
        TempData["Success"] = "Logo removed.";
        return RedirectToAction(nameof(Index));
    }

    // Fire the monthly digest immediately (for testing/demo) via Hangfire.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendDigestNow()
    {
        BackgroundJob.Enqueue<IDigestService>(s => s.SendMonthlyDigestAsync());
        TempData["Success"] = "Monthly digest queued — it will be emailed to managers and admins shortly.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SettingsViewModel> BuildVm(SettingsViewModel vm, bool fromCurrent)
    {
        var current = await _settings.GetAsync();
        if (fromCurrent)
        {
            vm.OrganizationName = current.OrganizationName;
            vm.SupportEmail = current.SupportEmail;
            vm.AllowAnonymousReporting = current.AllowAnonymousReporting;
        }
        vm.LogoPath = current.LogoPath;
        vm.RecentLogs = await _audit.GetRecentAsync(take: 25);
        vm.EnvironmentName = _env.EnvironmentName;
        vm.ServerTimeUtc = DateTime.UtcNow;
        vm.AppVersion = typeof(SettingsController).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        return vm;
    }
}
