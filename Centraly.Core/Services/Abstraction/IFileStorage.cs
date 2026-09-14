namespace Centraly.Api.Services.Abstraction;

// Host-specific: Api implements this with WebFileStorage (builds an absolute URL from
// the current HttpContext), Desktop implements it with a local-disk storage that returns
// a relative path instead. Services must depend on this, never on IWebHostEnvironment /
// IHttpContextAccessor directly, so they stay usable outside a web host.
public interface IFileStorage
{
    Task<string?> SaveAsync(IFormFile? file, string folder, CancellationToken ct = default);
    void Delete(string? storedPath, string folder);
}
