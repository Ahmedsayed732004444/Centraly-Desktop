using Centraly.Desktop.Services;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Centraly.Desktop.Converters;

// Resolves a Product/Wallet ImageUrl (a relative path like "uploads/products/xyz.webp",
// as returned by LocalFileStorage.SaveAsync) into a BitmapImage the UI can bind to.
public class ProductImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var relativePath = value as string;
        if (string.IsNullOrEmpty(relativePath))
            return null;

        var fullPath = LocalFileStorage.Resolve(relativePath);
        if (!File.Exists(fullPath))
            return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
