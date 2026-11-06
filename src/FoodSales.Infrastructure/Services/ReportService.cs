using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Enums;
using FoodSales.Domain.Exceptions;
using FoodSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodSales.Infrastructure.Services;

public sealed class ReportService(ShopDbContext db, PaginationOptions pagination) : IReportService
{
    private IQueryable<CustomerReportView> Customers() => db.Customers.AsNoTracking().Select(c => new CustomerReportView
    {
        Id = c.Id, Name = c.Name, Phone = c.Phone, City = c.City,
        OrderCount = db.Orders.Count(o => o.CustomerId == c.Id),
        CompletedOrderCount = db.Orders.Count(o => o.CustomerId == c.Id && o.Status == OrderStatus.Completed),
        PendingOrderCount = db.Orders.Count(o => o.CustomerId == c.Id && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
        OrderedTotal = db.Orders.Where(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled).Sum(o => (decimal?)o.Lines.Sum(l => l.Total)) ?? 0,
        SalesTotal = db.Orders.Where(o => o.CustomerId == c.Id && o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.Lines.Sum(l => l.Total)) ?? 0,
        PaidTotal = db.Payments.Where(p => db.Orders.Any(o => o.Id == p.OrderId && o.CustomerId == c.Id)).Sum(p => (decimal?)p.Amount) ?? 0,
        Debt = db.Orders.Where(o => o.CustomerId == c.Id && o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.Lines.Sum(l => l.Total) - (decimal?)o.Payments.Sum(p => p.Amount)) ?? 0
    });

    public async Task<Page<CustomerReportView>> CustomersAsync(int page, int? size, string? search, bool debtOnly, CancellationToken ct)
    {
        var p = pagination.Normalize(page, size); var query = Customers();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Name.StartsWith(search) || c.Phone.StartsWith(search));
        if (debtOnly) query = query.Where(c => c.Debt > 0);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.Debt).ThenBy(c => c.Name).ThenBy(c => c.Id).Skip((p.Page - 1) * p.Size).Take(p.Size).ToListAsync(ct);
        return new(items, p.Page, p.Size, count);
    }
    public async Task<CustomerReportView> CustomerAsync(Guid id, CancellationToken ct) => await Customers().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException();
    public async Task<Page<ReportOrderView>> OrdersAsync(Guid customerId, int page, int? size, CancellationToken ct)
    {
        await RequireCustomerAsync(customerId, ct);
        var p = pagination.Normalize(page, size); var query = db.Orders.AsNoTracking().Where(x => x.CustomerId == customerId);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((p.Page - 1) * p.Size).Take(p.Size)
            .Select(x => new ReportOrderView(x.Id, x.CreatedAt, x.CompletedAt, x.Status, x.Lines.Sum(l => l.Total), x.Payments.Sum(p => p.Amount),
                x.Status == OrderStatus.Completed ? x.Lines.Sum(l => l.Total) - x.Payments.Sum(p => p.Amount) : 0, x.RequestedDeliveryDate)).ToListAsync(ct);
        return new(items, p.Page, p.Size, count);
    }
    public async Task<Page<ReportPaymentView>> PaymentsAsync(Guid customerId, int page, int? size, CancellationToken ct)
    {
        await RequireCustomerAsync(customerId, ct);
        var p = pagination.Normalize(page, size);
        var query = db.Payments.AsNoTracking().Where(x => db.Orders.Any(o => o.Id == x.OrderId && o.CustomerId == customerId));
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.ReceivedAt).ThenBy(x => x.Id).Skip((p.Page - 1) * p.Size).Take(p.Size)
            .Select(x => new ReportPaymentView(x.Id, x.OrderId, x.Amount, x.ReceivedAt, x.Note)).ToListAsync(ct);
        return new(items, p.Page, p.Size, count);
    }
    private async Task RequireCustomerAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == id, ct)) throw new NotFoundException();
    }
}
