using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Web.Models.Notifications;

namespace SMSHazard.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notify;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationBellViewComponent(INotificationService notify, UserManager<ApplicationUser> users)
    {
        _notify = notify;
        _users = users;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _users.GetUserId(HttpContext.User);
        if (string.IsNullOrEmpty(userId))
            return View(new NotificationBellViewModel());

        var recent = await _notify.RecentAsync(userId, take: 5);
        var model = new NotificationBellViewModel
        {
            UnreadCount = await _notify.UnreadCountAsync(userId),
            UnreadItems = recent.Where(n => !n.IsRead).Take(4).ToList()
        };
        return View(model);
    }
}
