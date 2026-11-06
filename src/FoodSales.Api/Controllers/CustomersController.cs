using FoodSales.Application.Contracts;
using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/customers"), Authorize(Roles = "Admin")]
public sealed class CustomersController(CustomerService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<CustomerView>> List(int page = 1, int? pageSize = null, string? search = null, CancellationToken ct = default) => service.ListAsync(page, pageSize, search, ct);
    [HttpPost]
    public async Task<IActionResult> Create(CustomerRequest request, CancellationToken ct) => StatusCode(201, new { id = await service.SaveAsync(null, request, ct) });
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CustomerRequest request, CancellationToken ct) { await service.SaveAsync(id, request, ct); return NoContent(); }
    [HttpGet("{id:guid}/prices")]
    public Task<List<CustomerPriceView>> Prices(Guid id, CancellationToken ct) => service.PricesAsync(id, ct);
    [HttpPut("{id:guid}/prices")]
    public async Task<IActionResult> SetPrice(Guid id, CustomerPriceRequest request, CancellationToken ct) { await service.SetPriceAsync(id, request, ct); return NoContent(); }
}
