using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Exceptions;
using FoodSales.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodSales.Infrastructure.Services;

public sealed class ProfileService(ShopDbContext db, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, IUnitOfWork unit) : IProfileService
{
    public async Task<ProfileView> GetAsync(string userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new NotFoundException();
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        return View(user, customer);
    }

    public async Task<ProfileView> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct)
    {
        await unit.ExecuteAsync(async () =>
        {
            var user = await users.FindByIdAsync(userId) ?? throw new NotFoundException();
            var email = request.Email.Trim();
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.City))
                throw new BusinessException("Plotësoni emrin, telefonin dhe qytetin.");
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.UserId == userId, ct);
            if (customer != null && request.Version != Convert.ToBase64String(customer.Version))
                throw new ConflictException("Profili ka ndryshuar. Rifreskojeni faqen para ruajtjes.");
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(request.CurrentPassword) || !(await signIn.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: true)).Succeeded)
                    throw new BusinessException("Për të ndryshuar emailin, shkruani fjalëkalimin aktual të saktë.");
                var existing = await users.FindByEmailAsync(email);
                if (existing != null && existing.Id != userId) throw new ConflictException("Ky email përdoret nga një llogari tjetër.");
                user.Email = email; user.UserName = email; user.EmailConfirmed = false;
            }
            user.PhoneNumber = request.Phone.Trim(); user.PhoneNumberConfirmed = false;
            var result = await users.UpdateAsync(user);
            if (!result.Succeeded) throw new BusinessException("Profili nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri.");
            if (customer == null) { customer = new Customer { UserId = userId }; db.Customers.Add(customer); }
            customer.Name = request.Name.Trim(); customer.Phone = request.Phone.Trim();
            customer.City = request.City.Trim(); customer.Address = request.Address.Trim();
            // An email-only edit must also advance the profile's concurrency version.
            if (db.Entry(customer).State != EntityState.Added) db.Entry(customer).Property(x => x.Name).IsModified = true;
            return true;
        }, ct);
        var updated = await users.FindByIdAsync(userId) ?? throw new NotFoundException();
        await signIn.RefreshSignInAsync(updated);
        return await GetAsync(userId, ct);
    }

    private static ProfileView View(IdentityUser user, Customer? customer) => new(customer?.Name ?? "", user.Email!, customer?.Phone ?? user.PhoneNumber ?? "", customer?.City ?? "Skenderaj", customer?.Address ?? "", customer == null ? null : Convert.ToBase64String(customer.Version));
}
