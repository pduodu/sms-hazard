using SMSHazard.Domain.Enums;

namespace SMSHazard.Application.DTOs;

/// <summary>A user who can be assigned as a CAPA action owner (Identity ids are strings).</summary>
public sealed record UserOption(string Id, string Name);

public sealed class CreateCapaRequest
{
    public int HazardId { get; set; }
    public string Description { get; set; } = string.Empty;
    public CapaType Type { get; set; }
    public string AssignedToId { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

/// <summary>A row in an owner's "My actions" list.</summary>
public sealed class MyActionDto
{
    public int CapaId { get; set; }
    public int HazardId { get; set; }
    public string HazardRef { get; set; } = string.Empty;
    public string HazardTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CapaType Type { get; set; }
    public DateTime DueDate { get; set; }
    public CapaStatus Status { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>Data needed to render the owner's progress-update form.</summary>
public sealed class CapaEditDto
{
    public int CapaId { get; set; }
    public int HazardId { get; set; }
    public string HazardRef { get; set; } = string.Empty;
    public string HazardTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssignedToId { get; set; } = string.Empty;
    public CapaStatus Status { get; set; }
    public string? ProgressNote { get; set; }
}

/// <summary>Outcome of a verify-and-close attempt.</summary>
public sealed record VerifyOutcome(bool Closed, RiskLevel ResidualLevel, int ResidualScore);
