using System.Security.Claims;
using FoodSales.Application.Contracts;
using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/catalog")]
public sealed class CatalogController(CatalogService service, IConfiguration config) : ControllerBase
{
    [HttpGet("products"), AllowAnonymous]
    public Task<Page<ProductView>> Products(int page = 1, int? pageSize = null, string? search = null, string? category = null, bool offersOnly = false, CancellationToken ct = default) =>
        service.ProductsAsync(page, pageSize, search, category, offersOnly, false, User.FindFirstValue(ClaimTypes.NameIdentifier), ct);

    [HttpGet("banners"), AllowAnonymous]
    public Task<Page<BannerView>> Banners(int page = 1, int? pageSize = null, CancellationToken ct = default) => service.BannersAsync(page, pageSize, false, ct);

    [HttpGet("products/{id:guid}"), AllowAnonymous]
    public Task<ProductView> Product(Guid id, CancellationToken ct) => service.ProductAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), ct);

    [HttpGet("business"), AllowAnonymous]
    public BusinessView Business() => config.GetSection("Business").Get<BusinessView>()!;
}

[ApiController, Route("api/admin/products"), Authorize(Roles = "Admin")]
public sealed class ProductsController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<ProductView>> List(int page = 1, int? pageSize = null, string? search = null, CancellationToken ct = default) => service.ProductsAsync(page, pageSize, search, null, false, true, null, ct);
    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct) => StatusCode(201, new { id = await service.SaveProductAsync(null, request, ct) });
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct) { await service.SaveProductAsync(id, request, ct); return NoContent(); }
}

[ApiController, Route("api/admin/banners"), Authorize(Roles = "Admin")]
public sealed class BannersController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<BannerView>> List(int page = 1, int? pageSize = null, CancellationToken ct = default) => service.BannersAsync(page, pageSize, true, ct);
    [HttpPost]
    public async Task<IActionResult> Create(BannerRequest request, CancellationToken ct) => StatusCode(201, new { id = await service.SaveBannerAsync(null, request, ct) });
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BannerRequest request, CancellationToken ct) { await service.SaveBannerAsync(id, request, ct); return NoContent(); }
}
