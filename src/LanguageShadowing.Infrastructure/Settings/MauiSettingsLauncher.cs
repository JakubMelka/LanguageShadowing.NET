using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace LanguageShadowing.Infrastructure.Settings;

/// <summary>
/// Opens operating-system settings pages through MAUI's launcher abstraction.
/// </summary>
public sealed class MauiSettingsLauncher : ISettingsLauncher
{
    /// <summary>
    /// Opens the specified URI.
    /// </summary>
    public Task OpenAsync(string uri)
    {
        return Launcher.Default.OpenAsync(new Uri(uri));
    }
}
