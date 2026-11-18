using System.Net.Mail;
using System.Text;
using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace FoodSales.Infrastructure.Services;

public sealed class PasswordRecoveryService(UserManager<IdentityUser> users, IEmailSender emailSender,
    PasswordRecoveryOptions options, IUnitOfWork unit, ILogger<PasswordRecoveryService> logger) : IPasswordRecoveryService
{
    private const string InvalidLink = "Lidhja është e pavlefshme ose ka skaduar. Kërkoni një lidhje të re.";

    public async Task RequestAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!emailSender.IsAvailable)
            throw new AccountException("Rivendosja me email nuk është aktivizuar ende. Kontaktoni administratorin.");
        var user = await users.FindByEmailAsync(request.Email.Trim());
        // The public response is identical for registered and unknown addresses.
        if (user == null)
            return;
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        // A fragment keeps the token out of access logs and HTTP referrers.
        var link = $"{options.PublicAppUrl.TrimEnd('/')}/rivendos-fjalekalimin#email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
        var body = $"Përshëndetje,\n\nPër të vendosur fjalëkalimin e ri të llogarisë suaj në Tryeza, hapni lidhjen:\n\n{link}\n\nLidhja skadon pas {options.TokenLifetimeMinutes} minutash dhe përdoret vetëm një herë. Nëse nuk e keni kërkuar ju këtë ndryshim, shpërfilleni këtë mesazh.\n\nTryeza — Produkte Bio";
        try
        {
            await emailSender.SendAsync(user.Email!, "Rivendosni fjalëkalimin — Tryeza", body, ct);
        }
        catch (Exception ex) when (ex is SmtpException or IOException or UnauthorizedAccessException)
        {
            // Do not expose account existence or include addresses/tokens in delivery errors.
            logger.LogError("Password recovery email delivery failed. Check email transport configuration.");
        }
    }

    public Task ResetAsync(ResetPasswordRequest request, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user == null)
            throw new AccountException(InvalidLink);
        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            throw new AccountException(InvalidLink);
        }
        var result = await users.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "InvalidToken"))
                throw new AccountException(InvalidLink);
            throw new AccountException("Fjalëkalimi duhet të ketë të paktën 12 karaktere, shkronjë të madhe, të vogël, numër dhe simbol.");
        }
        // ResetPasswordAsync rotates the security stamp, invalidating the token and prior sessions.
        if (!(await users.ResetAccessFailedCountAsync(user)).Succeeded
            || !(await users.SetLockoutEndDateAsync(user, null)).Succeeded)
            throw new AccountException("Rivendosja dështoi. Provoni përsëri.");
        return true;
    }, ct);
}
