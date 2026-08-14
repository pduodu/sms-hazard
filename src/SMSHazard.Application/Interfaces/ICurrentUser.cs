namespace SMSHazard.Application.Interfaces;

/// <summary>Abstraction over the signed-in user, so Infrastructure (e.g. the audit interceptor)
/// stays free of ASP.NET HTTP dependencies. Implemented in the Web layer.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
}
