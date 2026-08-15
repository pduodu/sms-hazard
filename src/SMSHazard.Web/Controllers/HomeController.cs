using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;

namespace SMSHazard.Web.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;

    public HomeController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var canSeeDashboard = User.IsInRole("SafetyOfficer") || User.IsInRole("Manager") || User.IsInRole("Admin");
        return View(canSeeDashboard ? await _dashboard.GetAsync(ct) : null);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
