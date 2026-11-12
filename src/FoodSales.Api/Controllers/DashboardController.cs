using FoodSales.Application.Contracts;
using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/dashboard"), Authorize(Roles = "Admin")]
public sealed class DashboardController(OrderService service) : ControllerBase
{
    [HttpGet]
    public Task<DashboardView> Get(DateTime? from = null, DateTime? to = null, CancellationToken ct = default) => service.DashboardAsync(from, to, ct);
}
