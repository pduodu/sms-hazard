using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Common;
using SMSHazard.Domain.Entities;

namespace SMSHazard.Infrastructure.Persistence;

/// <summary>
/// Captures create/update/delete of the core safety entities into <see cref="AuditLog"/>,
/// and stamps <c>CreatedAt</c>/<c>UpdatedAt</c> on every entity. Audit rows for Added entities
/// are written after the first save (so store-generated keys are known), in a second save.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _current;

    // Per-DbContext scratch space, safe even if the interceptor is shared.
    private static readonly ConditionalWeakTable<DbContext, List<Pending>> _pending = new();

    private static readonly Type[] Audited =
    {
        typeof(HazardReport), typeof(RiskAssessment), typeof(CorrectiveAction)
    };

    public AuditSaveChangesInterceptor(ICurrentUser current) => _current = current;

    private sealed record Pending(EntityEntry Entry, string EntityName, string Action, string Summary);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Prepare(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Prepare(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        await FlushAsync(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private void Prepare(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;
        var pending = new List<Pending>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Stamp timestamps on all entities.
            if (entry.Entity is BaseEntity be)
            {
                if (entry.State == EntityState.Added && be.CreatedAt == default) be.CreatedAt = now;
                if (entry.State == EntityState.Modified) be.UpdatedAt = now;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;
            if (!Audited.Contains(entry.Entity.GetType()))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                _ => "Updated"
            };
            var summary = entry.State == EntityState.Modified ? Summarize(entry) : action;
            pending.Add(new Pending(entry, entry.Entity.GetType().Name, action, summary));
        }

        _pending.Remove(context);
        _pending.Add(context, pending);
    }

    private void Flush(DbContext? context) => FlushAsync(context, default).GetAwaiter().GetResult();

    private async Task FlushAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;
        if (!_pending.TryGetValue(context, out var pending)) return;
        _pending.Remove(context);
        if (pending.Count == 0) return;

        var user = _current.UserId ?? "system";
        foreach (var p in pending)
        {
            context.Set<AuditLog>().Add(new AuditLog
            {
                EntityName = p.EntityName,
                EntityId = p.Entry.Property("Id").CurrentValue?.ToString() ?? "",
                Action = p.Action,
                ChangedById = user,
                Timestamp = DateTime.UtcNow,
                ChangeSummary = Trunc(p.Summary),
                CreatedAt = DateTime.UtcNow
            });
        }
        // Second save persists the audit rows; it re-enters the interceptor but has no audited
        // Added/Modified entities (AuditLog is not audited), so it does not recurse.
        await context.SaveChangesAsync(ct);
    }

    private static string Summarize(EntityEntry entry)
    {
        var changes = entry.Properties
            .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
            .Select(p => $"{p.Metadata.Name}: {p.OriginalValue} -> {p.CurrentValue}");
        var text = string.Join("; ", changes);
        return string.IsNullOrWhiteSpace(text) ? "Updated" : text;
    }

    private static string Trunc(string s) => s.Length <= 1900 ? s : s[..1900] + "…";
}
