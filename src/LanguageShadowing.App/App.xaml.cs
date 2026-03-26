using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Graphics;

namespace LanguageShadowing.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        ApplyThemePalette(ResolveActiveTheme());
        RequestedThemeChanged += OnRequestedThemeChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(new Microsoft.Maui.Controls.NavigationPage(mainPage));
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
        Resources["BorderColor"] = Color.FromArgb(isDark ? "#30435E" : "#D6DEEA");
        Resources["PrimaryText"] = Color.FromArgb(isDark ? "#E7EEF8" : "#1B2430");
        Resources["SecondaryText"] = Color.FromArgb(isDark ? "#A2B2C9" : "#667286");
        Resources["AccentColor"] = Color.FromArgb(isDark ? "#7CB3FF" : "#2D7FF9");
        Resources["AccentSoft"] = Color.FromArgb(isDark ? "#223D63" : "#DCE9FF");
    }
}
