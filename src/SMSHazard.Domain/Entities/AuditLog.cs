using SMSHazard.Domain.Common;

namespace SMSHazard.Domain.Entities;

/// <summary>Immutable audit record captured by the EF Core SaveChanges interceptor.</summary>
public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Created / Updated / StateChanged / Deleted
    public string ChangedById { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
}
