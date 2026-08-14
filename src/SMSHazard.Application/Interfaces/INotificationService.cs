using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

/// <summary>
/// Writes the Notification row (single source of truth) and optionally sends email.
/// The in-app centre reads these rows; email is best-effort on top.
/// </summary>
public interface INotificationService
{
    Task NotifyUserAsync(string userId, string title, string message,
        string? linkUrl = null, bool alsoEmail = true, CancellationToken ct = default);

    /// <summary>Notify every active user in the given role.</summary>
    Task NotifyRoleAsync(string role, string title, string message,
        string? linkUrl = null, bool alsoEmail = true, CancellationToken ct = default);

    Task<int> UnreadCountAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> RecentAsync(string userId, int take = 30, CancellationToken ct = default);
    Task MarkReadAsync(int id, string userId, CancellationToken ct = default);
    Task MarkAllReadAsync(string userId, CancellationToken ct = default);
}
