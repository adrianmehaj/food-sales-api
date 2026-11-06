namespace FoodSales.Domain.Entities;

public sealed class PlatformNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string? RecipientUserId { get; set; }
    public bool ForAdministrators { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationRead
{
    public Guid NotificationId { get; set; }
    public string UserId { get; set; } = "";
    public DateTime ReadAt { get; set; }
}
