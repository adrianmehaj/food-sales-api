using FoodSales.Application.Contracts;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Enums;

namespace FoodSales.Application.Interfaces;

public interface IShopRepository
{
    Task<Page<ProductView>> ProductsAsync(int page, int size, string? search, string? category, bool offersOnly, bool admin, string? userId, DateTime now, CancellationToken ct);
    Task<Product?> ProductAsync(Guid id, CancellationToken ct);
    Task<Dictionary<Guid, Product>> ProductsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    void Add(Product product);
    Task<Customer?> CustomerAsync(Guid id, CancellationToken ct);
    Task<Customer?> CustomerForUserAsync(string userId, CancellationToken ct);
    void Add(Customer customer);
    Task<Page<CustomerView>> CustomersAsync(int page, int size, string? search, CancellationToken ct);
    Task<decimal?> SpecialPriceAsync(Guid customerId, Guid productId, CancellationToken ct);
    Task<Dictionary<Guid, decimal>> SpecialPricesAsync(Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken ct);
    Task<List<CustomerPriceView>> CustomerPricesAsync(Guid customerId, CancellationToken ct);
    Task SetCustomerPriceAsync(Guid customerId, Guid productId, decimal price, CancellationToken ct);
    Task<Order?> OrderAsync(Guid id, CancellationToken ct);
    Task<Order?> OrderByRequestAsync(Guid requestId, CancellationToken ct);
    Task<bool> PaymentRequestExistsAsync(Guid requestId, Guid exceptOrderId, CancellationToken ct);
    void Add(Order order);
    Task<Page<OrderView>> OrdersAsync(int page, int size, string? userId, OrderStatus? status, Guid? customerId, CancellationToken ct);
    Task<OrderView?> OrderViewAsync(Guid id, string? userId, CancellationToken ct);
    Task<DashboardView> DashboardAsync(DateTime from, DateTime to, CancellationToken ct);
    Task<Page<BannerView>> BannersAsync(int page, int size, bool admin, DateTime now, CancellationToken ct);
    Task<Banner?> BannerAsync(Guid id, CancellationToken ct);
    void Add(Banner banner);
}
