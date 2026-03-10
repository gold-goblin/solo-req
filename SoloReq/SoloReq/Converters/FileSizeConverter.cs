using System.Globalization;
using System.Windows.Data;

namespace SoloReq.Converters;

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} Б",
                < 1024 * 1024 => $"{bytes / 1024.0:F1} КБ",
                _ => $"{bytes / (1024.0 * 1024.0):F1} МБ"
            };
        }
        return "0 Б";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
