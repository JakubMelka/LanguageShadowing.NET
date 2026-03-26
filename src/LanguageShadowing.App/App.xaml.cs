using LanguageShadowing.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageShadowing.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(new Microsoft.Maui.Controls.NavigationPage(mainPage));
    }
}
