using SMSHazard.Domain.Common;

namespace SMSHazard.Domain.Entities;

public class Attachment : BaseEntity
{
    public int HazardReportId { get; set; }
    public HazardReport? HazardReport { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    /// <summary>Relative key/path under the configured attachments dir (or blob key).</summary>
    public string StorageKey { get; set; } = string.Empty;
    public string UploadedById { get; set; } = string.Empty;
}
