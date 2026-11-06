using FoodSales.Application.Common;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FoodSales.Infrastructure.Services;

public sealed class AccountService(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) : IAccountService
{
    public async Task<AccountView> RegisterAsync(RegisterRequest r, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = new IdentityUser { UserName = r.Email.Trim(), Email = r.Email.Trim(), LockoutEnabled = true };
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded) throw new AccountException("Regjistrimi dështoi. Përdorni email tjetër dhe fjalëkalim me të paktën 12 karaktere, shkronja të mëdha, të vogla, numër dhe simbol.");
        await signIn.SignInAsync(user, isPersistent: false);
        return new(user.Id, user.Email!, false);
    }
    public async Task<AccountView> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = await signIn.PasswordSignInAsync(r.Email.Trim(), r.Password, false, lockoutOnFailure: true);
        if (!result.Succeeded) throw new AccountException("Emaili ose fjalëkalimi është gabim, ose llogaria është bllokuar përkohësisht.", true);
        var user = await users.FindByEmailAsync(r.Email.Trim()) ?? throw new AccountException("Hyrja dështoi.", true);
        return new(user.Id, user.Email!, await users.IsInRoleAsync(user, "Admin"));
    }
    public Task LogoutAsync() => signIn.SignOutAsync();
    public async Task<AccountView?> CurrentAsync(string userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user == null) { await signIn.SignOutAsync(); return null; }
        var isAdmin = await users.IsInRoleAsync(user, "Admin");
        // Keep the cookie's role claims consistent with the account returned to the UI.
        if (signIn.Context.User.IsInRole("Admin") != isAdmin) await signIn.RefreshSignInAsync(user);
        return new(user.Id, user.Email!, isAdmin);
    }
}

public sealed class LocalClock : ILocalClock { public DateTime Now => DateTime.Now; }
