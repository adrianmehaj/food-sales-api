namespace FoodSales.Application.Common;

public sealed class ImageUploadOptions
{
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}

public static class ImageFileNames
{
    public static string? ContentType(string fileName)
    {
        var extensionAt = fileName.LastIndexOf('.');
        if (extensionAt != 32 || !Guid.TryParseExact(fileName[..extensionAt], "N", out _)) return null;
        return fileName[extensionAt..].ToLowerInvariant() switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
    }

    public static bool IsLocalUrl(string value) => value.StartsWith("/media/", StringComparison.Ordinal) && ContentType(value[7..]) != null;
}
