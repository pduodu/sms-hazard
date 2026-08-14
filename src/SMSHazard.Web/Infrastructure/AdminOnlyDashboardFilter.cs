using Hangfire.Dashboard;

namespace SMSHazard.Web.Infrastructure;

/// <summary>Restricts the Hangfire dashboard to authenticated Admin users only.</summary>
public sealed class AdminOnlyDashboardFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true && http.User.IsInRole("Admin");
    }
}
