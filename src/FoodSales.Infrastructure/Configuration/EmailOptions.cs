using System.Net.Mail;

namespace FoodSales.Infrastructure.Configuration;

public sealed class EmailOptions
{
    public string DeliveryMethod { get; init; } = "Disabled";
    public string SmtpHost { get; init; } = "";
    public int SmtpPort { get; init; } = 587;
    public string FromAddress { get; init; } = "";
    public string FromName { get; init; } = "Tryeza";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";

    public bool IsValid(bool development) => DeliveryMethod switch
    {
        "Disabled" => true,
        "DevelopmentFile" => development,
        "Smtp" => !string.IsNullOrWhiteSpace(SmtpHost) && SmtpPort is > 0 and <= 65535
            && MailAddress.TryCreate(FromAddress, out _) && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password),
        _ => false
    };
}
