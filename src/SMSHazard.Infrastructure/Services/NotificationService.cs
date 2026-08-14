using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Entities;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

/// <summary>
/// Persists notifications (source of truth) and sends best-effort email.
/// Email failures never throw (the sender logs and swallows), so the in-app
/// centre stays correct even when SMTP is down or unconfigured.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationService(AppDbContext db, IEmailSender email, UserManager<ApplicationUser> users)
    {
        _db = db;
        _email = email;
        _users = users;
    }

    public async Task NotifyUserAsync(string userId, string title, string message,
        string? linkUrl = null, bool alsoEmail = true, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        if (alsoEmail)
        {
            var user = await _users.FindByIdAsync(userId);
            if (!string.IsNullOrWhiteSpace(user?.Email))
                await _email.SendAsync(user!.Email!, title, BuildHtml(title, message, linkUrl), ct);
        }
    }

    public async Task NotifyRoleAsync(string role, string title, string message,
        string? linkUrl = null, bool alsoEmail = true, CancellationToken ct = default)
    {
        var recipients = await _users.GetUsersInRoleAsync(role);
        foreach (var user in recipients.Where(u => u.IsActive))
        {
            _db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);

        if (alsoEmail)
        {
            foreach (var user in recipients.Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email)))
                await _email.SendAsync(user.Email!, title, BuildHtml(title, message, linkUrl), ct);
        }
    }

    public Task<int> UnreadCountAsync(string userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<IReadOnlyList<NotificationDto>> RecentAsync(string userId, int take = 30, CancellationToken ct = default) =>
        await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedDate = n.CreatedAt
            })
            .ToListAsync(ct);

    public async Task MarkReadAsync(int id, string userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (n is null || n.IsRead) return;
        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var n in unread) { n.IsRead = true; n.ReadAt = now; }
        await _db.SaveChangesAsync(ct);
    }

    private static string BuildHtml(string title, string message, string? linkUrl)
    {
        var link = string.IsNullOrWhiteSpace(linkUrl)
            ? ""
            : $"<p><a href=\"{linkUrl}\">Open in SMS-Hazard</a></p>";
        return $@"<div style=""font-family:sans-serif"">
<h3>{System.Net.WebUtility.HtmlEncode(title)}</h3>
<p>{System.Net.WebUtility.HtmlEncode(message)}</p>
{link}
<hr/><small>SMS-Hazard — automated safety notification.</small></div>";
    }
}
