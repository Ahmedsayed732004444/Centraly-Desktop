using Centraly.Desktop.Services;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
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

// Same resolution as ProductImageConverter, but as a Brush - for filling an Ellipse to get
// a perfectly round product photo (an <Image> can't be clipped to a circle on its own).
public class ProductImageBrushConverter : IValueConverter
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

        var brush = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Companion to ProductImageConverter: Visible when the same ImageUrl resolves to a real
// file on disk, Collapsed otherwise - lets a card show either <Image> or the avatar
// fallback for the same bound property without a multi-converter.
public class ImageAvailableToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var relativePath = value as string;
        if (string.IsNullOrEmpty(relativePath))
            return Visibility.Collapsed;

        return File.Exists(LocalFileStorage.Resolve(relativePath)) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Inverse of ImageAvailableToVisibilityConverter - shows the avatar fallback exactly when
// the image doesn't.
public class ImageUnavailableToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var relativePath = value as string;
        if (string.IsNullOrEmpty(relativePath))
            return Visibility.Visible;

        return File.Exists(LocalFileStorage.Resolve(relativePath)) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
