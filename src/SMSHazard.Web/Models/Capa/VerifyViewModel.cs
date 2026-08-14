using System.ComponentModel.DataAnnotations;

namespace SMSHazard.Web.Models.Capa;

public class VerifyViewModel
{
    public int HazardId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Select a residual likelihood (1–5).")]
    [Display(Name = "Residual likelihood")]
    public int Likelihood { get; set; } = 2;

    [Range(1, 5, ErrorMessage = "Select a residual severity (1–5).")]
    [Display(Name = "Residual severity")]
    public int Severity { get; set; } = 2;

    [Required, StringLength(2000), Display(Name = "Effectiveness note")]
    public string EffectivenessNote { get; set; } = string.Empty;
}
