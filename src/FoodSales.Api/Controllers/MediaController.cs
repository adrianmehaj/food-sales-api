using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSales.Api.Controllers;

[ApiController, Route("media"), AllowAnonymous]
public sealed class MediaController(ImageUploadService service) : ControllerBase
{
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName, CancellationToken ct)
    {
        var image = await service.OpenAsync(fileName, ct);
        if (image == null)
            return NotFound();
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return File(image.Content, image.ContentType, enableRangeProcessing: true);
    }
}
