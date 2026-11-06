using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Exceptions;
using FoodSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace FoodSales.Infrastructure.Services;

public sealed class NotificationService(ShopDbContext db, ILocalClock clock, PaginationOptions pagination, IUnitOfWork unit) : INotificationService
{
    public void OrderCreated(Order order)
    {
        var items = string.Join(", ", order.Lines.Take(5).Select(x => $"{x.RequestedQuantity:0.###} {x.ProductName}"));
        var message = $"{order.CustomerName}: {items}. Totali i porositur: {order.Total:0.00} €.";
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
            var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((p.Page - 1) * p.Size).Take(p.Size)
                .Select(x => new NotificationView(x.Id, x.OrderId, x.Title, x.Message, x.CreatedAt,
                    db.NotificationReads.Any(r => r.NotificationId == x.Id && r.UserId == userId))).ToListAsync(ct);
            return new(new(items, p.Page, p.Size, total), unread);
        }
        // SqlClient can report an acknowledged cancellation as SQL error 0
        // instead of OperationCanceledException, especially while waiting for a lock.
        catch (SqlException exception) when (ct.IsCancellationRequested && exception.Number == 0)
        {
            throw new OperationCanceledException("Notification request cancelled.", exception, ct);
        }
    }
    public async Task MarkReadAsync(Guid id, string userId, bool administrator, CancellationToken ct) => await unit.ExecuteAsync(async () =>
    {
        if (!await Visible(userId, administrator).AnyAsync(x => x.Id == id, ct)) throw new NotFoundException();
        if (!await db.NotificationReads.AnyAsync(x => x.NotificationId == id && x.UserId == userId, ct))
            db.NotificationReads.Add(new NotificationRead { NotificationId = id, UserId = userId, ReadAt = clock.Now });
        return true;
    }, ct);
}
