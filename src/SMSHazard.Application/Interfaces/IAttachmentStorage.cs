using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

/// <summary>Persists hazard attachments outside the deploy dir; implemented in Infrastructure.</summary>
public interface IAttachmentStorage
{
    /// <summary>Saves the upload and returns an opaque storage key.</summary>
    Task<string> SaveAsync(AttachmentUpload upload, CancellationToken ct = default);

    /// <summary>Opens a previously stored file for reading, or null if missing.</summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);
}
