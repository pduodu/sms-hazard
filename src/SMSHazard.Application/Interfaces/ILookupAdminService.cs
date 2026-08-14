using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

/// <summary>Admin CRUD for the hazard-category and department lookups.</summary>
public interface ILookupAdminService
{
    Task<IReadOnlyList<LookupItem>> CategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupItem>> DepartmentsAsync(CancellationToken ct = default);
    Task<bool> AddCategoryAsync(string name, CancellationToken ct = default);
    Task<bool> AddDepartmentAsync(string name, CancellationToken ct = default);
    Task<bool> RenameCategoryAsync(int id, string name, CancellationToken ct = default);
    Task<bool> RenameDepartmentAsync(int id, string name, CancellationToken ct = default);
    Task<LookupItem?> GetCategoryAsync(int id, CancellationToken ct = default);
    Task<LookupItem?> GetDepartmentAsync(int id, CancellationToken ct = default);
}
