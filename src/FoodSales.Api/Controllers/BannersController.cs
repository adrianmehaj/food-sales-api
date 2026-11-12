using FoodSales.Application.Contracts;
using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/banners"), Authorize(Roles = "Admin")]
public sealed class BannersController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<BannerView>> List(int page = 1, int? pageSize = null, CancellationToken ct = default) => service.BannersAsync(page, pageSize, true, ct);
    [HttpPost]
    public async Task<IActionResult> Create(BannerRequest request, CancellationToken ct) => StatusCode(201, new { id = await service.SaveBannerAsync(null, request, ct) });
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BannerRequest request, CancellationToken ct)
    {
        await service.SaveBannerAsync(id, request, ct);
        return NoContent();
    }
}
