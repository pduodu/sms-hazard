namespace SMSHazard.Application.Interfaces;

/// <summary>
/// Emails a monthly safety summary to managers and admins. Invoked by Hangfire on a monthly
/// schedule (and on demand from Settings). Safe to run repeatedly — it only reads and sends.
/// </summary>
public interface IDigestService
{
    Task SendMonthlyDigestAsync();
}
