using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Centraly.Desktop.Converters;

// Shows an "empty state" placeholder overlay when a bound collection has no items.
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value switch
        {
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object>().Count(),
            _ => 0
        };
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
