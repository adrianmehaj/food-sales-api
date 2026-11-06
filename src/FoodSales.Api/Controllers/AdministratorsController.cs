using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/administrators"), Authorize(Roles = "Admin")]
public sealed class AdministratorsController(IAdministratorService service) : ControllerBase
{
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public Task<Page<AdministratorView>> List(int page = 1, int? pageSize = null,
        [MaxLength(254)] string? search = null, CancellationToken ct = default) => service.ListAsync(page, pageSize, search, ActorId, ct);

    [HttpPost]
    public async Task<ActionResult<AdministratorView>> Create(CreateAdministratorRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateAsync(request, ActorId, ct));

    [HttpPost("grant")]
    public Task<AdministratorView> Grant(GrantAdministratorRequest request, CancellationToken ct) => service.GrantAsync(request, ActorId, ct);
}
