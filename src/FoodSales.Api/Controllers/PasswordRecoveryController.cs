using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/account"), AllowAnonymous]
public sealed class PasswordRecoveryController(IPasswordRecoveryService service, IAccountService accounts) : ControllerBase
{
    [HttpPost("forgot-password"), EnableRateLimiting("password-recovery")]
    public async Task<IActionResult> Forgot(ForgotPasswordRequest request, CancellationToken ct)
    {
        await service.RequestAsync(request, ct);
        return Accepted(new
        {
            message = "Nëse ky email ka llogari në Tryeza, do të merrni një lidhje për rivendosjen e fjalëkalimit."
        });
    }

    [HttpPost("reset-password"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetAsync(request, ct);
        await accounts.LogoutAsync();
        return NoContent();
    }
}
