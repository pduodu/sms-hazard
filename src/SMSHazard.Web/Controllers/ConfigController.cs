using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSHazard.Application.Interfaces;
using SMSHazard.Web.Models.Config;

namespace SMSHazard.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ConfigController : Controller
{
    private readonly ILookupAdminService _lookups;
    public ConfigController(ILookupAdminService lookups) => _lookups = lookups;

    [HttpGet]
    public async Task<IActionResult> Index() => View(new ConfigIndexViewModel
    {
        Categories = await _lookups.CategoriesAsync(),
        Departments = await _lookups.DepartmentsAsync()
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name)
    {
        var added = await _lookups.AddCategoryAsync(name ?? "");
        TempData[added ? "Success" : "Error"] = added ? "Category added." : "Could not add category (empty or duplicate).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDepartment(string name)
    {
        var added = await _lookups.AddDepartmentAsync(name ?? "");
        TempData[added ? "Success" : "Error"] = added ? "Department added." : "Could not add department (empty or duplicate).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string kind)
    {
        var item = kind == "Department" ? await _lookups.GetDepartmentAsync(id) : await _lookups.GetCategoryAsync(id);
        if (item is null) return NotFound();
        return View(new EditLookupViewModel { Id = item.Id, Name = item.Name, Kind = kind });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditLookupViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var ok = vm.Kind == "Department"
            ? await _lookups.RenameDepartmentAsync(vm.Id, vm.Name)
            : await _lookups.RenameCategoryAsync(vm.Id, vm.Name);
        TempData[ok ? "Success" : "Error"] = ok ? "Saved." : "Could not save (empty or duplicate name).";
        return ok ? RedirectToAction(nameof(Index)) : View(vm);
    }
}
