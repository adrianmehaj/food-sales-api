using FoodSales.Application.Contracts;

namespace FoodSales.Application.Interfaces;

public interface IPasswordRecoveryService
{
    Task RequestAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task ResetAsync(ResetPasswordRequest request, CancellationToken ct);
}
