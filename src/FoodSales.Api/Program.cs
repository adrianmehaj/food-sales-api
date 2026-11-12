using System.Diagnostics;
using System.Net;
using FoodSales.Api.Extensions;
using FoodSales.Infrastructure;
using FoodSales.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFoodSales(builder.Configuration, builder.Environment.IsDevelopment())
    .AddApiServices(builder.Environment.IsDevelopment());
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Vite/ngrok terminates HTTPS before forwarding to this loopback-only API.
    // Keep production proxy configuration and client-IP handling unchanged.
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    forwarded.KnownProxies.Add(IPAddress.Loopback);
    forwarded.KnownProxies.Add(IPAddress.IPv6Loopback);
    app.UseForwardedHeaders(forwarded);
}

if (args.Any(x => x is "--migrate" or "--seed-demo" or "--create-admin" or "--grant-admin"))
{
    if (args.Contains("--seed-demo") && !app.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo seed is development-only.");
    await DatabaseSetup.RunAsync(app.Services, args.Contains("--migrate"), args.Contains("--seed-demo"), args.Contains("--create-admin"), args.Contains("--grant-admin"), CancellationToken.None);
    return;
}

app.UseExceptionHandler();
// A browser leaving a page cancels its reads. Handle that cancellation inside
// the application pipeline so the debugger does not classify it as unhandled.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        if (!context.Response.HasStarted)
            context.Response.StatusCode = 499;
    }
});
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    var watch = Stopwatch.StartNew();
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' https: data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    context.Response.OnCompleted(() =>
    {
        app.Logger.LogInformation("HTTP {Method} {Path} returned {Status} in {Duration}ms; TraceId {TraceId}", context.Request.Method, context.Request.Path, context.Response.StatusCode, watch.ElapsedMilliseconds, context.TraceIdentifier);
        return Task.CompletedTask;
    });
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = c =>
{
    if (c.File.Name is "sw.js" or "index.html" or "manifest.webmanifest")
        c.Context.Response.Headers.CacheControl = "no-cache";
}
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseDevelopmentSwagger();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.Run();
