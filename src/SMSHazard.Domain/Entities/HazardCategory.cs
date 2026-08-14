using SMSHazard.Domain.Common;

namespace SMSHazard.Domain.Entities;

public class HazardCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<HazardReport> Hazards { get; set; } = new List<HazardReport>();
}
