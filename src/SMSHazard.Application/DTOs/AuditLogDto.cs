namespace SMSHazard.Application.DTOs;

public sealed class AuditLogDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
}
