// MIT License
//
// Copyright (c) 2026 Jakub Melka and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LanguageShadowing.Infrastructure.Settings;

/// <summary>
/// Applies the application's theme preference and updates the shared resource palette.
/// </summary>
public sealed class MauiAppThemeService : IAppThemeService
{
    /// <summary>
    /// Applies the requested theme immediately.
    /// </summary>
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

    /// <summary>
    /// Updates all resource entries that depend on the active theme.
    /// </summary>
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
