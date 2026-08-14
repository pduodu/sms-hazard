namespace SMSHazard.Application.Common;

public sealed class StorageSettings
{
    /// <summary>Absolute path to the attachments directory (outside the deploy dir on the VPS).</summary>
    public string AttachmentsPath { get; set; } = string.Empty;
    public long MaxFileBytes { get; set; } = 25 * 1024 * 1024; // 25 MB
}
