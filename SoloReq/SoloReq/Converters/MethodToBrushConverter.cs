using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SoloReq.Converters;

public class MethodToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)),
                "POST" => new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xAA)),
                "PUT" => new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),
                "DELETE" => new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47)),
                "PATCH" => new SolidColorBrush(Color.FromRgb(0xFF, 0x92, 0x48)),
                _ => new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE6))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE6));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
