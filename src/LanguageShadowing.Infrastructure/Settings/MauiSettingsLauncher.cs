using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace LanguageShadowing.Infrastructure.Settings;

public sealed class MauiSettingsLauncher : ISettingsLauncher
{
    public Task OpenAsync(string uri)
    {
        return Launcher.Default.OpenAsync(new Uri(uri));
    }
}
