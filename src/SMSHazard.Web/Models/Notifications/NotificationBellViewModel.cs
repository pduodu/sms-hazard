using SMSHazard.Application.DTOs;

namespace SMSHazard.Web.Models.Notifications;

public sealed class NotificationBellViewModel
{
    public int UnreadCount { get; set; }
    public IReadOnlyList<NotificationDto> UnreadItems { get; set; } = Array.Empty<NotificationDto>();
}
