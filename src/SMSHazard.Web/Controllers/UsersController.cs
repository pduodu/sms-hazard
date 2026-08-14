using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Web.Models.Users;

namespace SMSHazard.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;

    public UsersController(UserManager<ApplicationUser> users) => _users = users;

    private static IEnumerable<SelectListItem> RoleOptions(string? selected = null) =>
        Roles.All.Select(r => new SelectListItem(r, r, r == selected));

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _users.Users.OrderBy(u => u.FullName).ToListAsync();
        var rows = new List<UserRowViewModel>();
        foreach (var u in users)
        {
            var roles = await _users.GetRolesAsync(u);
            rows.Add(new UserRowViewModel
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email ?? "",
                Role = string.Join(", ", roles), IsActive = u.IsActive
            });
        }
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateUserViewModel { Roles = RoleOptions("Reporter") });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Roles = RoleOptions(vm.Role); return View(vm); }

        var user = new ApplicationUser
        {
            UserName = vm.Email, Email = vm.Email, EmailConfirmed = true,
            FullName = vm.FullName, IsActive = true
        };
        var result = await _users.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            vm.Roles = RoleOptions(vm.Role);
            return View(vm);
        }
        await _users.AddToRoleAsync(user, vm.Role);
        TempData["Success"] = $"User {vm.Email} created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await _users.GetRolesAsync(user);
        return View(new EditUserViewModel
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? "Reporter", IsActive = user.IsActive,
            Roles = RoleOptions(roles.FirstOrDefault())
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel vm)
    {
        var user = await _users.FindByIdAsync(vm.Id);
        if (user is null) return NotFound();
        if (!ModelState.IsValid) { vm.Roles = RoleOptions(vm.Role); return View(vm); }

        user.FullName = vm.FullName;
        user.IsActive = vm.IsActive;
        await _users.UpdateAsync(user);

        var current = await _users.GetRolesAsync(user);
        if (!current.Contains(vm.Role))
        {
            await _users.RemoveFromRolesAsync(user, current);
            await _users.AddToRoleAsync(user, vm.Role);
        }
        TempData["Success"] = $"User {user.Email} updated.";
        return RedirectToAction(nameof(Index));
    }
}
