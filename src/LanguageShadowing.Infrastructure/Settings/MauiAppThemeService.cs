using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.Controls;

namespace LanguageShadowing.Infrastructure.Settings;

public sealed class MauiAppThemeService : IAppThemeService
{
    public void ApplyTheme(ThemePreferenceMode preference)
    {
        if (Microsoft.Maui.Controls.Application.Current is null)
        {
            return;
        }

        Microsoft.Maui.Controls.Application.Current.UserAppTheme = preference switch
        {
            ThemePreferenceMode.Light => AppTheme.Light,
            ThemePreferenceMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
