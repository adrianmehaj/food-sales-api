using FoodSales.Application.Contracts;
using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/products"), Authorize(Roles = "Admin")]
public sealed class ProductsController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<ProductView>> List(int page = 1, int? pageSize = null, string? search = null, CancellationToken ct = default) => service.ProductsAsync(page, pageSize, search, null, false, true, null, ct);
    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct) => StatusCode(201, new { id = await service.SaveProductAsync(null, request, ct) });
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct)
    {
        await service.SaveProductAsync(id, request, ct);
        return NoContent();
    }
}
