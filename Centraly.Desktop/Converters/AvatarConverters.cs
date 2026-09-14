using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Centraly.Desktop.Converters;

// Ports Centraly-Frontend/src/shared/components/ui/Avatar.tsx exactly: same hash over the
// name's char codes, same 8-color palette (as bg-*-50/text-*-600 hex equivalents), so a
// given product name gets the identical avatar color here as it did in the old frontend.
internal static class AvatarPalette
{
    public static readonly (Color Background, Color Foreground)[] Colors =
    [
        (Color.FromRgb(0xee, 0xf2, 0xff), Color.FromRgb(0x4f, 0x46, 0xe5)), // indigo
        (Color.FromRgb(0xef, 0xf6, 0xff), Color.FromRgb(0x25, 0x63, 0xeb)), // blue
        (Color.FromRgb(0xec, 0xfd, 0xf5), Color.FromRgb(0x05, 0x96, 0x69)), // emerald
        (Color.FromRgb(0xff, 0xfb, 0xeb), Color.FromRgb(0xb4, 0x53, 0x09)), // amber
        (Color.FromRgb(0xff, 0xf1, 0xf2), Color.FromRgb(0xe1, 0x1d, 0x48)), // rose
        (Color.FromRgb(0xfa, 0xf5, 0xff), Color.FromRgb(0x93, 0x33, 0xea)), // purple
        (Color.FromRgb(0xf0, 0xfd, 0xfa), Color.FromRgb(0x0d, 0x94, 0x88)), // teal
        (Color.FromRgb(0xff, 0xf7, 0xed), Color.FromRgb(0xea, 0x58, 0x0c)), // orange
    ];

    public static (Color Background, Color Foreground) For(string? name)
    {
        var text = string.IsNullOrEmpty(name) ? "?" : name;
        var hash = 0;
        unchecked
        {
            foreach (var c in text)
                hash = (hash << 5) - hash + c;
        }
        return Colors[Math.Abs(hash) % Colors.Length];
    }

    public static string InitialFor(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "؟" : name.Trim()[..1].ToUpperInvariant();
}

public class AvatarBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new SolidColorBrush(AvatarPalette.For(value as string).Background);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class AvatarForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new SolidColorBrush(AvatarPalette.For(value as string).Foreground);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class AvatarInitialConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvatarPalette.InitialFor(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
