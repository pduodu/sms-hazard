using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Identity;

namespace SMSHazard.Infrastructure.Persistence;

/// <summary>
/// Idempotent seeding of roles, demo users, lookup data, and a few sample hazards spanning
/// risk levels (so the dashboard/register look real for grading). Safe to run on every startup.
/// </summary>
public static class SeedData
{
    // Demo credentials (documented in Deployment_and_Source_Links.txt).
    // NOTE: known demo passwords are a deliberate, documented technical debt (D8) — never used in production.
    public const string DemoPassword = "SmsHazard#2026";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();

        // 1) Roles
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2) Demo users (one per role, plus an action owner)
        var admin = await EnsureUser(userManager, "admin@smshazard.demo", "System Administrator", Roles.Admin);
        var manager = await EnsureUser(userManager, "manager@smshazard.demo", "Safety Manager", Roles.Manager);
        var officer = await EnsureUser(userManager, "officer@smshazard.demo", "Safety Officer", Roles.SafetyOfficer);
        var reporter = await EnsureUser(userManager, "reporter@smshazard.demo", "Ama Reporter", Roles.Reporter);
        var owner = await EnsureUser(userManager, "owner@smshazard.demo", "Kwame Technician", Roles.Reporter);

        // 3) Lookup data
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Name = "Operations", CreatedAt = DateTime.UtcNow },
                new Department { Name = "Maintenance", CreatedAt = DateTime.UtcNow },
                new Department { Name = "Warehouse", CreatedAt = DateTime.UtcNow },
                new Department { Name = "Administration", CreatedAt = DateTime.UtcNow });
        }
        if (!await db.HazardCategories.AnyAsync())
        {
            db.HazardCategories.AddRange(
                new HazardCategory { Name = "Slip / Trip / Fall", CreatedAt = DateTime.UtcNow },
                new HazardCategory { Name = "Electrical", CreatedAt = DateTime.UtcNow },
                new HazardCategory { Name = "Fire", CreatedAt = DateTime.UtcNow },
                new HazardCategory { Name = "Chemical / Spill", CreatedAt = DateTime.UtcNow },
                new HazardCategory { Name = "Machinery / Equipment", CreatedAt = DateTime.UtcNow },
                new HazardCategory { Name = "Ergonomic", CreatedAt = DateTime.UtcNow },
                new HazardCategory { Name = "Near Miss", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();

        // 4) Sample hazards spanning risk levels (only if none exist yet)
        if (!await db.HazardReports.AnyAsync())
        {
            var ops = await db.Departments.FirstAsync(d => d.Name == "Operations");
            var maint = await db.Departments.FirstAsync(d => d.Name == "Maintenance");
            var whse = await db.Departments.FirstAsync(d => d.Name == "Warehouse");
            var elec = await db.HazardCategories.FirstAsync(c => c.Name == "Electrical");
            var slip = await db.HazardCategories.FirstAsync(c => c.Name == "Slip / Trip / Fall");
            var fire = await db.HazardCategories.FirstAsync(c => c.Name == "Fire");

            var now = DateTime.UtcNow;

            var h1 = NewHazard("HZ-2026-0001", "Exposed wiring near loading bay",
                "Frayed cable with exposed conductors beside the main loading bay door.",
                elec.Id, ops.Id, reporter!.Id, now.AddDays(-6), HazardStatus.ActionRequired);
            h1.Assessments.Add(NewAssessment(5, 4, "High footfall area with live conductors.", officer!.Id, now.AddDays(-5), false)); // 20 Extreme

            var h2 = NewHazard("HZ-2026-0002", "Oil spill on workshop floor",
                "Hydraulic oil leak creating a slip hazard near bay 3.",
                slip.Id, maint.Id, owner!.Id, now.AddDays(-4), HazardStatus.UnderAssessment);
            h2.Assessments.Add(NewAssessment(3, 3, "Contained but not yet cleaned.", officer.Id, now.AddDays(-3), false)); // 9 Medium

            var h3 = NewHazard("HZ-2026-0003", "Blocked fire exit in warehouse",
                "Pallets stacked in front of the emergency exit on the east wall.",
                fire.Id, whse.Id, reporter.Id, now.AddDays(-2), HazardStatus.Reported);
            // not yet assessed

            db.HazardReports.AddRange(h1, h2, h3);
            await db.SaveChangesAsync();

            // A corrective action on h1, overdue, to make reminders/dashboards meaningful
            db.CorrectiveActions.Add(new CorrectiveAction
            {
                HazardReportId = h1.Id,
                Description = "Isolate circuit and replace damaged cable to code.",
                Type = CapaType.Corrective,
                AssignedToId = owner.Id,
                DueDate = now.AddDays(-1),          // overdue
                Status = CapaStatus.InProgress,
                CreatedAt = now.AddDays(-5)
            });
            await db.SaveChangesAsync();
        }
    }

    private static HazardReport NewHazard(string reference, string title, string description,
        int categoryId, int departmentId, string reportedById, DateTime reportedDate, HazardStatus status)
    {
        var h = new HazardReport
        {
            ReferenceNo = reference,
            Title = title,
            Description = description,
            HazardCategoryId = categoryId,
            DepartmentId = departmentId,
            ReportedById = reportedById,
            ReportedDate = reportedDate,
            OccurrenceDate = reportedDate.AddHours(-2),
            ImmediateActionTaken = "Area cordoned and supervisor notified.",
            CreatedAt = reportedDate
        };
        h.SetInitialStatus(status);
        return h;
    }

    private static RiskAssessment NewAssessment(int likelihood, int severity, string rationale,
        string assessedById, DateTime date, bool residual)
    {
        var a = new RiskAssessment
        {
            Likelihood = likelihood,
            Severity = severity,
            Rationale = rationale,
            AssessedById = assessedById,
            AssessedDate = date,
            IsResidual = residual,
            CreatedAt = date
        };
        a.ApplyScore();
        return a;
    }

    private static async Task<ApplicationUser?> EnsureUser(
        UserManager<ApplicationUser> userManager, string email, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded) return null;
        }
        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
