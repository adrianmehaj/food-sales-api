using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Enums;
using FoodSales.Domain.Exceptions;

namespace FoodSales.Application.Services;

public sealed class CustomerService(IShopRepository store, IUnitOfWork unit, PaginationOptions pagination)
{
    public Task<Page<CustomerView>> ListAsync(int page, int? size, string? search, CancellationToken ct)
    {
        var p = pagination.Normalize(page, size);
        return store.CustomersAsync(p.Page, p.Size, search, ct);
    }

    public Task<Guid> SaveAsync(Guid? id, CustomerRequest r, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        if (!Enum.IsDefined(r.Type)) throw new BusinessException("Lloji i klientit nuk është i vlefshëm.");
        var c = id.HasValue ? await store.CustomerAsync(id.Value, ct) ?? throw new NotFoundException() : new Customer();
        if (id.HasValue) CatalogService.EnsureVersion(c.Version, r.Version);
        c.Name = r.Name.Trim(); c.Phone = r.Phone.Trim(); c.City = r.City.Trim(); c.Address = r.Address.Trim();
        c.Type = r.Type; c.PaymentTermDays = r.Type == CustomerType.Business ? r.PaymentTermDays : 0;
        if (!id.HasValue) store.Add(c);
        return c.Id;
    }, ct);

    public Task<List<CustomerPriceView>> PricesAsync(Guid id, CancellationToken ct) => store.CustomerPricesAsync(id, ct);

    public Task<bool> SetPriceAsync(Guid id, CustomerPriceRequest r, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        var c = await store.CustomerAsync(id, ct) ?? throw new NotFoundException();
        if (c.Type != CustomerType.Business) throw new BusinessException("Çmimet e veçanta vlejnë për bizneset.");
        _ = await store.ProductAsync(r.ProductId, ct) ?? throw new NotFoundException();
        if (decimal.Round(r.Price, 2) != r.Price) throw new BusinessException("Çmimi duhet të ketë deri dy shifra dhjetore.");
        await store.SetCustomerPriceAsync(id, r.ProductId, r.Price, ct);
        return true;
    }, ct);
}
