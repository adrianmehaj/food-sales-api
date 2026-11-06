using FoodSales.Application.Contracts;

namespace FoodSales.Application.Interfaces;

public interface IReportService
{
    Task<Page<CustomerReportView>> CustomersAsync(int page, int? size, string? search, bool debtOnly, CancellationToken ct);
    Task<CustomerReportView> CustomerAsync(Guid id, CancellationToken ct);
    Task<Page<ReportOrderView>> OrdersAsync(Guid customerId, int page, int? size, CancellationToken ct);
    Task<Page<ReportPaymentView>> PaymentsAsync(Guid customerId, int page, int? size, CancellationToken ct);
}
