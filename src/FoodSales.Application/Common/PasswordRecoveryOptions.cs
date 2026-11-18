namespace FoodSales.Application.Common;

public sealed class PasswordRecoveryOptions
{
    public string PublicAppUrl { get; init; } = "";
    public int TokenLifetimeMinutes { get; init; } = 30;
}
