using SMSHazard.Domain.Enums;

namespace SMSHazard.Web.Models.Hazards;

public static class HazardDisplay
{
    public static string RiskCss(RiskLevel? level) => level switch
    {
        RiskLevel.Low => "badge-green",
        RiskLevel.Medium => "badge-yellow",
        RiskLevel.High => "badge-orange",
        RiskLevel.Extreme => "badge-red",
        _ => "badge-gray"
    };

    public static string StatusCss(HazardStatus status) => status switch
    {
        HazardStatus.Reported => "badge-gray",
        HazardStatus.UnderAssessment => "badge-blue",
        HazardStatus.ActionRequired => "badge-yellow",
        HazardStatus.InProgress => "badge-blue",
        HazardStatus.UnderVerification => "badge-blue",
        HazardStatus.Closed => "badge-green",
        HazardStatus.Rejected => "badge-gray",
        _ => "badge-gray"
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
