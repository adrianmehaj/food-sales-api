using System.Security.Claims;
using FoodSales.Application.Contracts;
using FoodSales.Application.Services;
using FoodSales.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/orders")]
public sealed class OrdersController(OrderService service) : ControllerBase
{
    [HttpPost, AllowAnonymous, EnableRateLimiting("checkout")]
    public async Task<ActionResult<OrderReceipt>> Create(CreateOrderRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateAsync(request, false, User.FindFirstValue(ClaimTypes.NameIdentifier), ct));

    [HttpGet("mine"), Authorize]
    public Task<Page<OrderView>> Mine(int page = 1, int? pageSize = null, CancellationToken ct = default) =>
        service.ListAsync(page, pageSize, User.FindFirstValue(ClaimTypes.NameIdentifier)!, null, null, ct);

    [HttpGet("mine/{id:guid}"), Authorize]
    public Task<OrderView> MineById(Guid id, CancellationToken ct) => service.GetAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
}
