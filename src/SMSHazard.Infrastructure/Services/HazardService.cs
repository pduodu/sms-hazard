using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Entities;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

/// <summary>Hazard reporting use cases over EF Core. Queries project to DTOs to avoid N+1.</summary>
public sealed class HazardService : IHazardService
{
    private readonly AppDbContext _db;
    private readonly IAttachmentStorage _storage;
    private readonly INotificationService _notify;

    public HazardService(AppDbContext db, IAttachmentStorage storage, INotificationService notify)
    {
        _db = db;
        _storage = storage;
        _notify = notify;
    }

    public async Task<IReadOnlyList<LookupItem>> GetCategoriesAsync(CancellationToken ct = default) =>
        await _db.HazardCategories.OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name)).ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetDepartmentsAsync(CancellationToken ct = default) =>
        await _db.Departments.OrderBy(d => d.Name)
            .Select(d => new LookupItem(d.Id, d.Name)).ToListAsync(ct);

    public async Task<(int Id, string ReferenceNo)> CreateAsync(
        CreateHazardRequest request, IReadOnlyList<AttachmentUpload> attachments,
        string reporterId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var referenceNo = await GenerateReferenceNoAsync(now.Year, ct);

        var hazard = new HazardReport
        {
            ReferenceNo = referenceNo,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            HazardCategoryId = request.HazardCategoryId,
            DepartmentId = request.DepartmentId,
            ReportedById = reporterId,
            ReportedDate = now,
            OccurrenceDate = request.OccurrenceDate,
            ImmediateActionTaken = request.ImmediateActionTaken?.Trim(),
            CreatedAt = now
        };

        foreach (var upload in attachments)
        {
            var key = await _storage.SaveAsync(upload, ct);
            hazard.Attachments.Add(new Attachment
            {
                FileName = Path.GetFileName(upload.FileName),
                ContentType = upload.ContentType,
                SizeBytes = upload.Length,
                StorageKey = key,
                UploadedById = reporterId,
                CreatedAt = now
            });
        }

        _db.HazardReports.Add(hazard);
        await _db.SaveChangesAsync(ct);

        await _notify.NotifyRoleAsync("SafetyOfficer",
            $"New hazard reported: {hazard.ReferenceNo}",
            $"\"{hazard.Title}\" was reported and needs assessment.",
            $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return (hazard.Id, hazard.ReferenceNo);
    }

    private async Task<string> GenerateReferenceNoAsync(int year, CancellationToken ct)
    {
        var prefix = $"HZ-{year}-";
        // Count existing hazards for the year; next sequence = count + 1.
        // (A DB sequence would be more robust under high concurrency — noted as minor technical debt.)
        var count = await _db.HazardReports.CountAsync(h => h.ReferenceNo.StartsWith(prefix), ct);
        return $"{prefix}{(count + 1):D4}";
    }

    public async Task<(int Id, string ReferenceNo, string TrackingCode)> CreateAnonymousAsync(
        CreateHazardRequest request, IReadOnlyList<AttachmentUpload> attachments, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var referenceNo = await GenerateReferenceNoAsync(now.Year, ct);
        var trackingCode = await GenerateTrackingCodeAsync(ct);

        var hazard = new HazardReport
        {
            ReferenceNo = referenceNo,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            HazardCategoryId = request.HazardCategoryId,
            DepartmentId = request.DepartmentId,
            ReportedById = string.Empty,   // no authenticated user
            IsAnonymous = true,
            TrackingCode = trackingCode,
            ReportedDate = now,
            OccurrenceDate = request.OccurrenceDate,
            ImmediateActionTaken = request.ImmediateActionTaken?.Trim(),
            CreatedAt = now
        };

        foreach (var upload in attachments)
        {
            var key = await _storage.SaveAsync(upload, ct);
            hazard.Attachments.Add(new Attachment
            {
                FileName = Path.GetFileName(upload.FileName),
                ContentType = upload.ContentType,
                SizeBytes = upload.Length,
                StorageKey = key,
                UploadedById = string.Empty,
                CreatedAt = now
            });
        }

        _db.HazardReports.Add(hazard);
        await _db.SaveChangesAsync(ct);

        await _notify.NotifyRoleAsync("SafetyOfficer",
            $"New anonymous hazard reported: {hazard.ReferenceNo}",
            $"\"{hazard.Title}\" was submitted anonymously and needs assessment.",
            $"/Hazards/Details/{hazard.Id}", alsoEmail: true, ct);

        return (hazard.Id, hazard.ReferenceNo, trackingCode);
    }

    public async Task<PublicTrackDto?> TrackAsync(string trackingCode, CancellationToken ct = default)
    {
        var code = (trackingCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length == 0) return null;

        return await _db.HazardReports.AsNoTracking()
            .Where(h => h.TrackingCode == code)
            .Select(h => new PublicTrackDto
            {
                ReferenceNo = h.ReferenceNo,
                Title = h.Title,
                CategoryName = h.HazardCategory!.Name,
                DepartmentName = h.Department!.Name,
                Status = h.Status,
                ReportedDate = h.ReportedDate,
                CurrentRiskLevel = h.Assessments.OrderByDescending(a => a.AssessedDate)
                    .Select(a => (RiskLevel?)a.RiskLevel).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);
    }

    // Unambiguous alphabet (no 0/O/1/I) for human-readable tracking codes, e.g. TR-7K9F2Q4M.
    private static readonly char[] CodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

    private async Task<string> GenerateTrackingCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var bytes = RandomNumberGenerator.GetBytes(8);
            var sb = new StringBuilder("TR-");
            foreach (var b in bytes) sb.Append(CodeAlphabet[b % CodeAlphabet.Length]);
            var code = sb.ToString();
            if (!await _db.HazardReports.AnyAsync(h => h.TrackingCode == code, ct))
                return code;
        }
        // Extremely unlikely; fall back to a GUID-derived code.
        return "TR-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }

    public async Task<IReadOnlyList<HazardListItemDto>> ListAsync(HazardFilter filter, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var q = _db.HazardReports.AsNoTracking().AsQueryable();

        if (filter.Status is not null) q = q.Where(h => h.Status == filter.Status);
        if (filter.DepartmentId is not null) q = q.Where(h => h.DepartmentId == filter.DepartmentId);
        if (filter.ReporterId is not null) q = q.Where(h => h.ReportedById == filter.ReporterId);
        if (filter.FromDate is not null) q = q.Where(h => h.ReportedDate >= filter.FromDate);
        if (filter.ToDate is not null) q = q.Where(h => h.ReportedDate <= filter.ToDate);

        var projected = q.Select(h => new HazardListItemDto
        {
            Id = h.Id,
            ReferenceNo = h.ReferenceNo,
            Title = h.Title,
            CategoryName = h.HazardCategory!.Name,
            DepartmentName = h.Department!.Name,
            Status = h.Status,
            ReportedDate = h.ReportedDate,
            ReporterName = _db.Users.Where(u => u.Id == h.ReportedById).Select(u => u.FullName).FirstOrDefault() ?? "",
            // latest (most recent) assessment drives the shown risk level
            CurrentRiskScore = h.Assessments.OrderByDescending(a => a.AssessedDate).Select(a => (int?)a.RiskScoreValue).FirstOrDefault(),
            CurrentRiskLevel = h.Assessments.OrderByDescending(a => a.AssessedDate).Select(a => (RiskLevel?)a.RiskLevel).FirstOrDefault(),
            OverdueCount = h.CorrectiveActions.Count(c =>
                c.DueDate < today && c.Status != CapaStatus.Completed && c.Status != CapaStatus.Verified)
        });

        if (filter.RiskLevel is not null)
            projected = projected.Where(d => d.CurrentRiskLevel == filter.RiskLevel);

        return await projected.OrderByDescending(d => d.ReportedDate).ToListAsync(ct);
    }

    public async Task<HazardDetailDto?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var dto = await _db.HazardReports.AsNoTracking()
            .Where(h => h.Id == id)
            .Select(h => new HazardDetailDto
            {
                Id = h.Id,
                ReferenceNo = h.ReferenceNo,
                Title = h.Title,
                Description = h.Description,
                CategoryName = h.HazardCategory!.Name,
                DepartmentName = h.Department!.Name,
                ReporterId = h.ReportedById,
                ReporterName = _db.Users.Where(u => u.Id == h.ReportedById).Select(u => u.FullName).FirstOrDefault() ?? "",
                ReportedDate = h.ReportedDate,
                OccurrenceDate = h.OccurrenceDate,
                ImmediateActionTaken = h.ImmediateActionTaken,
                Status = h.Status,
                IsAnonymous = h.IsAnonymous,
                TrackingCode = h.TrackingCode,
                Assessments = h.Assessments.OrderBy(a => a.AssessedDate).Select(a => new AssessmentDto
                {
                    Likelihood = a.Likelihood,
                    Severity = a.Severity,
                    RiskScore = a.RiskScoreValue,
                    RiskLevel = a.RiskLevel,
                    Rationale = a.Rationale,
                    AssessedByName = _db.Users.Where(u => u.Id == a.AssessedById).Select(u => u.FullName).FirstOrDefault() ?? "",
                    AssessedDate = a.AssessedDate,
                    IsResidual = a.IsResidual
                }).ToList(),
                CorrectiveActions = h.CorrectiveActions.OrderBy(c => c.DueDate).Select(c => new CapaDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Type = c.Type,
                    AssignedToName = _db.Users.Where(u => u.Id == c.AssignedToId).Select(u => u.FullName).FirstOrDefault() ?? "",
                    DueDate = c.DueDate,
                    Status = c.Status,
                    IsOverdue = c.DueDate < today && c.Status != CapaStatus.Completed && c.Status != CapaStatus.Verified
                }).ToList(),
                Attachments = h.Attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        return dto;
    }

    public async Task<(AttachmentDto Meta, string ReporterId, Stream Content)?> OpenAttachmentAsync(int attachmentId, CancellationToken ct = default)
    {
        var att = await _db.Attachments.AsNoTracking()
            .Where(a => a.Id == attachmentId)
            .Select(a => new
            {
                a.Id, a.FileName, a.ContentType, a.SizeBytes, a.StorageKey,
                ReporterId = a.HazardReport!.ReportedById
            })
            .FirstOrDefaultAsync(ct);
        if (att is null) return null;

        var stream = await _storage.OpenReadAsync(att.StorageKey, ct);
        if (stream is null) return null;

        var meta = new AttachmentDto
        {
            Id = att.Id,
            FileName = att.FileName,
            ContentType = att.ContentType,
            SizeBytes = att.SizeBytes
        };
        return (meta, att.ReporterId, stream);
    }
}
