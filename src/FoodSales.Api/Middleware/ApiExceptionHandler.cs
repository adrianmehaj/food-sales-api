using FoodSales.Application.Common;
using FoodSales.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FoodSales.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            BusinessException e => (422, e.Message),
            NotFoundException => (404, "Të dhënat nuk u gjetën."),
            ConflictException e => (409, e.Message),
            ForbiddenException e => (403, e.Message),
            AccountException e => (e.Unauthorized ? 401 : 400, e.Message),
            DbUpdateConcurrencyException => (409, "Të dhënat kanë ndryshuar. Rifreskojeni faqen."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (409, "Kërkesa është regjistruar më parë. Rifreskojeni faqen."),
            SqlException => (503, "Lidhja me databazën nuk është e disponueshme. Provoni përsëri."),
            _ => (500, "Ndodhi një gabim. Provoni përsëri.")
        };
        if (status >= 500)
            logger.LogError(exception, "Request failed; TraceId {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier } }, ct);
        return true;
    }
}
