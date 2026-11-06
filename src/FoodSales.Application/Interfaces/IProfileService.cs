using FoodSales.Application.Contracts;

namespace FoodSales.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileView> GetAsync(string userId, CancellationToken ct);
    Task<ProfileView> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct);
}
