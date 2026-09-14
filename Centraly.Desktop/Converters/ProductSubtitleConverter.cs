using Centraly.Api.Contracts.Inventory.Products;
using System.Globalization;
using System.Windows.Data;

namespace Centraly.Desktop.Converters;

// Mirrors PosProductGrid.tsx / PosCart.tsx: show the product's key/value properties
// (joined "value - value") when any exist, otherwise fall back to the category name.
public class ProductSubtitleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ProductResponse { Properties.Count: > 0 } p => string.Join(" - ", p.Properties.Values),
        ProductResponse p => p.Category?.Name ?? string.Empty,
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
