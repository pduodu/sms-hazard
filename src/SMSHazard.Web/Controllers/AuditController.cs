using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;

namespace SMSHazard.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AuditController : Controller
{
    private readonly IAuditService _audit;
    public AuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> Index(string? entity)
    {
        ViewBag.Entity = entity;
        return View(await _audit.GetRecentAsync(entity));
    }
}
