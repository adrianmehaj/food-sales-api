namespace FoodSales.Application.Contracts;

public sealed record NotificationView(Guid Id, Guid OrderId, string Title, string Message, DateTime CreatedAt, bool IsRead);
public sealed record NotificationInbox(Page<NotificationView> Notifications, int UnreadCount);
