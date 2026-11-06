using FoodSales.Application.Contracts;
using FoodSales.Domain.Entities;

namespace FoodSales.Application.Interfaces;

public interface INotificationService
{
    void OrderCreated(Order order);
    void OrderConfirmed(Order order);
    Task<NotificationInbox> InboxAsync(string userId, bool administrator, int page, int? size, CancellationToken ct);
    Task MarkReadAsync(Guid id, string userId, bool administrator, CancellationToken ct);
}
