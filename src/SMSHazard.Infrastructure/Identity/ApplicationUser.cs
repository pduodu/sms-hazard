using Microsoft.AspNetCore.Identity;

namespace SMSHazard.Infrastructure.Identity;

/// <summary>Application user; extends ASP.NET Core Identity with a display name.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Canonical role names used across the app.</summary>
public static class Roles
{
    public const string Reporter = "Reporter";
    public const string SafetyOfficer = "SafetyOfficer";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly string[] All = { Reporter, SafetyOfficer, Manager, Admin };
}
