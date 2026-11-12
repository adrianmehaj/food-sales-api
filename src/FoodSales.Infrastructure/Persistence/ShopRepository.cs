using System.Linq.Expressions;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodSales.Infrastructure.Persistence;

public sealed class ShopRepository(ShopDbContext db) : IShopRepository
{
    public async Task<Page<ProductView>> ProductsAsync(int page, int size, string? search, string? category, bool offersOnly, bool admin, string? userId, DateTime now, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Where(x => admin || x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.StartsWith(search));
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);
        if (offersOnly)
            query = query.Where(x => x.OfferPrice != null && x.OfferStartsAt <= now && x.OfferEndsAt >= now);
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new ProductView(x.Id, x.Name, x.Description, x.ImageUrl, x.Category, x.Price,
                x.OfferPrice != null && x.OfferStartsAt <= now && x.OfferEndsAt >= now ? x.OfferPrice.Value : x.Price,
                x.Stock, x.Unit, x.IsActive, x.OfferPrice, x.OfferStartsAt, x.OfferEndsAt, Convert.ToBase64String(x.Version))).ToListAsync(ct);
        if (userId != null)
        {
            var ids = items.Select(x => x.Id).ToArray();
            var prices = await db.CustomerPrices.AsNoTracking()
                .Where(x => ids.Contains(x.ProductId) && db.Customers.Any(c => c.Id == x.CustomerId && c.UserId == userId && c.Type == CustomerType.Business))
                .ToDictionaryAsync(x => x.ProductId, x => x.Price, ct);
            items = items.Select(x => prices.TryGetValue(x.Id, out var price) ? x with { EffectivePrice = price } : x).ToList();
        }
        return new(items, page, size, count);
    }
    public Task<Product?> ProductAsync(Guid id, CancellationToken ct) => db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Dictionary<Guid, Product>> ProductsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        db.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
    public void Add(Product product) => db.Products.Add(product);
    public Task<Customer?> CustomerAsync(Guid id, CancellationToken ct) => db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Customer?> CustomerForUserAsync(string userId, CancellationToken ct) => db.Customers.SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public void Add(Customer customer) => db.Customers.Add(customer);
    public async Task<Page<CustomerView>> CustomersAsync(int page, int size, string? search, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.StartsWith(search) || x.Phone.StartsWith(search));
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new CustomerView(x.Id, x.Name, x.Phone, x.City, x.Address, x.Type, x.PaymentTermDays,
                db.Orders.Where(o => o.CustomerId == x.Id && o.Status == OrderStatus.Completed)
                    .Sum(o => (decimal?)o.Lines.Sum(l => l.Total) - (decimal?)(o.Payments.Sum(p => p.Amount))) ?? 0,
                Convert.ToBase64String(x.Version))).ToListAsync(ct);
        return new(items, page, size, count);
    }
    public Task<decimal?> SpecialPriceAsync(Guid customerId, Guid productId, CancellationToken ct) => db.CustomerPrices
        .Where(x => x.CustomerId == customerId && x.ProductId == productId).Select(x => (decimal?)x.Price).SingleOrDefaultAsync(ct);
    public Task<Dictionary<Guid, decimal>> SpecialPricesAsync(Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken ct) =>
        db.CustomerPrices.AsNoTracking().Where(x => x.CustomerId == customerId && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, x => x.Price, ct);
    public Task<List<CustomerPriceView>> CustomerPricesAsync(Guid customerId, CancellationToken ct) => db.CustomerPrices.AsNoTracking()
        .Where(x => x.CustomerId == customerId).OrderBy(x => x.ProductId).Take(100)
        .Select(x => new CustomerPriceView(x.ProductId, db.Products.Where(p => p.Id == x.ProductId).Select(p => p.Name).First(), x.Price)).ToListAsync(ct);
    public async Task SetCustomerPriceAsync(Guid customerId, Guid productId, decimal price, CancellationToken ct)
    {
        var existing = await db.CustomerPrices.SingleOrDefaultAsync(x => x.CustomerId == customerId && x.ProductId == productId, ct);
        if (existing == null)
            db.CustomerPrices.Add(new CustomerPrice { CustomerId = customerId, ProductId = productId, Price = price });
        else
            existing.Price = price;
    }
    public Task<Order?> OrderAsync(Guid id, CancellationToken ct) => Aggregate().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Order?> OrderByRequestAsync(Guid requestId, CancellationToken ct) => Aggregate().SingleOrDefaultAsync(x => x.RequestId == requestId, ct);
    private IQueryable<Order> Aggregate() => db.Orders.Include(x => x.Customer).Include(x => x.Lines).Include(x => x.Payments).Include(x => x.History).AsSplitQuery();
    public Task<bool> PaymentRequestExistsAsync(Guid requestId, Guid exceptOrderId, CancellationToken ct) =>
        db.Payments.AnyAsync(x => x.RequestId == requestId && x.OrderId != exceptOrderId, ct);
    public void Add(Order order) => db.Orders.Add(order);

    private static readonly Expression<Func<Order, OrderView>> OrderProjection = x => new OrderView(
        x.Id, x.CustomerId, x.CustomerName, x.Phone, x.City, x.Address, x.Notes, x.Fulfillment, x.Source, x.Status,
        x.CreatedAt, x.CompletedAt, x.DueAt, x.Lines.Sum(l => l.Total), x.Payments.Sum(p => p.Amount),
        x.Status == OrderStatus.Completed ? x.Lines.Sum(l => l.Total) - x.Payments.Sum(p => p.Amount) : 0,
        x.Payments.Sum(p => p.Amount) == 0 ? PaymentStatus.Unpaid : x.Payments.Sum(p => p.Amount) < x.Lines.Sum(l => l.Total) ? PaymentStatus.PartiallyPaid : PaymentStatus.Paid,
        Convert.ToBase64String(x.Version),
        x.Lines.OrderBy(l => l.ProductName).Select(l => new LineView(l.Id, l.ProductId, l.ProductName, l.Unit, l.RequestedQuantity, l.FinalQuantity, l.UnitPrice, l.Total)).ToList(),
        x.Payments.OrderByDescending(p => p.ReceivedAt).Select(p => new PaymentView(p.Id, p.Amount, p.ReceivedAt, p.Note)).ToList(),
        x.History.OrderBy(h => h.At).Select(h => new EventView(h.Status, h.At)).ToList(), x.RequestedDeliveryDate);

    public async Task<Page<OrderView>> OrdersAsync(int page, int size, string? userId, OrderStatus? status, Guid? customerId, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().AsSplitQuery();
        if (userId != null)
            query = query.Where(x => x.Customer.UserId == userId);
        if (status.HasValue)
            query = query.Where(x => x.Status == status);
        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size).Select(OrderProjection).ToListAsync(ct);
        return new(items, page, size, count);
    }
    public Task<OrderView?> OrderViewAsync(Guid id, string? userId, CancellationToken ct) => db.Orders.AsNoTracking().AsSplitQuery()
        .Where(x => x.Id == id && (userId == null || x.Customer.UserId == userId)).Select(OrderProjection).SingleOrDefaultAsync(ct);

    public async Task<DashboardView> DashboardAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var sales = await db.Orders.AsNoTracking().Where(x => x.Status == OrderStatus.Completed && x.CompletedAt >= from && x.CompletedAt < to)
            .GroupBy(x => x.CompletedAt!.Value.Date).Select(g => new { Date = g.Key, Total = g.Sum(x => x.Lines.Sum(l => l.Total)) }).ToListAsync(ct);
        var collections = await db.Payments.AsNoTracking().Where(x => x.ReceivedAt >= from && x.ReceivedAt < to)
            .GroupBy(x => x.ReceivedAt.Date).Select(g => new { Date = g.Key, Total = g.Sum(x => x.Amount) }).ToListAsync(ct);
        var debt = await db.Orders.AsNoTracking().Where(x => x.Status == OrderStatus.Completed)
            .SumAsync(x => (decimal?)x.Lines.Sum(l => l.Total) - (decimal?)x.Payments.Sum(p => p.Amount), ct) ?? 0;
        var newOrders = await db.Orders.CountAsync(x => x.Status == OrderStatus.New, ct);
        var pending = await db.Orders.CountAsync(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled, ct);
        var days = Enumerable.Range(0, (to - from).Days).Select(i => from.AddDays(i))
            .Select(d => new ReportBucket(d, sales.FirstOrDefault(x => x.Date == d)?.Total ?? 0, collections.FirstOrDefault(x => x.Date == d)?.Total ?? 0)).ToList();
        return new(sales.Sum(x => x.Total), collections.Sum(x => x.Total), debt, newOrders, pending, from, to, days);
    }
    public async Task<Page<BannerView>> BannersAsync(int page, int size, bool admin, DateTime now, CancellationToken ct)
    {
        var query = db.Banners.AsNoTracking().Where(x => admin || x.IsActive && x.StartsAt <= now && x.EndsAt >= now);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.StartsAt).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new BannerView(x.Id, x.Title, x.Text, x.ImageUrl, x.StartsAt, x.EndsAt, x.IsActive, x.ImageUrls)).ToListAsync(ct);
        return new(items, page, size, count);
    }
    public Task<Banner?> BannerAsync(Guid id, CancellationToken ct) => db.Banners.SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add(Banner banner) => db.Banners.Add(banner);
}
