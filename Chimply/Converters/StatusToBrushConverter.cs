using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Chimply.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Up" => new SolidColorBrush(Color.Parse("#4CAF50")),
            "New" => new SolidColorBrush(Color.Parse("#FFEB3B")),
            "Down" => new SolidColorBrush(Color.Parse("#F44336")),
            _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
