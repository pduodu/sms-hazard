using SMSHazard.Application.DTOs;
using SMSHazard.Domain.Enums;

namespace SMSHazard.Application.Interfaces;

public interface ICapaService
{
    /// <summary>Active users who can be assigned as action owners.</summary>
    Task<IReadOnlyList<UserOption>> GetAssignableUsersAsync(CancellationToken ct = default);

    /// <summary>Assigns a corrective/preventive action; advances Action Required → In Progress.</summary>
    Task<bool> CreateAsync(CreateCapaRequest request, string officerId, CancellationToken ct = default);

    /// <summary>Actions assigned to a given owner (for "My actions").</summary>
    Task<IReadOnlyList<MyActionDto>> MyActionsAsync(string ownerId, CancellationToken ct = default);

    Task<CapaEditDto?> GetForUpdateAsync(int capaId, CancellationToken ct = default);

    /// <summary>Owner updates progress; when all actions complete, advances In Progress → Under Verification.</summary>
    Task<bool> UpdateProgressAsync(int capaId, CapaStatus newStatus, string? note, string userId, bool isStaff, CancellationToken ct = default);

    /// <summary>
    /// Records residual risk + effectiveness, verifies the actions, and either closes the hazard
    /// (residual acceptable = Low/Medium) or returns it to Action Required (residual High/Extreme).
    /// </summary>
    Task<VerifyOutcome?> VerifyAndCloseAsync(int hazardId, int likelihood, int severity,
        string effectivenessNote, string officerId, CancellationToken ct = default);

    Task<bool> RejectAsync(int hazardId, string reason, string officerId, CancellationToken ct = default);
    Task<bool> ReopenAsync(int hazardId, string officerId, CancellationToken ct = default);
}
