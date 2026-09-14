using Centraly.Api.Contracts.Inventory.Products;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

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

// Exact hex values from PosProductGrid.tsx's statusClass / stock-count text colors.
public class StockStatusBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush OutOfStockBg = new(Color.FromRgb(0xfc, 0xe8, 0xe6));
    private static readonly SolidColorBrush LowStockBg = new(Color.FromRgb(0xfe, 0xf7, 0xe0));
    private static readonly SolidColorBrush InStockBg = new(Color.FromRgb(0xe6, 0xf4, 0xed));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ProductResponse { IsOutOfStock: true } => OutOfStockBg,
        ProductResponse { IsLowStock: true } => LowStockBg,
        _ => InStockBg
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StockStatusForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush OutOfStockFg = new(Color.FromRgb(0xc5, 0x22, 0x1f));
    private static readonly SolidColorBrush LowStockFg = new(Color.FromRgb(0xea, 0x86, 0x00));
    private static readonly SolidColorBrush InStockFg = new(Color.FromRgb(0x0f, 0x8e, 0x4c));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ProductResponse { IsOutOfStock: true } => OutOfStockFg,
        ProductResponse { IsLowStock: true } => LowStockFg,
        _ => InStockFg
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class PositiveIntToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
