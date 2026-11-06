using System.Security.Claims;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/account/profile"), Authorize]
public sealed class ProfileController(IProfileService service) : ControllerBase
{
    [HttpGet]
    public Task<ProfileView> Get(CancellationToken ct) => service.GetAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
    [HttpPut, EnableRateLimiting("auth")]
    public Task<ProfileView> Update(UpdateProfileRequest request, CancellationToken ct) => service.UpdateAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request, ct);
}
