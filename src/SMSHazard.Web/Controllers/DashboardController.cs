using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SMSHazard.Web.Controllers;

[Authorize(Roles = "SafetyOfficer,Manager,Admin")]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Home");
}
