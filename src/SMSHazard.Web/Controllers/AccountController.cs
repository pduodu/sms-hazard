using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using SMSHazard.Application.Common;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure.Identity;
using SMSHazard.Web.Models;

namespace SMSHazard.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return RedirectToLocal(model.ReturnUrl);

        if (result.RequiresTwoFactor)
            return RedirectToAction(nameof(LoginWith2fa),
                new { returnUrl = model.ReturnUrl, rememberMe = model.RememberMe });

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
        else
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---- Profile ----
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var roles = await _userManager.GetRolesAsync(user);
        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            Roles = string.Join(", ", roles)
        });
    }

    // ---- Change password ----
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    // ---- Forgot password (AUTH-01) ----
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        // Only send for active accounts, but always show the same confirmation so we never
        // reveal whether an email is registered.
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = Url.Action(nameof(ResetPassword), "Account",
                new { userId = user.Id, code }, Request.Scheme)!;

            var body =
                EmailTemplate.Paragraph($"Hello {user.FullName},") +
                EmailTemplate.Paragraph("We received a request to reset your SMS-Hazard password. Use the button below to choose a new one. This link is valid for a limited time.") +
                EmailTemplate.Paragraph("If you didn't request this, you can safely ignore this email — your password won't change.");
            var html = EmailTemplate.Render(
                title: "Reset your password",
                bodyHtml: body,
                buttonUrl: link,
                buttonText: "Reset your password");

            // Offload SMTP to Hangfire so the request never blocks on the mail server.
            BackgroundJob.Enqueue<IEmailSender>(s =>
                s.SendAsync(user.Email!, "Reset your SMS-Hazard password", html, CancellationToken.None));
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ---- Reset password (from the emailed link) ----
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? userId = null, string? code = null)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            TempData["Error"] = "That password-reset link is invalid or incomplete.";
            return RedirectToAction(nameof(Login));
        }
        return View(new ResetPasswordViewModel { UserId = userId, Code = code });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            // Don't reveal that the user doesn't exist.
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "That reset link is invalid. Please request a new one.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    // ============================================================
    //  Two-factor authentication (AUTH-03) — TOTP authenticator app
    // ============================================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> TwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new TwoFactorStatusViewModel
        {
            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) is not null,
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user)
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EnableAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(await BuildEnableVm(user, new EnableAuthenticatorViewModel()));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
            return View(await BuildEnableVm(user, model));

        var code = model.Code.Replace(" ", "").Replace("-", "");
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            ModelState.AddModelError(nameof(model.Code), "That code is not valid. Try the current one from your app.");
            return View(await BuildEnableVm(user, model));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["RecoveryCodes"] = string.Join(",", recoveryCodes ?? Enumerable.Empty<string>());
        TempData["Success"] = "Two-factor authentication is on.";
        return RedirectToAction(nameof(ShowRecoveryCodes));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ShowRecoveryCodes()
    {
        if (TempData["RecoveryCodes"] is not string joined || joined.Length == 0)
            return RedirectToAction(nameof(TwoFactor));
        return View(joined.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2fa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _userManager.SetTwoFactorEnabledAsync(user, false);
        TempData["Success"] = "Two-factor authentication is off.";
        return RedirectToAction(nameof(TwoFactor));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        TempData["Info"] = "Your authenticator key was reset. Set it up again below.";
        return RedirectToAction(nameof(EnableAuthenticator));
    }

    // ---- Second-factor challenge at sign-in ----
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login));
        return View(new LoginWith2faViewModel { RememberMe = rememberMe, ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login));

        var code = model.TwoFactorCode.Replace(" ", "").Replace("-", "");
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code, model.RememberMe, model.RememberMachine);

        if (result.Succeeded)
            return RedirectToLocal(model.ReturnUrl);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login));
        return View(new LoginWithRecoveryCodeViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login));

        var code = model.RecoveryCode.Replace(" ", "");
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded)
            return RedirectToLocal(model.ReturnUrl);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid recovery code.");
        return View(model);
    }

    // ---- 2FA helpers ----
    private async Task<EnableAuthenticatorViewModel> BuildEnableVm(ApplicationUser user, EnableAuthenticatorViewModel model)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        model.SharedKey = FormatKey(key!);
        model.AuthenticatorUri = GenerateQrCodeUri(user.Email ?? user.UserName ?? "user", key!);
        return model;
    }

    private static string FormatKey(string unformattedKey)
    {
        var sb = new StringBuilder();
        var pos = 0;
        while (pos + 4 < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(pos, 4)).Append(' ');
            pos += 4;
        }
        if (pos < unformattedKey.Length)
            sb.Append(unformattedKey.AsSpan(pos));
        return sb.ToString().ToUpperInvariant();
    }

    private static string GenerateQrCodeUri(string email, string unformattedKey)
    {
        const string issuer = "SMS-Hazard";
        return
            $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
            $"?secret={unformattedKey}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");
}
