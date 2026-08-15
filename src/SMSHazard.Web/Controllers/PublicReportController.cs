using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Web.Models.PublicReport;

namespace SMSHazard.Web.Controllers;

/// <summary>
/// Public, unauthenticated hazard reporting and status tracking (feature HR-04).
/// Anyone can submit a hazard without an account and later check its progress with a tracking code.
/// </summary>
[AllowAnonymous]
public class PublicReportController : Controller
{
    private const long MaxFileBytes = 25 * 1024 * 1024; // 25 MB
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf"
    };

    private readonly IHazardService _hazards;
    private readonly IValidator<CreateHazardRequest> _validator;
    private readonly ISystemSettingsService _settings;

    public PublicReportController(IHazardService hazards, IValidator<CreateHazardRequest> validator,
        ISystemSettingsService settings)
    {
        _hazards = hazards;
        _validator = validator;
        _settings = settings;
    }

    /// <summary>Returns true when the admin has enabled anonymous public reporting.</summary>
    private async Task<bool> AnonymousEnabledAsync()
        => (await _settings.GetAsync()).AllowAnonymousReporting;

    // ---- Submit a hazard anonymously ----
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await AnonymousEnabledAsync()) return NotFound();
        return View(await BuildVm(new PublicReportViewModel()));
    }

    [HttpPost]
    [EnableRateLimiting("public-submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PublicReportViewModel vm)
    {
        if (!await AnonymousEnabledAsync()) return NotFound();

        // Honeypot: real users never see or fill this field.
        if (!string.IsNullOrWhiteSpace(vm.Website))
            return RedirectToAction(nameof(Submitted)); // silently drop bots

        var request = new CreateHazardRequest
        {
            Title = vm.Title,
            Description = vm.Description,
            HazardCategoryId = vm.HazardCategoryId,
            DepartmentId = vm.DepartmentId,
            OccurrenceDate = DateTime.SpecifyKind(vm.OccurrenceDate, DateTimeKind.Utc),
            ImmediateActionTaken = vm.ImmediateActionTaken
        };

        var result = await _validator.ValidateAsync(request);
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

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
            return View(await BuildVm(vm));

        var (_, referenceNo, trackingCode) = await _hazards.CreateAnonymousAsync(request, uploads);

        TempData["ReferenceNo"] = referenceNo;
        TempData["TrackingCode"] = trackingCode;
        return RedirectToAction(nameof(Submitted));
    }

    // ---- Confirmation with tracking code (shown once, post-submit) ----
    [HttpGet]
    public IActionResult Submitted()
    {
        var code = TempData["TrackingCode"] as string;
        if (string.IsNullOrEmpty(code))
            return RedirectToAction(nameof(Create));

        ViewBag.ReferenceNo = TempData["ReferenceNo"] as string;
        ViewBag.TrackingCode = code;
        return View();
    }

    // ---- Track status by code ----
    [HttpGet]
    public async Task<IActionResult> Track(string? code = null)
    {
        if (!await AnonymousEnabledAsync()) return NotFound();
        return View(new TrackViewModel { TrackingCode = code ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(TrackViewModel vm)
    {
        if (!await AnonymousEnabledAsync()) return NotFound();
        vm.Searched = true;
        if (ModelState.IsValid)
            vm.Result = await _hazards.TrackAsync(vm.TrackingCode);
        return View(vm);
    }

    private async Task<PublicReportViewModel> BuildVm(PublicReportViewModel vm)
    {
        var cats = await _hazards.GetCategoriesAsync();
        var deps = await _hazards.GetDepartmentsAsync();
        vm.Categories = cats.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
        vm.Departments = deps.Select(d => new SelectListItem(d.Name, d.Id.ToString()));
        return vm;
    }
}
