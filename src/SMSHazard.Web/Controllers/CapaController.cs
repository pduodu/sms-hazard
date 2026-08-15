using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Web.Models.Capa;

namespace SMSHazard.Web.Controllers;

[Authorize]
public class CapaController : Controller
{
    private readonly ICapaService _capa;
    private readonly IHazardService _hazards;
    private readonly UserManager<ApplicationUser> _users;

    public CapaController(ICapaService capa, IHazardService hazards, UserManager<ApplicationUser> users)
    {
        _capa = capa;
        _hazards = hazards;
        _users = users;
    }

    private const string Staff = "SafetyOfficer,Manager,Admin";
    private bool IsStaff() =>
        User.IsInRole(Roles.SafetyOfficer) || User.IsInRole(Roles.Manager) || User.IsInRole(Roles.Admin);
    private bool IsDrawerRequest() => Request.Headers["X-CAPA-Drawer"] == "1";

    // ---- Assign a corrective/preventive action (staff) ----
    [HttpGet]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Create(int hazardId)
    {
        var h = await _hazards.GetDetailAsync(hazardId);
        if (h is null) return NotFound();
        if (h.Status is not (HazardStatus.ActionRequired or HazardStatus.InProgress))
        {
            TempData["Error"] = "Actions can only be assigned when the hazard is Action Required or In Progress.";
            return RedirectToAction("Details", "Hazards", new { id = hazardId });
        }
        var vm = new CreateCapaViewModel { HazardId = h.Id, ReferenceNo = h.ReferenceNo, Title = h.Title };
        vm.Owners = await OwnerOptions();
        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> CreateDrawer(int hazardId)
    {
        var h = await _hazards.GetDetailAsync(hazardId);
        if (h is null) return NotFound();
        if (h.Status is not (HazardStatus.ActionRequired or HazardStatus.InProgress))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Content("Actions can only be assigned when the hazard is Action Required or In Progress.");
        }

        var vm = new CreateCapaViewModel { HazardId = h.Id, ReferenceNo = h.ReferenceNo, Title = h.Title };
        vm.Owners = await OwnerOptions();
        return PartialView("_CreateForm", vm);
    }

    [HttpPost]
    [Authorize(Roles = Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCapaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Owners = await OwnerOptions();
            if (IsDrawerRequest()) return PartialView("_CreateForm", vm);
            return View(vm);
        }
        var ok = await _capa.CreateAsync(new CreateCapaRequest
        {
            HazardId = vm.HazardId,
            Description = vm.Description,
            Type = vm.Type,
            AssignedToId = vm.AssignedToId,
            DueDate = vm.DueDate
        }, _users.GetUserId(User)!);

        if (!ok) return NotFound();
        TempData["Success"] = "Corrective action assigned.";
        if (IsDrawerRequest())
        {
            return Json(new { redirectUrl = Url.Action("Details", "Hazards", new { id = vm.HazardId }) });
        }
        return RedirectToAction("Details", "Hazards", new { id = vm.HazardId });
    }

    // ---- My actions (any authenticated user who is an owner) ----
    [HttpGet]
    public async Task<IActionResult> MyActions()
    {
        var items = await _capa.MyActionsAsync(_users.GetUserId(User)!);
        return View(items);
    }

    // ---- Owner updates progress ----
    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var dto = await _capa.GetForUpdateAsync(id);
        if (dto is null) return NotFound();
        if (!IsStaff() && dto.AssignedToId != _users.GetUserId(User)) return Forbid();

        return View(MapUpdateViewModel(dto));
    }

    [HttpGet]
    public async Task<IActionResult> UpdateDrawer(int id)
    {
        var dto = await _capa.GetForUpdateAsync(id);
        if (dto is null) return NotFound();
        if (!IsStaff() && dto.AssignedToId != _users.GetUserId(User)) return Forbid();

        return PartialView("_UpdateForm", MapUpdateViewModel(dto));
    }

    private static UpdateCapaViewModel MapUpdateViewModel(CapaEditDto dto)
    {
        return new UpdateCapaViewModel
        {
            CapaId = dto.CapaId,
            HazardId = dto.HazardId,
            HazardRef = dto.HazardRef,
            HazardTitle = dto.HazardTitle,
            Description = dto.Description,
            Status = dto.Status,
            ProgressNote = dto.ProgressNote
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateCapaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            if (IsDrawerRequest()) return PartialView("_UpdateForm", vm);
            return View(vm);
        }
        var ok = await _capa.UpdateProgressAsync(
            vm.CapaId, vm.Status, vm.ProgressNote, _users.GetUserId(User)!, IsStaff());
        if (!ok) return Forbid();
        TempData["Success"] = "Action updated.";
        if (IsDrawerRequest())
        {
            return Json(new { redirectUrl = Url.Action("Details", "Hazards", new { id = vm.HazardId }) });
        }
        return RedirectToAction("Details", "Hazards", new { id = vm.HazardId });
    }

    // ---- Verify & close (staff) ----
    [HttpGet]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Verify(int hazardId)
    {
        var h = await _hazards.GetDetailAsync(hazardId);
        if (h is null) return NotFound();
        if (h.Status != HazardStatus.UnderVerification)
        {
            TempData["Error"] = "This hazard is not awaiting verification.";
            return RedirectToAction("Details", "Hazards", new { id = hazardId });
        }
        return View(new VerifyViewModel { HazardId = h.Id, ReferenceNo = h.ReferenceNo, Title = h.Title });
    }

    [HttpPost]
    [Authorize(Roles = Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(VerifyViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var outcome = await _capa.VerifyAndCloseAsync(
            vm.HazardId, vm.Likelihood, vm.Severity, vm.EffectivenessNote, _users.GetUserId(User)!);
        if (outcome is null) return NotFound();

        TempData[outcome.Closed ? "Success" : "Error"] = outcome.Closed
            ? $"Hazard closed. Residual risk {outcome.ResidualScore} ({outcome.ResidualLevel})."
            : $"Residual risk {outcome.ResidualScore} ({outcome.ResidualLevel}) is not acceptable — returned to Action Required for further mitigation.";
        return RedirectToAction("Details", "Hazards", new { id = vm.HazardId });
    }

    // ---- Reject (staff) ----
    [HttpPost]
    [Authorize(Roles = Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int hazardId, string? reason)
    {
        var ok = await _capa.RejectAsync(hazardId, reason ?? "", _users.GetUserId(User)!);
        TempData[ok ? "Success" : "Error"] = ok ? "Hazard rejected." : "Unable to reject (must be a newly reported hazard).";
        return RedirectToAction("Details", "Hazards", new { id = hazardId });
    }

    // ---- Reopen (staff) ----
    [HttpPost]
    [Authorize(Roles = Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int hazardId)
    {
        var ok = await _capa.ReopenAsync(hazardId, _users.GetUserId(User)!);
        TempData[ok ? "Success" : "Error"] = ok ? "Hazard reopened." : "Unable to reopen (must be a closed hazard).";
        return RedirectToAction("Details", "Hazards", new { id = hazardId });
    }

    private async Task<IEnumerable<SelectListItem>> OwnerOptions()
    {
        var users = await _capa.GetAssignableUsersAsync();
        return users.Select(u => new SelectListItem(u.Name, u.Id));
    }
}
