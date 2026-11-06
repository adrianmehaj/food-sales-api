using FoodSales.Application.Common;
using FoodSales.Application.Interfaces;
using FoodSales.Application.Services;
using FoodSales.Infrastructure.Persistence;
using FoodSales.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodSales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFoodSales(this IServiceCollection services, IConfiguration configuration, bool development)
    {
        var connection = configuration.GetConnectionString("Shop") ?? throw new InvalidOperationException("Configure ConnectionStrings:Shop via environment or user secrets.");
        services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(connection));
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<ShopDbContext>().AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "FoodSales.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        var pagination = configuration.GetSection("Pagination").Get<PaginationOptions>() ?? new PaginationOptions();
        if (pagination.DefaultPageSize < 1 || pagination.MaxPageSize > 100 || pagination.MaxPageSize < pagination.DefaultPageSize)
            throw new InvalidOperationException("Invalid pagination configuration.");
        services.AddSingleton(pagination);
        var imageUploads = configuration.GetSection("ImageUploads").Get<ImageUploadOptions>() ?? new ImageUploadOptions();
        if (imageUploads.MaxBytes is < 1 or > 5 * 1024 * 1024) throw new InvalidOperationException("ImageUploads:MaxBytes must be between 1 byte and 5 MiB.");
        services.AddSingleton(imageUploads);
        services.AddSingleton<IImageStore, LocalImageStore>();
        services.AddScoped<ImageUploadService>();
        services.AddSingleton<ILocalClock, LocalClock>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdministratorService, AdministratorService>();
        services.AddScoped<CatalogService>(); services.AddScoped<CustomerService>(); services.AddScoped<OrderService>();
        return services;
    }
}
