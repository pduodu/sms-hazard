using Microsoft.Extensions.Options;
using SMSHazard.Application.Common;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;

namespace SMSHazard.Infrastructure.Storage;

/// <summary>
/// Stores attachments on the persistent filesystem under the configured attachments path
/// (outside the deploy dir on the VPS). Falls back to a local folder if unconfigured.
/// Files are served only through an authorised controller action, never static hosting.
/// </summary>
public sealed class FileAttachmentStorage : IAttachmentStorage
{
    private readonly string _root;

    public FileAttachmentStorage(IOptions<StorageSettings> settings)
    {
        var configured = settings.Value.AttachmentsPath;
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "attachments")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(AttachmentUpload upload, CancellationToken ct = default)
    {
        // Year/month subfolders keep directories manageable; GUID avoids collisions.
        var now = DateTime.UtcNow;
        var subDir = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"));
        var absDir = Path.Combine(_root, subDir);
        Directory.CreateDirectory(absDir);

        var ext = Path.GetExtension(upload.FileName);
        var key = Path.Combine(subDir, $"{Guid.NewGuid():N}{ext}");
        var absPath = Path.Combine(_root, key);

        await using (var fs = new FileStream(absPath, FileMode.CreateNew, FileAccess.Write))
        {
            await upload.Content.CopyToAsync(fs, ct);
        }
        return key.Replace('\\', '/'); // store with forward slashes for portability
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var absPath = Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absPath))
            return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(absPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }
}
