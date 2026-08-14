namespace SMSHazard.Domain.Common;

/// <summary>Base for all persisted entities: identity plus audit timestamps.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
