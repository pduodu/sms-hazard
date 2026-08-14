using System.ComponentModel.DataAnnotations;
using SMSHazard.Domain.Enums;

namespace SMSHazard.Web.Models.Capa;

public class UpdateCapaViewModel
{
    public int CapaId { get; set; }
    public int HazardId { get; set; }
    public string HazardRef { get; set; } = string.Empty;
    public string HazardTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Status")]
    public CapaStatus Status { get; set; }

    [StringLength(2000), Display(Name = "Progress / evidence note")]
    public string? ProgressNote { get; set; }
}
