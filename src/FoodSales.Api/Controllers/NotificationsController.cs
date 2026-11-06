using System.Security.Claims;
using FoodSales.Application.Contracts;
using FoodSales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<ActionResult<NotificationInbox>> Get(int page = 1, int? pageSize = null, CancellationToken ct = default)
    {
        try { return Ok(await service.InboxAsync(UserId, User.IsInRole("Admin"), page, pageSize, ct)); }
        // Handle this read before MVC awaits a cancelled Task: .NET 9+ debuggers
        // otherwise classify navigation/poll cancellations as user-unhandled.
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            return StatusCode(499);
        }
    }
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await service.MarkReadAsync(id, UserId, User.IsInRole("Admin"), ct); return NoContent(); }
}
