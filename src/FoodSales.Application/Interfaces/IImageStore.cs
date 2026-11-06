namespace FoodSales.Application.Interfaces;

public sealed record ImageFile(Stream Content, string ContentType);
public sealed record UploadedImage(string Url);

public interface IImageStore
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct);
    Task<ImageFile?> OpenAsync(string fileName, CancellationToken ct);
}
