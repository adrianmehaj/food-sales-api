using FoodSales.Application.Interfaces;
using FoodSales.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodSales.Api.Controllers;

[ApiController, Route("api/admin/images"), Authorize(Roles = "Admin")]
public sealed class ImagesController(ImageUploadService service) : ControllerBase
{
    [HttpPost, Consumes("multipart/form-data"), EnableRateLimiting("uploads")]
    [RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<UploadedImage>> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var image = await service.UploadAsync(stream, file.FileName, file.ContentType, file.Length, ct);
        return Created(image.Url, image);
    }
}

[ApiController, Route("media"), AllowAnonymous]
public sealed class MediaController(ImageUploadService service) : ControllerBase
{
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName, CancellationToken ct)
    {
        var image = await service.OpenAsync(fileName, ct);
        if (image == null) return NotFound();
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return File(image.Content, image.ContentType, enableRangeProcessing: true);
    }
}
