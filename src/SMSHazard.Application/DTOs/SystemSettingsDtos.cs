namespace SMSHazard.Application.DTOs;

public sealed class SystemSettingsDto
{
    public string OrganizationName { get; set; } = "SMS-Hazard";
    public string? LogoPath { get; set; }
    public string? SupportEmail { get; set; }
    public bool AllowAnonymousReporting { get; set; } = true;
}

public sealed class SystemSettingsUpdate
{
    public string OrganizationName { get; set; } = "SMS-Hazard";
    public string? SupportEmail { get; set; }
    public bool AllowAnonymousReporting { get; set; } = true;
}
