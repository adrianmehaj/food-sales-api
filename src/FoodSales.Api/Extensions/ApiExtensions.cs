using System.Threading.RateLimiting;
using FoodSales.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, bool development)
    {
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "FoodSales.Antiforgery";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails(); services.AddAuthorization();
        if (development)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options => options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "FoodSales API",
                Version = "v1",
                Description = "API për produktet, porositë, klientët dhe pagesat. Hyni përmes POST /api/account/login për veprimet e mbrojtura. Swagger dërgon automatikisht cookie-n e sesionit dhe tokenin antiforgery."
            }));
        }
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            AddLimit("auth", 10, TimeSpan.FromMinutes(5));
            AddLimit("checkout", 20, TimeSpan.FromMinutes(5));
            AddLimit("uploads", 30, TimeSpan.FromMinutes(5));
            void AddLimit(string name, int limit, TimeSpan window) => options.AddPolicy(name, context =>
                RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ =>
                    new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = window, QueueLimit = 0 }));
        });
        return services;
    }

    public static WebApplication UseDevelopmentSwagger(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        var assembly = typeof(ApiExtensions).Assembly;
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            // An external initializer supports CSRF-protected requests without relaxing the application's CSP.
            options.IndexStream = () => assembly.GetManifestResourceStream("FoodSales.Api.Swagger.index.html")!;
        });
        app.MapGet("/swagger/foodsales-init.js", () => Results.Stream(
            assembly.GetManifestResourceStream("FoodSales.Api.Swagger.init.js")!, "text/javascript"))
            .AllowAnonymous().ExcludeFromDescription();
        return app;
    }
}
