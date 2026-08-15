using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMSHazard.Application.Common;
using SMSHazard.Application.Interfaces;
using SMSHazard.Domain.Enums;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Infrastructure.Persistence;

namespace SMSHazard.Infrastructure.Services;

/// <summary>
/// Builds and emails a monthly safety digest (new/open/closed hazards, high-risk and overdue counts)
/// to every active Manager and Admin. Runs inside a Hangfire job, so it sends email synchronously;
/// the SMTP sender swallows failures so a mail outage never fails the job.
/// </summary>
public sealed class DigestService : IDigestService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<DigestService> _logger;

    public DigestService(AppDbContext db, IEmailSender email,
        UserManager<ApplicationUser> users, ILogger<DigestService> logger)
    {
        _db = db;
        _email = email;
        _users = users;
        _logger = logger;
    }

    public async Task SendMonthlyDigestAsync()
    {
        var now = DateTime.UtcNow;
        var from = now.AddDays(-30);
        var today = now.Date;

        var newHazards = await _db.HazardReports.CountAsync(h => h.ReportedDate >= from);
        var openHazards = await _db.HazardReports.CountAsync(h =>
            h.Status != HazardStatus.Closed && h.Status != HazardStatus.Rejected);
        var closedThisPeriod = await _db.HazardReports.CountAsync(h =>
            h.Status == HazardStatus.Closed && h.UpdatedAt != null && h.UpdatedAt >= from);

        var highExtremeOpen = await _db.HazardReports
            .Where(h => h.Status != HazardStatus.Closed && h.Status != HazardStatus.Rejected)
            .Select(h => h.Assessments
                .OrderByDescending(a => a.AssessedDate)
                .Select(a => (RiskLevel?)a.RiskLevel)
                .FirstOrDefault())
            .CountAsync(l => l == RiskLevel.High || l == RiskLevel.Extreme);

        var overdueActions = await _db.CorrectiveActions.CountAsync(c =>
            c.DueDate < today && c.Status != CapaStatus.Completed && c.Status != CapaStatus.Verified);

        var period = $"{from:dd MMM} – {now:dd MMM yyyy}";
        var subject = $"SMS-Hazard monthly safety digest ({now:MMMM yyyy})";
        var html = BuildHtml(period, newHazards, openHazards, closedThisPeriod, highExtremeOpen, overdueActions);

        // Recipients: active Managers and Admins, de-duplicated.
        var managers = await _users.GetUsersInRoleAsync("Manager");
        var admins = await _users.GetUsersInRoleAsync("Admin");
        var recipients = managers.Concat(admins)
            .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
            .GroupBy(u => u.Id)
            .Select(g => g.First())
            .ToList();

        foreach (var user in recipients)
            await _email.SendAsync(user.Email!, subject, html);

        _logger.LogInformation("Monthly digest sent to {Count} recipient(s) at {Time}.", recipients.Count, now);
    }

    private static string BuildHtml(string period, int newHazards, int openHazards,
        int closedThisPeriod, int highExtremeOpen, int overdueActions)
    {
        static string Row(string label, int value, string colour) =>
            $@"<tr>
                 <td style=""padding:10px 14px;border-bottom:1px solid #edf1f6;color:#4b5675;font-size:15px"">{label}</td>
                 <td style=""padding:10px 14px;border-bottom:1px solid #edf1f6;text-align:right;font-weight:700;font-size:16px;color:{colour}"">{value}</td>
               </tr>";

        var body = $@"<p style=""margin:0 0 4px 0;font-size:15px;color:#4b5675;"">Reporting period: <strong style=""color:#132852"">{period}</strong></p>
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;width:100%;margin-top:14px;border:1px solid #edf1f6;border-radius:10px;overflow:hidden"">
  {Row("New hazards reported", newHazards, "#045C9D")}
  {Row("Currently open", openHazards, "#132852")}
  {Row("Closed in period", closedThisPeriod, "#16A34A")}
  {Row("Open High / Extreme risk", highExtremeOpen, "#DC2626")}
  {Row("Overdue corrective actions", overdueActions, "#DC2626")}
</table>
<p style=""color:#94a3b8;font-size:13px;margin:16px 0 0 0;"">Sign in to review the risk register and dashboard.</p>";

        return EmailTemplate.Render(title: "Monthly safety digest", bodyHtml: body);
    }
}
