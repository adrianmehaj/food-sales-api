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

[ApiController, Route("api/admin/orders"), Authorize(Roles = "Admin")]
public sealed class AdminOrdersController(OrderService service) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public Task<Page<OrderView>> List(int page = 1, int? pageSize = null, OrderStatus? status = null, Guid? customerId = null, CancellationToken ct = default) =>
        service.ListAsync(page, pageSize, null, status, customerId, ct);
    [HttpGet("{id:guid}")]
    public Task<OrderView> Get(Guid id, CancellationToken ct) => service.GetAsync(id, null, ct);
    [HttpPost]
    public async Task<ActionResult<OrderReceipt>> Create(CreateOrderRequest request, CancellationToken ct) => StatusCode(201, await service.CreateAsync(request, true, Actor, ct));
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, ChangeStatusRequest request, CancellationToken ct) { await service.ChangeStatusAsync(id, request, Actor, ct); return NoContent(); }
    [HttpPut("{id:guid}/quantity")]
    public async Task<IActionResult> Quantity(Guid id, FinalQuantityRequest request, CancellationToken ct) { await service.SetQuantityAsync(id, request, ct); return NoContent(); }
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteOrderRequest request, CancellationToken ct) { await service.CompleteAsync(id, request, Actor, ct); return NoContent(); }
    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> Payment(Guid id, PaymentRequest request, CancellationToken ct) { await service.AddPaymentAsync(id, request, Actor, ct); return NoContent(); }
}

[ApiController, Route("api/admin/dashboard"), Authorize(Roles = "Admin")]
public sealed class DashboardController(OrderService service) : ControllerBase
{
    [HttpGet]
    public Task<DashboardView> Get(DateTime? from = null, DateTime? to = null, CancellationToken ct = default) => service.DashboardAsync(from, to, ct);
}
