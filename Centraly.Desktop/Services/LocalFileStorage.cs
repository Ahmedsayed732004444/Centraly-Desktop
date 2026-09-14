using Microsoft.AspNetCore.Http;

namespace Centraly.Desktop.Services;

// Desktop's IFileStorage: saves under %LocalAppData%\Centraly\uploads and returns a
// relative path (not an absolute URL - there's no host to build one from). The UI resolves
// that relative path back to a full local path when it needs to display an image.
public class LocalFileStorage : IFileStorage
{
    // ProductService/WalletService always pass folder="uploads/products" / "uploads/wallets"
    // (the same literal they used to pass to the old web FileHelper) - Root must NOT also
    // end in "uploads", or the two combine into ".../uploads/uploads/products/...".
    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Centraly");

    public async Task<string?> SaveAsync(IFormFile? file, string folder, CancellationToken ct = default)
    {
        if (file is null)
            return null;

        var dir = Path.Combine(Root, folder);
        Directory.CreateDirectory(dir);

        var extension = Path.GetExtension(file.FileName);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        var fullPath = Path.Combine(dir, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        return $"{folder}/{fileName}";
    }

    public void Delete(string? storedPath, string folder)
    {
        if (string.IsNullOrEmpty(storedPath))
            return;

        var fileName = Path.GetFileName(storedPath);
        var path = Path.Combine(Root, folder, fileName);

        if (File.Exists(path))
            File.Delete(path);
    }

    public static string Resolve(string? storedPath) =>
        string.IsNullOrEmpty(storedPath) ? string.Empty : Path.Combine(Root, storedPath.Replace('/', Path.DirectorySeparatorChar));
}
