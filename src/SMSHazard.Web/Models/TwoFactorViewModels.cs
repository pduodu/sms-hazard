using System.ComponentModel.DataAnnotations;

namespace SMSHazard.Web.Models;

public class TwoFactorStatusViewModel
{
    public bool Is2faEnabled { get; set; }
    public bool HasAuthenticator { get; set; }
    public int RecoveryCodesLeft { get; set; }
}

public class EnableAuthenticatorViewModel
{
    /// <summary>Base32 secret formatted in groups of four for manual entry.</summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>otpauth:// URI encoded into the QR code.</summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    [Required, StringLength(8, MinimumLength = 6, ErrorMessage = "The code is 6 digits.")]
    [DataType(DataType.Text), Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;
}

public class LoginWith2faViewModel
{
    [Required, StringLength(8, MinimumLength = 6, ErrorMessage = "The code is 6 digits.")]
    [DataType(DataType.Text), Display(Name = "Authenticator code")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "Remember this device")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class LoginWithRecoveryCodeViewModel
{
    [Required, DataType(DataType.Text), Display(Name = "Recovery code")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
