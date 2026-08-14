using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure.Identity;

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
        var count = string.IsNullOrEmpty(userId) ? 0 : await _notify.UnreadCountAsync(userId);
        return View(count);
    }
}
