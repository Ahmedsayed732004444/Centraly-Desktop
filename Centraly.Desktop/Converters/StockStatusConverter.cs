using Centraly.Api.Contracts.Inventory.Products;
using System.Globalization;
using System.Windows.Data;

namespace Centraly.Desktop.Converters;

// Mirrors the old frontend's ProductStatusBadge: bind the whole row ({Binding}) since the
// label depends on two fields (IsOutOfStock / IsLowStock) at once.
public class StockStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ProductResponse { IsOutOfStock: true } => "نفد المخزون",
        ProductResponse { IsLowStock: true } => "مخزون منخفض",
        ProductResponse => "متوفر",
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
