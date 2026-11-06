using FoodSales.Application.Contracts;

namespace FoodSales.Application.Interfaces;

public interface IAdministratorService
{
    Task<Page<AdministratorView>> ListAsync(int page, int? size, string? search, string actorId, CancellationToken ct);
    Task<AdministratorView> CreateAsync(CreateAdministratorRequest request, string actorId, CancellationToken ct);
    Task<AdministratorView> GrantAsync(GrantAdministratorRequest request, string actorId, CancellationToken ct);
}
