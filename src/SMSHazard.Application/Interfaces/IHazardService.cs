using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Interfaces;

public interface IHazardService
{
    Task<IReadOnlyList<LookupItem>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupItem>> GetDepartmentsAsync(CancellationToken ct = default);

    /// <summary>Creates a hazard (status Reported), stores attachments, returns the new id and reference no.</summary>
    Task<(int Id, string ReferenceNo)> CreateAsync(
        CreateHazardRequest request, IReadOnlyList<AttachmentUpload> attachments,
        string reporterId, CancellationToken ct = default);

    Task<IReadOnlyList<HazardListItemDto>> ListAsync(HazardFilter filter, CancellationToken ct = default);
    Task<HazardDetailDto?> GetDetailAsync(int id, CancellationToken ct = default);

    /// <summary>Returns an attachment's metadata, its hazard's reporter id (for access checks), and a read stream.</summary>
    Task<(AttachmentDto Meta, string ReporterId, Stream Content)?> OpenAttachmentAsync(int attachmentId, CancellationToken ct = default);
}
