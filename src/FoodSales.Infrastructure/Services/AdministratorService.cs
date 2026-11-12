using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodSales.Infrastructure.Services;

public sealed class AdministratorService(ShopDbContext db, UserManager<IdentityUser> users, IUnitOfWork unit,
    PaginationOptions pagination, ILocalClock clock, ILogger<AdministratorService> logger) : IAdministratorService
{
    public async Task<Page<AdministratorView>> ListAsync(int page, int? size, string? search, string actorId, CancellationToken ct)
    {
        await RequireAdministratorAsync(actorId, ct);
        var query = from user in db.Users.AsNoTracking()
                    join membership in db.UserRoles on user.Id equals membership.UserId
                    join role in db.Roles on membership.RoleId equals role.Id
                    where role.NormalizedName == "ADMIN"
                    select user;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = users.NormalizeEmail(search.Trim());
            query = query.Where(user => user.NormalizedEmail != null && user.NormalizedEmail.StartsWith(normalized));
        }
        var p = pagination.Normalize(page, size);
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(user => user.Email).ThenBy(user => user.Id)
            .Skip((p.Page - 1) * p.Size).Take(p.Size)
            .Select(user => new AdministratorView(user.Id, user.Email!)).ToListAsync(ct);
        return new(items, p.Page, p.Size, count);
    }

    public async Task<AdministratorView> CreateAsync(CreateAdministratorRequest request, string actorId, CancellationToken ct)
    {
        var created = await unit.ExecuteAsync(async () =>
        {
            await RequireAdministratorAsync(actorId, ct);
            var email = request.Email.Trim();
            if (await users.FindByEmailAsync(email) != null)
                throw new ConflictException("Ky email ka llogari. Përdorni opsionin “Llogari ekzistuese” për t'i dhënë rol admin pa ndryshuar fjalëkalimin.");
            var user = new IdentityUser { Email = email, UserName = email, LockoutEnabled = true };
            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new AccountException("Llogaria nuk u krijua. Kontrolloni emailin dhe përdorni fjalëkalim me të paktën 12 karaktere, shkronja të mëdha/të vogla, numër dhe simbol.");
            RequireRoleResult(await users.AddToRoleAsync(user, "Admin"));
            return new AdministratorView(user.Id, user.Email!);
        }, ct);
        logger.LogInformation("Administrator {ActorId} created administrator account {TargetId} at {LocalTime}.", actorId, created.Id, clock.Now);
        return created;
    }

    public async Task<AdministratorView> GrantAsync(GrantAdministratorRequest request, string actorId, CancellationToken ct)
    {
        var (result, changed) = await unit.ExecuteAsync(async () =>
        {
            await RequireAdministratorAsync(actorId, ct);
            var user = await users.FindByEmailAsync(request.Email.Trim()) ?? throw new NotFoundException();
            var isAdmin = await users.IsInRoleAsync(user, "Admin");
            if (!isAdmin)
                RequireRoleResult(await users.AddToRoleAsync(user, "Admin"));
            return (new AdministratorView(user.Id, user.Email!), !isAdmin);
        }, ct);
        if (changed)
            logger.LogInformation("Administrator {ActorId} granted Admin role to account {TargetId} at {LocalTime}.", actorId, result.Id, clock.Now);
        return result;
    }

    private async Task RequireAdministratorAsync(string actorId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var actor = await users.FindByIdAsync(actorId);
        if (actor == null || !await users.IsInRoleAsync(actor, "Admin"))
            throw new ForbiddenException("Vetëm administratorët mund të menaxhojnë administratorë të tjerë.");
    }

    private static void RequireRoleResult(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ConflictException("Roli nuk u ruajt. Rifreskojeni listën dhe provoni përsëri.");
    }
}
