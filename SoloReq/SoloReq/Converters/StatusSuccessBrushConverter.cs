using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SoloReq.Converters;

public class StatusSuccessBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int statusCode && statusCode >= 200 && statusCode < 300)
            return new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)); // #4EC9B0

        return new SolidColorBrush(Color.FromRgb(0xF4, 0x47, 0x47)); // #F44747
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
