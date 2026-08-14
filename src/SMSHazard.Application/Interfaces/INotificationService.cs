namespace SMSHazard.Application.Interfaces;

/// <summary>Writes the Notification row (source of truth) and optionally pushes email.</summary>
public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string message, string? linkUrl = null,
        bool alsoEmail = true, CancellationToken ct = default);
}
