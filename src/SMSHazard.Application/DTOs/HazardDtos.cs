using SMSHazard.Domain.Enums;

namespace SMSHazard.Application.DTOs;

/// <summary>A file to be stored, decoupled from ASP.NET's IFormFile so the Application stays host-agnostic.</summary>
public sealed record AttachmentUpload(string FileName, string ContentType, long Length, Stream Content);

/// <summary>Simple id/name pair for dropdowns.</summary>
public sealed record LookupItem(int Id, string Name);

public sealed class CreateHazardRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HazardCategoryId { get; set; }
    public int DepartmentId { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public string? ImmediateActionTaken { get; set; }
}

public sealed class HazardFilter
{
    public HazardStatus? Status { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    /// <summary>When set, restrict to hazards reported by this user (for "My Reports").</summary>
    public string? ReporterId { get; set; }
}

public sealed class HazardListItemDto
{
    public int Id { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public HazardStatus Status { get; set; }
    public RiskLevel? CurrentRiskLevel { get; set; }
    public int? CurrentRiskScore { get; set; }
    public DateTime ReportedDate { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public int OverdueCount { get; set; }
}

public sealed class AssessmentDto
{
    public int Likelihood { get; set; }
    public int Severity { get; set; }
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string AssessedByName { get; set; } = string.Empty;
    public DateTime AssessedDate { get; set; }
    public bool IsResidual { get; set; }
}

public sealed class CapaDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public CapaType Type { get; set; }
    public string AssignedToName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public CapaStatus Status { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public sealed class HazardDetailDto
{
    public int Id { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterId { get; set; } = string.Empty;
    public DateTime ReportedDate { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public string? ImmediateActionTaken { get; set; }
    public HazardStatus Status { get; set; }
    public bool IsAnonymous { get; set; }
    public string? TrackingCode { get; set; }
    public List<AssessmentDto> Assessments { get; set; } = new();
    public List<CapaDto> CorrectiveActions { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
}

/// <summary>Read-only status view returned to an anonymous reporter who enters their tracking code.</summary>
public sealed class PublicTrackDto
{
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public HazardStatus Status { get; set; }
    public RiskLevel? CurrentRiskLevel { get; set; }
    public DateTime ReportedDate { get; set; }
}
