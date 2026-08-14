using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Web.Models.Hazards;

namespace SMSHazard.Web.Controllers;

[Authorize]
public class HazardsController : Controller
{
    private const long MaxFileBytes = 25 * 1024 * 1024; // 25 MB
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf"
    };

    private readonly IHazardService _hazards;
    private readonly IRiskService _risk;
    private readonly IValidator<CreateHazardRequest> _validator;
    private readonly UserManager<ApplicationUser> _users;

    public HazardsController(IHazardService hazards, IRiskService risk,
        IValidator<CreateHazardRequest> validator, UserManager<ApplicationUser> users)
    {
        _hazards = hazards;
        _risk = risk;
        _validator = validator;
        _users = users;
    }

    private bool IsStaff() =>
        User.IsInRole(Roles.SafetyOfficer) || User.IsInRole(Roles.Manager) || User.IsInRole(Roles.Admin);

    // ---- Register / list (staff see all) ----
    [HttpGet]
    [Authorize(Roles = "SafetyOfficer,Manager,Admin")]
    public async Task<IActionResult> Index(HazardListViewModel vm)
    {
        var filter = new HazardFilter
        {
            Status = vm.Status,
            RiskLevel = vm.RiskLevel,
            DepartmentId = vm.DepartmentId
        };
        vm.Items = await _hazards.ListAsync(filter);
        vm.Departments = await DepartmentOptions();
        vm.Heading = "Hazard register";
        return View(vm);
    }

    // ---- My reports (any authenticated user) ----
    [HttpGet]
    public async Task<IActionResult> MyReports()
    {
        var userId = _users.GetUserId(User)!;
        var items = await _hazards.ListAsync(new HazardFilter { ReporterId = userId });
        var vm = new HazardListViewModel { Items = items, Heading = "My reports", ShowFilters = false };
        return View("Index", vm);
    }

    // ---- Create ----
    [HttpGet]
    public async Task<IActionResult> Create()
        => View(await BuildCreateVm(new CreateHazardViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateHazardViewModel vm)
    {
        var request = new CreateHazardRequest
        {
            Title = vm.Title,
            Description = vm.Description,
            HazardCategoryId = vm.HazardCategoryId,
            DepartmentId = vm.DepartmentId,
            OccurrenceDate = DateTime.SpecifyKind(vm.OccurrenceDate, DateTimeKind.Utc),
            ImmediateActionTaken = vm.ImmediateActionTaken
        };

        // FluentValidation (server-side, Application-layer rules)
        var result = await _validator.ValidateAsync(request);
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        // Attachment validation
        var uploads = new List<AttachmentUpload>();
        if (vm.Attachments is not null)
        {
            foreach (var file in vm.Attachments.Where(f => f.Length > 0))
            {
                if (file.Length > MaxFileBytes)
                    ModelState.AddModelError(nameof(vm.Attachments), $"{file.FileName} exceeds the 25 MB limit.");
                else if (!AllowedContentTypes.Contains(file.ContentType))
                    ModelState.AddModelError(nameof(vm.Attachments), $"{file.FileName} is not an accepted type (images or PDF).");
                else
                    uploads.Add(new AttachmentUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream()));
            }
        }

        if (!ModelState.IsValid)
            return View(await BuildCreateVm(vm));

        var reporterId = _users.GetUserId(User)!;
        var (id, referenceNo) = await _hazards.CreateAsync(request, uploads, reporterId);

        TempData["Success"] = $"Hazard {referenceNo} submitted successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Details ----
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var dto = await _hazards.GetDetailAsync(id);
        if (dto is null) return NotFound();

        // Access check: reporters may only view their own; staff may view all.
        if (!IsStaff() && dto.ReporterId != _users.GetUserId(User))
            return Forbid();

        return View(dto);
    }

    // ---- Risk assessment (Safety Officer / Manager / Admin) ----
    [HttpGet]
    [Authorize(Roles = "SafetyOfficer,Manager,Admin")]
    public async Task<IActionResult> Assess(int id, bool residual = false)
    {
        var h = await _hazards.GetDetailAsync(id);
        if (h is null) return NotFound();
        if (h.Status is HazardStatus.Closed or HazardStatus.Rejected)
        {
            TempData["Error"] = "This hazard is closed or rejected and cannot be assessed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new AssessViewModel
        {
            HazardId = h.Id,
            ReferenceNo = h.ReferenceNo,
            Title = h.Title,
            CurrentStatus = h.Status,
            IsResidual = residual
        });
    }

    [HttpPost]
    [Authorize(Roles = "SafetyOfficer,Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assess(AssessViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var assessorId = _users.GetUserId(User)!;
        var result = await _risk.AssessAsync(
            vm.HazardId, vm.Likelihood, vm.Severity, vm.Rationale, assessorId, vm.IsResidual);

        if (result is null) return NotFound();

        var (score, level) = result.Value;
        TempData["Success"] = $"Risk assessed: score {score} ({level}).";
        return RedirectToAction(nameof(Details), new { id = vm.HazardId });
    }

    // ---- Attachment download (authorised action, not static hosting) ----
    [HttpGet]
    public async Task<IActionResult> Attachment(int id)
    {
        var result = await _hazards.OpenAttachmentAsync(id);
        if (result is null) return NotFound();

        var (meta, reporterId, content) = result.Value;
        if (!IsStaff() && reporterId != _users.GetUserId(User))
        {
            await content.DisposeAsync();
            return Forbid();
        }
        return File(content, meta.ContentType, meta.FileName);
    }

    // ---- helpers ----
    private async Task<CreateHazardViewModel> BuildCreateVm(CreateHazardViewModel vm)
    {
        var cats = await _hazards.GetCategoriesAsync();
        var deps = await _hazards.GetDepartmentsAsync();
        vm.Categories = cats.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
        vm.Departments = deps.Select(d => new SelectListItem(d.Name, d.Id.ToString()));
        return vm;
    }

    private async Task<IEnumerable<SelectListItem>> DepartmentOptions()
    {
        var deps = await _hazards.GetDepartmentsAsync();
        return deps.Select(d => new SelectListItem(d.Name, d.Id.ToString()));
    }
}
