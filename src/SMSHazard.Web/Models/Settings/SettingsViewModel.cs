using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SMSHazard.Application.DTOs;

namespace SMSHazard.Web.Models.Settings;

public class SettingsViewModel
{
    [Required, StringLength(200), Display(Name = "Organisation name")]
    public string OrganizationName { get; set; } = "SMS-Hazard";

    [EmailAddress, StringLength(256), Display(Name = "Support email")]
    public string? SupportEmail { get; set; }

    [Display(Name = "Allow anonymous public reporting")]
    public bool AllowAnonymousReporting { get; set; } = true;

    public string? LogoPath { get; set; }

    [Display(Name = "Upload a new logo")]
    public IFormFile? LogoFile { get; set; }

    // Read-only panels
    public IReadOnlyList<AuditLogDto> RecentLogs { get; set; } = new List<AuditLogDto>();
    public string EnvironmentName { get; set; } = "";
    public DateTime ServerTimeUtc { get; set; }
    public string AppVersion { get; set; } = "";
}
