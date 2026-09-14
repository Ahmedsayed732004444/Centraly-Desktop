using Centraly.Api.Contracts.Shared.Enums;
using System.Globalization;
using System.Windows.Data;

namespace Centraly.Desktop.Converters;

public class SaleTypeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SaleTypeDto.Retail => "قطاعي (تجزئة)",
        SaleTypeDto.Wholesale => "جملة",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
