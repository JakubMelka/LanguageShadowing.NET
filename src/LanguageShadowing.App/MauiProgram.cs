using LanguageShadowing.Application.Analysis;
using LanguageShadowing.Application.ViewModels;
using LanguageShadowing.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LanguageShadowing.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddLanguageShadowingInfrastructure();
        builder.Services.AddSingleton<IShadowingAnalyzer, ShadowingAnalyzer>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

