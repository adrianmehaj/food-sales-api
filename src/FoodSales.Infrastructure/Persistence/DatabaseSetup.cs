using System.Data;
using FoodSales.Domain.Entities;
using FoodSales.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSales.Infrastructure.Persistence;

public static class DatabaseSetup
{
    public static async Task RunAsync(IServiceProvider provider, bool migrate, bool demo, bool createAdmin, bool grantAdmin, CancellationToken ct)
    {
        if (createAdmin && grantAdmin)
            throw new InvalidOperationException("Choose either --create-admin for a new account or --grant-admin for an existing account.");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        if (migrate)
            await db.Database.MigrateAsync(ct);
        // CLI bootstrap is only available before the first administrator exists.
        await using var bootstrapTransaction = createAdmin || grantAdmin
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        if ((createAdmin || grantAdmin) && await db.UserRoles.AnyAsync(membership =>
            db.Roles.Any(role => role.Id == membership.RoleId && role.NormalizedName == "ADMIN"), ct))
            throw new InvalidOperationException("The first administrator already exists. Add other administrators from the authenticated Administrators panel.");
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin"))
            Require(await roles.CreateAsync(new IdentityRole("Admin")));
        if (createAdmin)
        {
            var email = Environment.GetEnvironmentVariable("FOODSALES_ADMIN_EMAIL") ?? throw new InvalidOperationException("Set FOODSALES_ADMIN_EMAIL.");
            var password = Environment.GetEnvironmentVariable("FOODSALES_ADMIN_PASSWORD") ?? throw new InvalidOperationException("Set FOODSALES_ADMIN_PASSWORD.");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            if (await users.FindByEmailAsync(email) != null)
                throw new InvalidOperationException("Account already exists; refusing to change its role or password.");
            var user = new IdentityUser { Email = email, UserName = email, LockoutEnabled = true };
            Require(await users.CreateAsync(user, password));
            Require(await users.AddToRoleAsync(user, "Admin"));
        }
        if (grantAdmin)
        {
            var email = Environment.GetEnvironmentVariable("FOODSALES_ADMIN_EMAIL") ?? throw new InvalidOperationException("Set FOODSALES_ADMIN_EMAIL.");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await users.FindByEmailAsync(email.Trim()) ?? throw new InvalidOperationException("Account not found. Use --create-admin to create a new account.");
            if (!await users.IsInRoleAsync(user, "Admin"))
                Require(await users.AddToRoleAsync(user, "Admin"));
        }
        if (bootstrapTransaction != null)
            await bootstrapTransaction.CommitAsync(ct);
        if (demo && !await db.Products.AnyAsync(ct))
        {
            AddProduct(db, "Djathë i bardhë", "Djathë i freskët, me shije të butë. Pesha përfundimtare konfirmohet gjatë përgatitjes.", "Djathë", 5m, 80m, Unit.Kilogram, "https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?auto=format&fit=crop&w=800&q=85");
            AddProduct(db, "Qumësht i freskët", "Qumësht i freskët për tryezën tuaj. Ruajeni në frigorifer.", "Qumësht", 1m, 120m, Unit.Liter, "https://images.unsplash.com/photo-1563636619-e9143da7973b?auto=format&fit=crop&w=800&q=85");
            AddProduct(db, "Kos natyral", "Kos me shije të lehtë dhe teksturë kremoze, për çdo ditë.", "Kos & ajkë", 1.5m, 60m, Unit.Liter, "https://images.unsplash.com/photo-1488477181946-6428a0291777?auto=format&fit=crop&w=800&q=85");
            AddProduct(db, "Djathë i stazhionuar", "Shije e plotë për një mëngjes të mirë ose për ta ndarë me familjen.", "Djathë", 8m, 30m, Unit.Kilogram, "https://images.unsplash.com/photo-1452195100486-9cc805987862?auto=format&fit=crop&w=800&q=85");
            await db.SaveChangesAsync(ct);
        }
    }
    private static void AddProduct(ShopDbContext db, string name, string description, string category, decimal price, decimal stock, Unit unit, string image)
    {
        var p = new Product();
        p.Update(name, description, image, category, price, stock, unit, true, null, null, null);
        db.Products.Add(p);
    }
    private static void Require(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
