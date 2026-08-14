using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;

namespace SMSHazard.Web.Controllers;

[Authorize(Roles = "SafetyOfficer,Manager,Admin")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<IActionResult> Index() => View(await _dashboard.GetAsync());
}
