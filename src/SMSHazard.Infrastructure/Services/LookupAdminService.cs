using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Entities;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

public sealed class LookupAdminService : ILookupAdminService
{
    private readonly AppDbContext _db;
    public LookupAdminService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LookupItem>> CategoriesAsync(CancellationToken ct = default) =>
        await _db.HazardCategories.OrderBy(c => c.Name).Select(c => new LookupItem(c.Id, c.Name)).ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> DepartmentsAsync(CancellationToken ct = default) =>
        await _db.Departments.OrderBy(d => d.Name).Select(d => new LookupItem(d.Id, d.Name)).ToListAsync(ct);

    public async Task<bool> AddCategoryAsync(string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || await _db.HazardCategories.AnyAsync(c => c.Name == name, ct)) return false;
        _db.HazardCategories.Add(new HazardCategory { Name = name, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AddDepartmentAsync(string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || await _db.Departments.AnyAsync(d => d.Name == name, ct)) return false;
        _db.Departments.Add(new Department { Name = name, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RenameCategoryAsync(int id, string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return false;
        var c = await _db.HazardCategories.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return false;
        if (await _db.HazardCategories.AnyAsync(x => x.Name == name && x.Id != id, ct)) return false;
        c.Name = name; c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RenameDepartmentAsync(int id, string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return false;
        var d = await _db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (d is null) return false;
        if (await _db.Departments.AnyAsync(x => x.Name == name && x.Id != id, ct)) return false;
        d.Name = name; d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<LookupItem?> GetCategoryAsync(int id, CancellationToken ct = default) =>
        await _db.HazardCategories.Where(c => c.Id == id).Select(c => new LookupItem(c.Id, c.Name)).FirstOrDefaultAsync(ct);

    public async Task<LookupItem?> GetDepartmentAsync(int id, CancellationToken ct = default) =>
        await _db.Departments.Where(d => d.Id == id).Select(d => new LookupItem(d.Id, d.Name)).FirstOrDefaultAsync(ct);
}
