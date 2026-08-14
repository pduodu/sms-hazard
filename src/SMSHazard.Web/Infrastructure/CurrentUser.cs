using System.Security.Claims;
using SMSHazard.Application.Interfaces;

namespace SMSHazard.Web.Infrastructure;

/// <summary>Resolves the signed-in user's id from the HTTP context for the audit interceptor.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public CurrentUser(IHttpContextAccessor http) => _http = http;

    public string? UserId =>
        _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
