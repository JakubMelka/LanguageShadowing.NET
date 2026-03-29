using System.Globalization;

namespace LanguageShadowing.Core.Models;

/// <summary>
/// Describes one selectable synthesis voice.
/// </summary>
/// <param name="Id">The platform-specific identifier used to select the voice.</param>
/// <param name="DisplayName">The human-readable voice name.</param>
/// <param name="Locale">The BCP-47 locale exposed by the platform.</param>
/// <param name="Gender">An optional gender label exposed by the platform.</param>
/// <param name="IsDefault">Indicates whether the voice matches the platform default voice.</param>
public sealed record VoiceInfo(
    string Id,
    string DisplayName,
    string Locale,
    string? Gender = null,
    bool IsDefault = false)
{
    /// <summary>
    /// Gets a localized display name for <see cref="Locale"/> when the locale can be resolved by <see cref="CultureInfo"/>.
    /// </summary>
    public string LanguageDisplay => TryGetLanguageDisplay(Locale);

    /// <summary>
    /// Gets the label shown in the voice picker.
    /// </summary>
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
