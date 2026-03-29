using System.Globalization;

namespace LanguageShadowing.App.Converters;

/// <summary>
/// Converts a hexadecimal color string into a MAUI <see cref="Color"/>.
/// </summary>
public sealed class HexToColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text)
            ? Color.FromArgb(text)
            : Color.FromArgb("#A0A7B8");
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
