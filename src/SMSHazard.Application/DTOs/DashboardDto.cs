namespace SMSHazard.Application.DTOs;

/// <summary>Management dashboard KPIs and risk distribution (assurance view).</summary>
public sealed class DashboardDto
{
    public int TotalHazards { get; set; }
    public int OpenHazards { get; set; }          // not Closed/Rejected
    public int OverdueActions { get; set; }
    public int ClosedThisPeriod { get; set; }     // closed in the last 30 days
    public int HighRiskHazards { get; set; }       // current level High or Extreme

    // Risk-level distribution (by each hazard's latest assessment)
    public int Low { get; set; }
    public int Medium { get; set; }
    public int High { get; set; }
    public int Extreme { get; set; }
    public int NotAssessed { get; set; }
}
