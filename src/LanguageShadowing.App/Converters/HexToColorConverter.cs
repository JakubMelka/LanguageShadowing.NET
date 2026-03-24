using System.Globalization;

namespace LanguageShadowing.App.Converters;

public sealed class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text)
            ? Color.FromArgb(text)
            : Color.FromArgb("#A0A7B8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
