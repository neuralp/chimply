using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Chimply.Converters;

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value?.ToString();
        return string.IsNullOrEmpty(hex)
            ? new SolidColorBrush(Color.Parse("#9E9E9E"))
            : new SolidColorBrush(Color.Parse(hex));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
