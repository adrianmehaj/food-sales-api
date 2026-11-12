using System.Security.Claims;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/account")]
public sealed class AccountController(IAccountService service) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken
        });
    }
    [HttpGet("me"), AllowAnonymous]
    public async Task<ActionResult<AccountView?>> Me(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(id == null ? null : await service.CurrentAsync(id, ct));
    }
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public Task<AccountView> Login(LoginRequest request, CancellationToken ct) => service.LoginAsync(request, ct);
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("auth")]
    public Task<AccountView> Register(RegisterRequest request, CancellationToken ct) => service.RegisterAsync(request, ct);
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await service.LogoutAsync();
        return NoContent();
    }
}
