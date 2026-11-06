using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Enums;
using FoodSales.Domain.Exceptions;

namespace FoodSales.Application.Services;

public sealed class OrderService(IShopRepository store, IUnitOfWork unit, ILocalClock clock, PaginationOptions pagination)
{
    public Task<OrderReceipt> CreateAsync(CreateOrderRequest r, bool manual, string? actor, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        ValidateCreate(r, manual);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Request = r, Actor = actor, Manual = manual }))));
        var previous = await store.OrderByRequestAsync(r.RequestId, ct);
        if (previous != null)
        {
            if (previous.RequestHash != hash) throw new ConflictException("Identifikuesi është përdorur për një porosi tjetër.");
            return new OrderReceipt(previous.Id, previous.Total, previous.Status);
        }
        var customer = await ResolveCustomerAsync(r, manual, actor, ct);
        if (!manual && r.Fulfillment == Fulfillment.Delivery && r.RequestedDeliveryDate == null)
            throw new BusinessException("Zgjidhni datën e dëshiruar të dërgesës.");
        var order = Order.Create(customer, r.RequestId, hash, manual ? OrderSource.Manual : OrderSource.Online,
            r.Fulfillment, r.Name.Trim(), r.Phone.Trim(), r.City.Trim(), r.Address.Trim(), r.Notes.Trim(), actor, clock.Now, r.RequestedDeliveryDate);
        foreach (var item in r.Items)
        {
            var product = await store.ProductAsync(item.ProductId, ct) ?? throw new NotFoundException();
            var price = customer.Type == CustomerType.Business ? await store.SpecialPriceAsync(customer.Id, product.Id, ct) : null;
            order.AddLine(product, item.Quantity, price ?? product.PriceAt(clock.Now));
        }
        store.Add(order);
        if (manual && r.CompleteImmediately)
        {
            await CompleteCoreAsync(order, actor!, r.InitialPayment, r.ApproveCredit, r.RequestId, ct);
        }
        return new OrderReceipt(order.Id, order.Total, order.Status);
    }, ct);

    public Task<Page<OrderView>> ListAsync(int page, int? size, string? userId, OrderStatus? status, Guid? customerId, CancellationToken ct)
    {
        var p = pagination.Normalize(page, size);
        return store.OrdersAsync(p.Page, p.Size, userId, status, customerId, ct);
    }

    public async Task<OrderView> GetAsync(Guid id, string? userId, CancellationToken ct) =>
        await store.OrderViewAsync(id, userId, ct) ?? throw new NotFoundException();

    public Task<bool> ChangeStatusAsync(Guid id, ChangeStatusRequest r, string actor, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        var order = await RequiredAsync(id, r.Version, ct);
        order.MoveTo(r.Status, actor, clock.Now);
        return true;
    }, ct);

    public Task<bool> SetQuantityAsync(Guid id, FinalQuantityRequest r, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        var order = await RequiredAsync(id, r.Version, ct);
        order.SetFinalQuantity(r.LineId, r.Quantity);
        return true;
    }, ct);

    public Task<bool> CompleteAsync(Guid id, CompleteOrderRequest r, string actor, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        var order = await RequiredAsync(id, r.Version, ct);
        await CompleteCoreAsync(order, actor, r.InitialPayment, r.ApproveCredit, r.PaymentRequestId, ct);
        return true;
    }, ct);

    public Task<bool> AddPaymentAsync(Guid id, PaymentRequest r, string actor, CancellationToken ct) => unit.ExecuteAsync(async () =>
    {
        var order = await store.OrderAsync(id, ct) ?? throw new NotFoundException();
        if (await store.PaymentRequestExistsAsync(r.RequestId, id, ct)) throw new ConflictException("Identifikuesi i pagesës është përdorur.");
        order.AddPayment(r.Amount, r.RequestId, actor, clock.Now, r.Note.Trim());
        return true;
    }, ct);

    public Task<DashboardView> DashboardAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var start = (from ?? clock.Now.Date.AddDays(-6)).Date;
        var end = (to ?? clock.Now.Date.AddDays(1)).Date;
        if (end <= start || (end - start).TotalDays > 366) throw new BusinessException("Periudha duhet të jetë nga 1 deri në 366 ditë.");
        return store.DashboardAsync(start, end, ct);
    }

    private async Task<Order> RequiredAsync(Guid id, string version, CancellationToken ct)
    {
        var order = await store.OrderAsync(id, ct) ?? throw new NotFoundException();
        CatalogService.EnsureVersion(order.Version, version);
        return order;
    }

    private async Task CompleteCoreAsync(Order order, string actor, decimal payment, bool approveCredit, Guid paymentId, CancellationToken ct)
    {
        if (payment > 0 && await store.PaymentRequestExistsAsync(paymentId, order.Id, ct))
            throw new ConflictException("Identifikuesi i pagesës është përdorur.");
        order.Complete(actor, clock.Now, approveCredit, order.Customer.PaymentTermDays, payment, paymentId);
        foreach (var line in order.Lines)
        {
            var product = await store.ProductAsync(line.ProductId, ct) ?? throw new NotFoundException();
            product.DeductStock(line.FinalQuantity);
        }
    }

    private async Task<Customer> ResolveCustomerAsync(CreateOrderRequest r, bool manual, string? actor, CancellationToken ct)
    {
        if (manual && r.CustomerId.HasValue) return await store.CustomerAsync(r.CustomerId.Value, ct) ?? throw new NotFoundException();
        if (!manual && actor != null)
        {
            var owner = await store.CustomerForUserAsync(actor, ct);
            if (owner != null) return owner;
        }
        // A matching phone number does not prove account ownership. Guest records are never linked automatically.
        var customer = new Customer { UserId = manual ? null : actor, Name = r.Name.Trim(), Phone = r.Phone.Trim(), City = r.City.Trim(), Address = r.Address.Trim() };
        store.Add(customer);
        return customer;
    }

    private static void ValidateCreate(CreateOrderRequest r, bool manual)
    {
        if (r.RequestId == Guid.Empty || r.Items.Count is < 1 or > 50 || r.Items.Select(x => x.ProductId).Distinct().Count() != r.Items.Count)
            throw new BusinessException("Porosia duhet të ketë 1–50 produkte pa përsëritje.");
        if (r.Fulfillment == Fulfillment.Delivery && string.IsNullOrWhiteSpace(r.Address)) throw new BusinessException("Shkruani adresën e dërgesës.");
        if (!manual && (r.CustomerId.HasValue || r.CompleteImmediately || r.ApproveCredit || r.InitialPayment != 0))
            throw new BusinessException("Klienti mund të krijojë vetëm porosi të re.");
        if (manual && !r.CompleteImmediately && (r.InitialPayment != 0 || r.ApproveCredit))
            throw new BusinessException("Pagesa regjistrohet kur shitja përfundon.");
    }
}
