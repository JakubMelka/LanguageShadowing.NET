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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Graphics;

namespace LanguageShadowing.App;

/// <summary>
/// Application entry point responsible for creating the main window and keeping the runtime palette in sync.
/// </summary>
public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        ApplyThemePalette(ResolveActiveTheme());
        RequestedThemeChanged += OnRequestedThemeChanged;
    }

    /// <summary>
    /// Creates the main application window.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(mainPage);
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        ApplyThemePalette(ResolveActiveTheme());
    }

    private AppTheme ResolveActiveTheme()
    {
        return UserAppTheme == AppTheme.Unspecified ? RequestedTheme : UserAppTheme;
    }

    private void ApplyThemePalette(AppTheme theme)
    {
        var isDark = theme == AppTheme.Dark;
        Resources["PageBackground"] = Color.FromArgb(isDark ? "#0F1722" : "#F4F7FB");
        Resources["CardBackground"] = Color.FromArgb(isDark ? "#172232" : "#FFFFFF");
        Resources["SurfaceMuted"] = Color.FromArgb(isDark ? "#223248" : "#EEF2F8");
        Resources["SurfaceHover"] = Color.FromArgb(isDark ? "#2B3E57" : "#E2E8F2");
        Resources["SurfacePressed"] = Color.FromArgb(isDark ? "#334A69" : "#D7DFEC");
        Resources["BorderColor"] = Color.FromArgb(isDark ? "#30435E" : "#D6DEEA");
        Resources["BorderHoverColor"] = Color.FromArgb(isDark ? "#48617F" : "#C1CBDC");
        Resources["PrimaryText"] = Color.FromArgb(isDark ? "#E7EEF8" : "#1B2430");
        Resources["SecondaryText"] = Color.FromArgb(isDark ? "#A2B2C9" : "#667286");
        Resources["AccentColor"] = Color.FromArgb(isDark ? "#7CB3FF" : "#2D7FF9");
        Resources["AccentHoverColor"] = Color.FromArgb(isDark ? "#66A6FF" : "#1F6EE3");
        Resources["AccentPressedColor"] = Color.FromArgb(isDark ? "#4E92F2" : "#175FC7");
        Resources["AccentSoft"] = Color.FromArgb(isDark ? "#223D63" : "#DCE9FF");
        Resources["AccentSoftHover"] = Color.FromArgb(isDark ? "#2D4C77" : "#C8DCFF");
        Resources["AccentSoftPressed"] = Color.FromArgb(isDark ? "#355789" : "#B7D0FF");
    }
}

