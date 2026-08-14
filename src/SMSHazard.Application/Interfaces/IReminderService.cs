namespace SMSHazard.Application.Interfaces;

/// <summary>
/// Scans CAPAs for approaching-due (T-2d) and overdue items, notifying owner + safety officer.
/// Invoked hourly by Hangfire; must be idempotent (LastRemindedAt guard).
/// </summary>
public interface IReminderService
{
    Task ProcessDueRemindersAsync();
}
