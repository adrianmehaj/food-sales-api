using FoodSales.Application.Contracts;

namespace FoodSales.Application.Interfaces;

public interface IAccountService
{
    Task<AccountView> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AccountView> LoginAsync(LoginRequest request, CancellationToken ct);
    Task LogoutAsync();
    Task<AccountView?> CurrentAsync(string userId, CancellationToken ct);
}
