using System.ComponentModel.DataAnnotations;
using SMSHazard.Domain.Enums;

namespace SMSHazard.Web.Models.Hazards;

public class AssessViewModel
{
    public int HazardId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public HazardStatus CurrentStatus { get; set; }

    [Range(1, 5, ErrorMessage = "Select a likelihood (1–5).")]
    [Display(Name = "Likelihood")]
    public int Likelihood { get; set; } = 3;

    [Range(1, 5, ErrorMessage = "Select a severity (1–5).")]
    [Display(Name = "Severity")]
    public int Severity { get; set; } = 3;

    [Required, StringLength(2000)]
    [Display(Name = "Assessor rationale")]
    public string Rationale { get; set; } = string.Empty;

    /// <summary>True when recording a post-mitigation residual assessment (used at verification).</summary>
    public bool IsResidual { get; set; }
}
