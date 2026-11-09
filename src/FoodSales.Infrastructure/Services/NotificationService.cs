using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Exceptions;
using FoodSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using FoodSales.Domain.Enums;
using System.Globalization;

namespace FoodSales.Infrastructure.Services;

public sealed class NotificationService(ShopDbContext db, ILocalClock clock, PaginationOptions pagination, IUnitOfWork unit) : INotificationService
{
    public void OrderCreated(Order order)
    {
        var message = OrderMessage(order.Id, order.CustomerName, order.Fulfillment, order.RequestedDeliveryDate, order.Total,
            order.Lines.Select(x => new NoticeLine(x.ProductName, x.RequestedQuantity, x.Unit)));
        Add(order, null, true, "Porosi e re", message);
        if (order.Customer.UserId != null) Add(order, order.Customer.UserId, false, "Porosia u regjistrua", $"Porosia #{order.Id.ToString()[..8].ToUpperInvariant()} u pranua. Do t'ju njoftojmë pas konfirmimit.");
    }
    public void OrderConfirmed(Order order)
    {
        if (order.Customer.UserId == null) return;
        var date = order.RequestedDeliveryDate.HasValue ? $" dhe do të jetë gati më {order.RequestedDeliveryDate.Value:dd.MM.yyyy}" : "";
        Add(order, order.Customer.UserId, false, "Porosia juaj u konfirmua", $"Porosia #{order.Id.ToString()[..8].ToUpperInvariant()} u konfirmua{date}.");
    }
    private void Add(Order order, string? userId, bool admins, string title, string message) => db.Notifications.Add(new PlatformNotification
    {
        OrderId = order.Id, RecipientUserId = userId, ForAdministrators = admins, Title = title,
        Message = message.Length <= 1000 ? message : message[..997] + "…", CreatedAt = clock.Now
    });
    private IQueryable<PlatformNotification> Visible(string userId, bool admin) => db.Notifications.AsNoTracking()
        .Where(x => x.RecipientUserId == userId || admin && x.ForAdministrators);
    public async Task<NotificationInbox> InboxAsync(string userId, bool administrator, int page, int? size, CancellationToken ct)
    {
        try
        {
            var p = pagination.Normalize(page, size); var query = Visible(userId, administrator);
            var total = await query.CountAsync(ct);
            var unread = await query.CountAsync(x => !db.NotificationReads.Any(r => r.NotificationId == x.Id && r.UserId == userId), ct);
            var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((p.Page - 1) * p.Size).Take(p.Size)
                .Select(x => new { x.ForAdministrators, View = new NotificationView(x.Id, x.OrderId, x.Title, x.Message, x.CreatedAt,
                    db.NotificationReads.Any(r => r.NotificationId == x.Id && r.UserId == userId)) }).ToListAsync(ct);
            var items = rows.Select(x => x.View).ToList();
            // Enrich only this authorized page. Existing notifications gain units
            // without rewriting their records, timestamps or per-user read state.
            var orderIds = rows.Where(x => x.ForAdministrators && administrator).Select(x => x.View.OrderId).Distinct().ToArray();
            if (orderIds.Length > 0)
            {
                var orders = await db.Orders.AsNoTracking().Where(x => orderIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.CustomerName, x.Fulfillment, x.RequestedDeliveryDate,
                        Total = x.Lines.Sum(l => l.Total),
                        Lines = x.Lines.OrderBy(l => l.ProductName).ThenBy(l => l.Id)
                            .Select(l => new NoticeLine(l.ProductName, l.RequestedQuantity, l.Unit)).ToList() }).ToListAsync(ct);
                var messages = orders.ToDictionary(x => x.Id,
                    x => OrderMessage(x.Id, x.CustomerName, x.Fulfillment, x.RequestedDeliveryDate, x.Total, x.Lines));
                items = rows.Select(x => x.ForAdministrators && administrator && messages.TryGetValue(x.View.OrderId, out var message)
                    ? x.View with { Message = message } : x.View).ToList();
            }
            return new(new(items, p.Page, p.Size, total), unread);
        }
        // SqlClient can report an acknowledged cancellation as SQL error 0
        // instead of OperationCanceledException, especially while waiting for a lock.
        catch (SqlException exception) when (ct.IsCancellationRequested && exception.Number == 0)
        {
            throw new OperationCanceledException("Notification request cancelled.", exception, ct);
        }
    }
    private sealed record NoticeLine(string Name, decimal Quantity, Unit Unit);

    private static string OrderMessage(Guid id, string customer, Fulfillment fulfillment, DateOnly? deliveryDate, decimal total, IEnumerable<NoticeLine> lines)
    {
        var items = lines.Select(x =>
        {
            var unit = x.Unit switch { Unit.Kilogram => "kg", Unit.Liter => x.Quantity == 1 ? "litër" : "litra", _ => "copë" };
            return $"• {x.Name}: {x.Quantity.ToString("0.###", CultureInfo.InvariantCulture)} {unit}";
        });
        var message = $"Klienti: {customer}\nPorosia: #{id.ToString()[..8].ToUpperInvariant()}\n" + string.Join("\n", items)
            + $"\n\nTotali i porosisë: {total.ToString("0.00", CultureInfo.InvariantCulture)} €"
            + $"\nMarrja: {(fulfillment == Fulfillment.Delivery ? "Dërgesë në adresë" : "E merr vetë")}";
        if (deliveryDate.HasValue) message += $"\nData e kërkuar: {deliveryDate.Value:dd.MM.yyyy}";
        return message;
    }
    public async Task MarkReadAsync(Guid id, string userId, bool administrator, CancellationToken ct) => await unit.ExecuteAsync(async () =>
    {
        if (!await Visible(userId, administrator).AnyAsync(x => x.Id == id, ct)) throw new NotFoundException();
        if (!await db.NotificationReads.AnyAsync(x => x.NotificationId == id && x.UserId == userId, ct))
            db.NotificationReads.Add(new NotificationRead { NotificationId = id, UserId = userId, ReadAt = clock.Now });
        return true;
    }, ct);
}
