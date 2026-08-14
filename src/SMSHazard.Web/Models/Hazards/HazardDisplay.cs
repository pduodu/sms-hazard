using SMSHazard.Domain.Enums;

namespace SMSHazard.Web.Models.Hazards;

public static class HazardDisplay
{
    public static string RiskCss(RiskLevel? level) => level switch
    {
        RiskLevel.Low => "bg-success",
        RiskLevel.Medium => "bg-warning text-dark",
        RiskLevel.High => "bg-orange text-white",
        RiskLevel.Extreme => "bg-danger",
        _ => "bg-secondary"
    };

    public static string StatusCss(HazardStatus status) => status switch
    {
        HazardStatus.Reported => "bg-secondary",
        HazardStatus.UnderAssessment => "bg-info text-dark",
        HazardStatus.ActionRequired => "bg-warning text-dark",
        HazardStatus.InProgress => "bg-primary",
        HazardStatus.UnderVerification => "bg-info text-dark",
        HazardStatus.Closed => "bg-success",
        HazardStatus.Rejected => "bg-dark",
        _ => "bg-secondary"
    };

    public static string StatusText(HazardStatus status) => status switch
    {
        HazardStatus.UnderAssessment => "Under Assessment",
        HazardStatus.ActionRequired => "Action Required",
        HazardStatus.InProgress => "In Progress",
        HazardStatus.UnderVerification => "Under Verification",
        _ => status.ToString()
    };
}
