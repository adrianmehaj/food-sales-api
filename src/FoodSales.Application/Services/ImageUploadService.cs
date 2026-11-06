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
        var expectedType = extension switch { ".jpg" or ".jpeg" or ".jfif" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
        var suppliedType = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (suppliedType is "image/jpg" or "image/pjpeg") suppliedType = "image/jpeg";
        // Some file pickers omit the MIME type. The file signature is still checked below.
        if (expectedType == null || (suppliedType is not ("" or "application/octet-stream") && expectedType != suppliedType))
            throw new BusinessException("Zgjidhni një fotografi JPG, JPEG, JFIF, PNG ose WebP.");
        await using var buffer = new MemoryStream((int)length);
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > options.MaxBytes) throw new BusinessException("Fotografia e tejkalon madhësinë e lejuar.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        var actualType = DetectContentType(buffer.GetBuffer(), (int)buffer.Length);
        if (buffer.Length != length || actualType == null)
            throw new BusinessException("Skedari nuk është një fotografi e vlefshme JPG, JPEG, JFIF, PNG ose WebP.");
        buffer.Position = 0;
        // Store a canonical extension matching the bytes, even if a photo was renamed.
        var storedExtension = actualType switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".webp" };
        var storedName = await store.SaveAsync(buffer, storedExtension, ct);
        return new("/media/" + storedName);
    }

    private static string? DetectContentType(byte[] bytes, int length)
    {
        if (length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return "image/jpeg";
        if (length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }

    public Task<ImageFile?> OpenAsync(string fileName, CancellationToken ct) => store.OpenAsync(fileName, ct);
}
