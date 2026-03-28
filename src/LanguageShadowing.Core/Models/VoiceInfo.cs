using System.Globalization;

namespace LanguageShadowing.Core.Models;

public sealed record VoiceInfo(
    string Id,
    string DisplayName,
    string Locale,
    string? Gender = null,
    bool IsDefault = false)
{
    public string LanguageDisplay => TryGetLanguageDisplay(Locale);

    public string SelectionLabel => string.Join(" | ", new[]
    {
        DisplayName,
        string.IsNullOrWhiteSpace(Gender) ? "Unknown" : Gender,
        string.IsNullOrWhiteSpace(LanguageDisplay) ? "Unknown language" : LanguageDisplay
    });

    private static string TryGetLanguageDisplay(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return string.Empty;
        }

        try
        {
            return CultureInfo.GetCultureInfo(locale).DisplayName;
        }
        catch (CultureNotFoundException)
        {
            return locale;
        }
    }
}
