using System.ComponentModel.DataAnnotations;
using SMSHazard.Application.DTOs;

namespace SMSHazard.Web.Models.Config;

public class ConfigIndexViewModel
{
    public IReadOnlyList<LookupItem> Categories { get; set; } = new List<LookupItem>();
    public IReadOnlyList<LookupItem> Departments { get; set; } = new List<LookupItem>();
}

public class EditLookupViewModel
{
    public int Id { get; set; }
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Category"; // "Category" | "Department"
}
