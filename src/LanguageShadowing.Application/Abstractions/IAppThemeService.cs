namespace LanguageShadowing.Application.Abstractions;

/// <summary>
/// Applies the application's theme preference to the active MAUI application.
/// </summary>
public interface IAppThemeService
{
    /// <summary>
    /// Applies the requested theme preference immediately.
    /// </summary>
    void ApplyTheme(ThemePreferenceMode preference);
}
