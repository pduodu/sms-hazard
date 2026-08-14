using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SMSHazard.Web.Models.Hazards;

public class CreateHazardViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Required, Display(Name = "Category")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int HazardCategoryId { get; set; }

    [Required, Display(Name = "Department / Location")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a department.")]
    public int DepartmentId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Occurrence date")]
    public DateTime OccurrenceDate { get; set; } = DateTime.UtcNow.Date;

    [StringLength(2000), Display(Name = "Immediate action taken")]
    [DataType(DataType.MultilineText)]
    public string? ImmediateActionTaken { get; set; }

    [Display(Name = "Evidence attachments")]
    public List<IFormFile>? Attachments { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}
