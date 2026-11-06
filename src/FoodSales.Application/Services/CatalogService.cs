using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Exceptions;

namespace FoodSales.Application.Services;

public sealed class CatalogService(IShopRepository store, IUnitOfWork unit, ILocalClock clock, PaginationOptions pagination)
{
    public Task<Page<ProductView>> ProductsAsync(int page, int? size, string? search, string? category, bool offersOnly, bool admin, string? userId, CancellationToken ct)
    {
        var p = pagination.Normalize(page, size);
        return store.ProductsAsync(p.Page, p.Size, search, category, offersOnly, admin, userId, clock.Now, ct);
    }

    public Task<Guid> SaveProductAsync(Guid? id, ProductRequest r, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        EnsureImage(r.ImageUrl);
        var product = id.HasValue ? await store.ProductAsync(id.Value, ct) ?? throw new NotFoundException() : new Product();
        if (id.HasValue) EnsureVersion(product.Version, r.Version);
        product.Update(r.Name, r.Description, r.ImageUrl, r.Category, r.Price, r.Stock, r.Unit, r.IsActive, r.OfferPrice, r.OfferStartsAt, r.OfferEndsAt);
        if (!id.HasValue) store.Add(product);
        return product.Id;
    }, ct);

    public Task<Page<BannerView>> BannersAsync(int page, int? size, bool admin, CancellationToken ct)
    {
        var p = pagination.Normalize(page, size);
        return store.BannersAsync(p.Page, p.Size, admin, clock.Now, ct);
    }

    public Task<Guid> SaveBannerAsync(Guid? id, BannerRequest r, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        EnsureImage(r.ImageUrl);
        if (r.EndsAt <= r.StartsAt) throw new BusinessException("Periudha e banerit nuk është e vlefshme.");
        var banner = id.HasValue ? await store.BannerAsync(id.Value, ct) ?? throw new NotFoundException() : new Banner();
        banner.Title = r.Title.Trim(); banner.Text = r.Text.Trim(); banner.ImageUrl = r.ImageUrl.Trim();
        banner.StartsAt = r.StartsAt; banner.EndsAt = r.EndsAt; banner.IsActive = r.IsActive;
        if (!id.HasValue) store.Add(banner);
        return banner.Id;
    }, ct);

    public static void EnsureVersion(byte[] actual, string? expected)
    {
        if (string.IsNullOrEmpty(expected) || Convert.ToBase64String(actual) != expected)
            throw new ConflictException("Të dhënat kanë ndryshuar. Rifreskojeni faqen dhe provoni përsëri.");
    }

    private static void EnsureImage(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (ImageFileNames.IsLocalUrl(value)) return;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new BusinessException("Ngarkoni një fotografi nga pajisja juaj.");
    }
}
