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
