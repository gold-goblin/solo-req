using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SoloReq.Converters;

public class StatusCodeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int statusCode)
        {
            return statusCode switch
            {
                >= 200 and < 300 => new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)),
                >= 300 and < 400 => new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),
                >= 400 and < 500 => new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xAA)),
                >= 500 => new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47)),
                _ => new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x7B))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x7B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
