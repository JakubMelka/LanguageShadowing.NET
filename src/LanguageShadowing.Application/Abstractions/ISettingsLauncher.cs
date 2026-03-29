namespace LanguageShadowing.Application.Abstractions;

/// <summary>
/// Opens an operating-system settings page or external URI.
/// </summary>
public interface ISettingsLauncher
{
    /// <summary>
    /// Opens the specified URI.
    /// </summary>
    Task OpenAsync(string uri);
}
