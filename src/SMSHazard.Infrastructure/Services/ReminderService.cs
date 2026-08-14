using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

/// <summary>
/// Scans corrective actions for approaching-due (T-2 days) and overdue items and reminds the
/// owner (and safety officers for overdue). Idempotent via <c>LastRemindedAt</c> — a given action
/// is reminded at most once per day, so Hangfire retries never double-send.
/// </summary>
public sealed class ReminderService : IReminderService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(AppDbContext db, INotificationService notify, ILogger<ReminderService> logger)
    {
        _db = db;
        _notify = notify;
        _logger = logger;
    }

    public async Task ProcessDueRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var horizon = today.AddDays(2); // approaching-due window (T-2 days)

        var due = await _db.CorrectiveActions
            .Include(c => c.HazardReport)
            .Where(c =>
                c.Status != CapaStatus.Completed &&
                c.Status != CapaStatus.Verified &&
                c.DueDate.Date <= horizon &&
                (c.LastRemindedAt == null || c.LastRemindedAt < today))
            .ToListAsync();

        if (due.Count == 0)
        {
            _logger.LogInformation("Reminder scan: nothing due at {Time}.", now);
            return;
        }

        foreach (var capa in due)
        {
            var reference = capa.HazardReport!.ReferenceNo;
            var link = $"/Hazards/Details/{capa.HazardReportId}";
            var overdue = capa.DueDate.Date < today;
            var state = overdue ? "overdue" : "approaching its due date";

            // Owner is always reminded.
            await _notify.NotifyUserAsync(capa.AssignedToId,
                $"Corrective action {state}: {reference}",
                $"\"{capa.Description}\" is {state} (due {capa.DueDate:dd MMM yyyy}).",
                link, alsoEmail: true);

            // Safety officers are escalated only for overdue actions.
            if (overdue)
                await _notify.NotifyRoleAsync("SafetyOfficer",
                    $"Overdue corrective action: {reference}",
                    $"\"{capa.Description}\" is overdue (due {capa.DueDate:dd MMM yyyy}).",
                    link, alsoEmail: true);

            capa.LastRemindedAt = now; // idempotency guard
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Reminder scan: processed {Count} action(s) at {Time}.", due.Count, now);
    }
}
