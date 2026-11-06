using FoodSales.Application.Common;
using FoodSales.Application.Interfaces;
using FoodSales.Domain.Exceptions;

namespace FoodSales.Application.Services;

public sealed class ImageUploadService(IImageStore store, ImageUploadOptions options)
{
    public async Task<UploadedImage> UploadAsync(Stream content, string fileName, string contentType, long length, CancellationToken ct)
    {
        if (length <= 0 || length > options.MaxBytes) throw new BusinessException($"Fotografia duhet të jetë deri në {options.MaxBytes / (1024 * 1024d):0.#} MB.");
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var expectedType = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
        if (expectedType == null || !string.Equals(expectedType, contentType, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Zgjidhni një fotografi JPG, PNG ose WebP.");
        await using var buffer = new MemoryStream((int)length);
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > options.MaxBytes) throw new BusinessException("Fotografia e tejkalon madhësinë e lejuar.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        if (buffer.Length != length || !MatchesSignature(buffer.GetBuffer(), (int)buffer.Length, expectedType))
            throw new BusinessException("Skedari nuk është një fotografi e vlefshme JPG, PNG ose WebP.");
        buffer.Position = 0;
        var storedExtension = expectedType == "image/jpeg" ? ".jpg" : extension;
        var storedName = await store.SaveAsync(buffer, storedExtension, ct);
        return new("/media/" + storedName);
    }

    private static bool MatchesSignature(byte[] bytes, int length, string type) => type switch
    {
        "image/jpeg" => length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "image/png" => length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/webp" => length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    public Task<ImageFile?> OpenAsync(string fileName, CancellationToken ct) => store.OpenAsync(fileName, ct);
}
