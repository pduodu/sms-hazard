using Microsoft.AspNetCore.Mvc.Rendering;
using SMSHazard.Application.DTOs;
using SMSHazard.Domain.Enums;

namespace SMSHazard.Web.Models.Hazards;

public class HazardListViewModel
{
    public string Heading { get; set; } = "Hazards";
    public IReadOnlyList<HazardListItemDto> Items { get; set; } = new List<HazardListItemDto>();

    // filter state (null = any)
    public HazardStatus? Status { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public int? DepartmentId { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
    public bool ShowFilters { get; set; } = true;
}
