using System.Net;

namespace SMSHazard.Application.Common;

/// <summary>
/// Wraps email content in a single branded, responsive HTML shell so every message the app sends
/// (notifications, password resets, the monthly digest) looks consistent. Based on the approved
/// SMS-Hazard email template.
/// </summary>
public static class EmailTemplate
{
    private const string Shell = """
<!DOCTYPE html>
<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <title>{TITLE}</title>
    <style>
        body { margin:0; padding:0; -webkit-text-size-adjust:100%; text-size-adjust:100%; -ms-text-size-adjust:100%; }
        table { border-collapse:collapse; }
        img { border:0; line-height:100%; outline:none; text-decoration:none; }
        a { text-decoration:none; }
        @media only screen and (max-width:620px) { .container { width:100% !important; } .px { padding-left:22px !important; padding-right:22px !important; } .h1 { font-size:24px !important; line-height:30px !important; } }
    </style>
</head>
<body style="margin:0; padding:0; background-color:#eef2f6; font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
    <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:#eef2f6; font-size:1px; line-height:1px;">{TITLE} from {COMPANY}</div>
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#eef2f6;">
        <tr>
            <td align="center" style="padding:28px 12px;">
                <table role="presentation" class="container" width="600" cellpadding="0" cellspacing="0" style="width:600px; max-width:600px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 28px rgba(1,49,97,0.12);">
                    <tr>
                        <td class="px" style="background:#045C9D; background-image:linear-gradient(135deg,#1086D4 0%,#045C9D 55%,#013161 100%); padding:34px 36px 30px 36px;">
                            <p style="margin:0 0 6px 0; font-size:12px; letter-spacing:2px; text-transform:uppercase; color:#cfe6f7; font-weight:600; text-align:center;">{COMPANY}</p>
                            <h1 class="h1" style="margin:0; font-size:30px; line-height:36px; color:#ffffff; font-weight:700; text-align:center;">{TITLE}</h1>
                        </td>
                    </tr>
                    <tr>
                        <td class="px" style="padding:30px 36px 8px 36px; font-size:15px; line-height:23px; color:#4b5675;">
                            {BODY}
                        </td>
                    </tr>
                    <tr>
                        <td class="px" style="padding:0 36px 30px 36px;">
                            {BUTTON}
                        </td>
                    </tr>
                    <tr>
                        <td class="px" style="background-color:#f4f6f9; padding:22px 36px; border-top:1px solid #e6eaf0;">
                            <p style="margin:0 0 8px 0; font-size:12px; line-height:18px; color:#47484a;">You received this email from <strong style="color:#132852;">{COMPANY}</strong>.</p>
                            <p style="margin:0; font-size:11px; color:#47484a;">Powered by {APP}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";

    private const string ButtonStyle =
        "display:inline-block; padding:14px 34px; border-radius:10px; " +
        "background:#045C9D; background-image:linear-gradient(135deg,#1086D4 0%,#045C9D 60%,#013161 100%); " +
        "color:#ffffff; font-size:15px; font-weight:700; text-align:center;";

    /// <summary>
    /// Renders a branded email. <paramref name="bodyHtml"/> is inserted as-is (callers build safe HTML);
    /// a call-to-action button is shown only when both <paramref name="buttonUrl"/> and text are provided.
    /// </summary>
    public static string Render(
        string title,
        string bodyHtml,
        string? buttonUrl = null,
        string? buttonText = null,
        string company = "SMS-Hazard",
        string app = "SMS-Hazard")
    {
        var button = (!string.IsNullOrWhiteSpace(buttonUrl) && !string.IsNullOrWhiteSpace(buttonText))
            ? $"<a href=\"{buttonUrl}\" style=\"{ButtonStyle}\">{WebUtility.HtmlEncode(buttonText)}</a>"
            : "";

        return Shell
            .Replace("{TITLE}", WebUtility.HtmlEncode(title))
            .Replace("{COMPANY}", WebUtility.HtmlEncode(company))
            .Replace("{APP}", WebUtility.HtmlEncode(app))
            .Replace("{BODY}", bodyHtml)
            .Replace("{BUTTON}", button);
    }

    /// <summary>Convenience: wraps a plain-text message in a paragraph and escapes it.</summary>
    public static string Paragraph(string text) =>
        $"<p style=\"margin:0 0 14px 0;\">{WebUtility.HtmlEncode(text)}</p>";
}
