using FoodSales.Domain.Enums;

namespace FoodSales.Application.Contracts;

public sealed record CustomerReportView
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Phone { get; init; } = "";
    public string City { get; init; } = "";
    public int OrderCount { get; init; }
    public int CompletedOrderCount { get; init; }
    public int PendingOrderCount { get; init; }
    public decimal OrderedTotal { get; init; }
    public decimal SalesTotal { get; init; }
    public decimal PaidTotal { get; init; }
    public decimal Debt { get; init; }
}
public sealed record ReportOrderView(Guid Id, DateTime CreatedAt, DateTime? CompletedAt, OrderStatus Status,
    decimal Total, decimal Paid, decimal Debt, DateOnly? RequestedDeliveryDate);
public sealed record ReportPaymentView(Guid Id, Guid OrderId, decimal Amount, DateTime ReceivedAt, string Note);
