using SMSHazard.Domain.Common;

namespace SMSHazard.Domain.Entities;

/// <summary>
/// Singleton row (Id = 1) holding admin-configurable system settings: branding and feature toggles.
/// </summary>
public class SystemSettings : BaseEntity
{
    public string OrganizationName { get; set; } = "SMS-Hazard";

    /// <summary>Public static URL of the uploaded logo (e.g. /branding/logo-xxxx.png). Null = use default icon.</summary>
    public string? LogoPath { get; set; }

    public string? SupportEmail { get; set; }

    /// <summary>When false, the public anonymous reporting channel is disabled.</summary>
    public bool AllowAnonymousReporting { get; set; } = true;
}
