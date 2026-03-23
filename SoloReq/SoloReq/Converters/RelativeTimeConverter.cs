using System.Globalization;
using System.Windows.Data;

namespace SoloReq.Converters;

public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
            return "";

        var diff = DateTime.Now - dt;

        if (diff.TotalSeconds < 60)
            return "только что";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} мин назад";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} ч назад";

        var today = DateTime.Today;
        if (dt.Date == today.AddDays(-1))
            return "вчера";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} дн назад";

        return dt.ToString("dd.MM.yyyy");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
