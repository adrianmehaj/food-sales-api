using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/reports/customers"), Authorize(Roles = "Admin")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<CustomerReportView>> List(int page = 1, int? pageSize = null, string? search = null, bool debtOnly = false, CancellationToken ct = default) => service.CustomersAsync(page, pageSize, search, debtOnly, ct);
    [HttpGet("{id:guid}")]
    public Task<CustomerReportView> Customer(Guid id, CancellationToken ct) => service.CustomerAsync(id, ct);
    [HttpGet("{id:guid}/orders")]
    public Task<Page<ReportOrderView>> Orders(Guid id, int page = 1, int? pageSize = null, CancellationToken ct = default) => service.OrdersAsync(id, page, pageSize, ct);
    [HttpGet("{id:guid}/payments")]
    public Task<Page<ReportPaymentView>> Payments(Guid id, int page = 1, int? pageSize = null, CancellationToken ct = default) => service.PaymentsAsync(id, page, pageSize, ct);
}
