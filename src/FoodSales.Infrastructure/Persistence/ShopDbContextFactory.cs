using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FoodSales.Infrastructure.Persistence;

/// <summary>Creates the migration context without starting the web application.</summary>
public sealed class ShopDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        var builder = new ConfigurationBuilder().SetBasePath(FindApiDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true);
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            builder.AddUserSecrets("food-sales-local-development");
        var configuration = builder.AddEnvironmentVariables().AddCommandLine(args).Build();
        var connection = configuration.GetConnectionString("Shop")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Shop for migrations.");
        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connection).Options;
        return new ShopDbContext(options);
    }

    private static string FindApiDirectory()
    {
        foreach (var origin in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(origin); directory != null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "src", "FoodSales.Api");
                if (File.Exists(Path.Combine(candidate, "FoodSales.Api.csproj"))) return candidate;
                if (File.Exists(Path.Combine(directory.FullName, "FoodSales.Api.csproj"))) return directory.FullName;
            }
        }
        throw new InvalidOperationException("Open FoodSales.sln or run migration tools from the backend checkout.");
    }
}
