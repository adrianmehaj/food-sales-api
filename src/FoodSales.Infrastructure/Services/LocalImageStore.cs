using FoodSales.Application.Common;
using FoodSales.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FoodSales.Infrastructure.Services;

public sealed class LocalImageStore : IImageStore
{
    private readonly string _root;

    public LocalImageStore(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration["ImageUploads:RootPath"] ?? "App_Data/uploads";
        if (string.IsNullOrWhiteSpace(configuredPath)) throw new InvalidOperationException("Configure ImageUploads:RootPath.");
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct)
    {
        var fileName = Guid.NewGuid().ToString("N") + extension;
        if (ImageFileNames.ContentType(fileName) == null) throw new InvalidOperationException("Unsupported image format.");
        Directory.CreateDirectory(_root);
        var temporaryPath = Path.Combine(_root, fileName + ".tmp");
        var finalPath = Path.Combine(_root, fileName);
        try
        {
            await using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await content.CopyToAsync(file, ct);
                await file.FlushAsync(ct);
            }
            File.Move(temporaryPath, finalPath);
            return fileName;
        }
        finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
    }

    public Task<ImageFile?> OpenAsync(string fileName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var type = ImageFileNames.ContentType(fileName);
        if (type == null) return Task.FromResult<ImageFile?>(null);
        try
        {
            Stream file = new FileStream(Path.Combine(_root, fileName), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            return Task.FromResult<ImageFile?>(new(file, type));
        }
        catch (FileNotFoundException) { return Task.FromResult<ImageFile?>(null); }
        catch (DirectoryNotFoundException) { return Task.FromResult<ImageFile?>(null); }
    }
}
