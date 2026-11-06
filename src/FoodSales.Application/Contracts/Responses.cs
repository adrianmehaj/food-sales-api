using FoodSales.Domain.Enums;

namespace FoodSales.Application.Contracts;

public sealed record Page<T>(IReadOnlyList<T> Items, int Number, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
public sealed record ProductView(Guid Id, string Name, string Description, string ImageUrl, string Category,
    decimal Price, decimal EffectivePrice, decimal Stock, Unit Unit, bool IsActive,
    decimal? OfferPrice, DateTime? OfferStartsAt, DateTime? OfferEndsAt, string Version);
public sealed record CustomerView(Guid Id, string Name, string Phone, string City, string Address,
    CustomerType Type, int PaymentTermDays, decimal Debt, string Version);
public sealed record CustomerPriceView(Guid ProductId, string ProductName, decimal Price);
public sealed record LineView(Guid Id, Guid ProductId, string ProductName, Unit Unit,
    decimal RequestedQuantity, decimal FinalQuantity, decimal UnitPrice, decimal Total);
public sealed record PaymentView(Guid Id, decimal Amount, DateTime ReceivedAt, string Note);
public sealed record EventView(OrderStatus Status, DateTime At);
public sealed record OrderView(Guid Id, Guid CustomerId, string CustomerName, string Phone, string City, string Address,
    string Notes, Fulfillment Fulfillment, OrderSource Source, OrderStatus Status, DateTime CreatedAt,
    DateTime? CompletedAt, DateTime? DueAt, decimal Total, decimal Paid, decimal Debt, PaymentStatus PaymentStatus,
    string Version, List<LineView> Lines, List<PaymentView> Payments, List<EventView> History, DateOnly? RequestedDeliveryDate);
public sealed record OrderReceipt(Guid Id, decimal Total, OrderStatus Status);
public sealed record BannerView(Guid Id, string Title, string Text, string ImageUrl, DateTime StartsAt, DateTime EndsAt, bool IsActive);
public sealed record ReportBucket(DateTime Date, decimal Sales, decimal Collections);
public sealed record DashboardView(decimal Sales, decimal Collections, decimal OutstandingDebt,
    int NewOrders, int PendingOrders, DateTime From, DateTime To, List<ReportBucket> Days);
public sealed record AccountView(string Id, string Email, bool IsAdmin);
public sealed record BusinessView(string Name, string City, string PickupAddress, string Phone, string? MapUrl = null);
