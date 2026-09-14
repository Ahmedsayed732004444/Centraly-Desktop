using Centraly.Api.Services.Abstraction;

namespace Centraly.Api.Infrastructure;

// Web-hosted implementation of IFileStorage: saves under wwwroot and returns an absolute
// URL built from the current request (same behavior the old static FileHelper had).
public class WebFileStorage(IWebHostEnvironment env, IHttpContextAccessor accessor) : IFileStorage
{
    public async Task<string?> SaveAsync(IFormFile? file, string folder, CancellationToken ct = default)
    {
        if (file is null)
            return null;

        var path = Path.Combine(env.WebRootPath, folder);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var extension = Path.GetExtension(file.FileName);
        var fileName = Guid.NewGuid().ToString().Replace("-", string.Empty);
        var fullPath = Path.Combine(path, fileName + extension);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var origin = accessor.HttpContext?.Request;
        return $"{origin?.Scheme}://{origin?.Host}/{folder}/{fileName}{extension}";
    }

    public void Delete(string? storedPath, string folder)
    {
        if (string.IsNullOrEmpty(storedPath))
            return;

        var fileName = Path.GetFileName(new Uri(storedPath).LocalPath);
        var path = Path.Combine(env.WebRootPath, folder, fileName);

        if (File.Exists(path))
            File.Delete(path);
    }
}
