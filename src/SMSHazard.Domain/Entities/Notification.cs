using SMSHazard.Domain.Common;

namespace SMSHazard.Domain.Entities;

/// <summary>Single source of truth for a notifiable event; email/real-time are downstream.</summary>
public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
