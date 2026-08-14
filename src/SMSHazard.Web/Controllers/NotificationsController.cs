using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure.Identity;

namespace SMSHazard.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notify;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationsController(INotificationService notify, UserManager<ApplicationUser> users)
    {
        _notify = notify;
        _users = users;
    }

    private string Uid => _users.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await _notify.RecentAsync(Uid));

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
        => Json(new { count = await _notify.UnreadCountAsync(Uid) });

    // Mark read and navigate to the notification's target (click-through).
    [HttpGet]
    public async Task<IActionResult> Open(int id, string? url)
    {
        await _notify.MarkReadAsync(id, Uid);
        return Url.IsLocalUrl(url) ? Redirect(url!) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notify.MarkAllReadAsync(Uid);
        return RedirectToAction(nameof(Index));
    }
}
