using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SMSHazard.Domain.Enums;

namespace SMSHazard.Web.Models.Capa;

public class CreateCapaViewModel
{
    public int HazardId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Action type")]
    public CapaType Type { get; set; } = CapaType.Corrective;

    [Required, Display(Name = "Assign to")]
    public string AssignedToId { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Due date")]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    public IEnumerable<SelectListItem> Owners { get; set; } = new List<SelectListItem>();
}
