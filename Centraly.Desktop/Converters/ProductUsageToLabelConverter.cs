using Centraly.Api.Contracts.Shared.Enums;
using System.Globalization;
using System.Windows.Data;

namespace Centraly.Desktop.Converters;

public class ProductUsageToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ProductUsageDto.SaleOnly => "بيع فقط",
        ProductUsageDto.MaintenanceOnly => "صيانة فقط",
        ProductUsageDto.SaleAndMaintenance => "بيع وصيانة",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
