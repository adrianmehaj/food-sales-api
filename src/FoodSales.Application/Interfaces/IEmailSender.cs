namespace FoodSales.Application.Interfaces;

public interface IEmailSender
{
    bool IsAvailable
    {
        get;
    }
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct);
}
