using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LanguageShadowing.Infrastructure.Settings;

public sealed class MauiAppThemeService : IAppThemeService
{
    public void ApplyTheme(ThemePreferenceMode preference)
    {
        if (Microsoft.Maui.Controls.Application.Current is null)
        {
            return;
        }

        var app = Microsoft.Maui.Controls.Application.Current;
        var theme = preference switch
        {
            ThemePreferenceMode.Light => AppTheme.Light,
            ThemePreferenceMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        app.UserAppTheme = theme;
        ApplyThemePalette(app, theme == AppTheme.Unspecified ? app.RequestedTheme : theme);
    }

    private static void ApplyThemePalette(Microsoft.Maui.Controls.Application app, AppTheme theme)
    {
        var isDark = theme == AppTheme.Dark;
        app.Resources["PageBackground"] = Color.FromArgb(isDark ? "#0F1722" : "#F4F7FB");
        app.Resources["CardBackground"] = Color.FromArgb(isDark ? "#172232" : "#FFFFFF");
        app.Resources["SurfaceMuted"] = Color.FromArgb(isDark ? "#223248" : "#EEF2F8");
        app.Resources["SurfaceHover"] = Color.FromArgb(isDark ? "#2B3F58" : "#E2E8F2");
        app.Resources["BorderColor"] = Color.FromArgb(isDark ? "#30435E" : "#D6DEEA");
        app.Resources["BorderHoverColor"] = Color.FromArgb(isDark ? "#45607F" : "#C1CBDC");
        app.Resources["PrimaryText"] = Color.FromArgb(isDark ? "#E7EEF8" : "#1B2430");
        app.Resources["SecondaryText"] = Color.FromArgb(isDark ? "#A2B2C9" : "#667286");
        app.Resources["AccentColor"] = Color.FromArgb(isDark ? "#7CB3FF" : "#2D7FF9");
        app.Resources["AccentHoverColor"] = Color.FromArgb(isDark ? "#95C0FF" : "#1F6EE3");
        app.Resources["AccentSoft"] = Color.FromArgb(isDark ? "#223D63" : "#DCE9FF");
        app.Resources["AccentSoftHover"] = Color.FromArgb(isDark ? "#2B4D78" : "#C8DCFF");
    }
}
