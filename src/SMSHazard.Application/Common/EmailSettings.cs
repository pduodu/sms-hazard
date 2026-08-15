namespace SMSHazard.Application.Common;

/// <summary>SMTP configuration bound from the "Email" configuration section (env file on the VPS).</summary>
public sealed class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "SMS-Hazard";

    /// <summary>
    /// Transport security: Auto (default, negotiates per server/port), None, StartTls,
    /// StartTlsWhenAvailable, or SslOnConnect. Mailjet uses StartTls on 587 or SslOnConnect on 465.
    /// </summary>
    public string Security { get; set; } = "Auto";
}
