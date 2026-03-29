using LanguageShadowing.Application.Analysis;
using LanguageShadowing.Application.ViewModels;
using LanguageShadowing.Infrastructure.DependencyInjection;

namespace LanguageShadowing.App;

/// <summary>
/// Creates and configures the MAUI host for the application.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Builds the MAUI application container.
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddLanguageShadowingInfrastructure();
        builder.Services.AddSingleton<IShadowingAnalyzer, ShadowingAnalyzer>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
