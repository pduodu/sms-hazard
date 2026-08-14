namespace SMSHazard.Application.Interfaces;

/// <summary>Abstraction over outbound email; implemented in Infrastructure via MailKit/SMTP.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
