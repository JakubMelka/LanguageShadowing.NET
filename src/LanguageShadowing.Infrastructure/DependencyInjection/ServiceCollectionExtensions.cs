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
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using LanguageShadowing.Infrastructure.Engines;
using LanguageShadowing.Infrastructure.Playback;
using LanguageShadowing.Infrastructure.Recognition;
using LanguageShadowing.Infrastructure.Settings;
using LanguageShadowing.Infrastructure.Synthesis;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageShadowing.Infrastructure.DependencyInjection;

/// <summary>
/// Registers the application's infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the platform-specific infrastructure services required by the shadowing application.
    /// </summary>
    public static IServiceCollection AddLanguageShadowingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppDispatcher, MauiAppDispatcher>();
        services.AddSingleton<IAppThemeService, MauiAppThemeService>();
        services.AddSingleton<ISettingsLauncher, MauiSettingsLauncher>();
        services.AddSingleton<IShadowingSettingsStore, PreferencesSettingsStore>();
        services.AddSingleton<SegmentedSpeechPlanner>();
        services.AddSingleton<WaveformFactory>();
        services.AddSingleton<IVoiceCatalogService, BuiltInVoiceCatalogService>();
        services.AddSingleton<ITextToSpeechService>(sp => CreateTextToSpeech(sp));
        services.AddSingleton<IAudioPlaybackController>(sp => CreatePlaybackController(sp));
        services.AddSingleton<ISpeechRecognitionService>(_ => CreateRecognitionService());
        services.AddSingleton<ISpeechEngine>(sp => new BuiltInSpeechEngine(
            name: OperatingSystem.IsWindows() ? "Built-in Windows Speech" : "Built-in System Speech",
            capabilities: CreateCapabilities(),
            voiceCatalog: sp.GetRequiredService<IVoiceCatalogService>(),
            textToSpeech: sp.GetRequiredService<ITextToSpeechService>(),
            recognition: sp.GetRequiredService<ISpeechRecognitionService>(),
            playback: sp.GetRequiredService<IAudioPlaybackController>()));

        return services;
    }

    private static SpeechEngineCapabilities CreateCapabilities()
    {
#if WINDOWS
        return new SpeechEngineCapabilities(
            SupportsSeek: true,
            SupportsPause: true,
            SupportsWaveform: true,
            SupportsStreamingRecognition: true,
            SupportsOfflineMode: true,
            SupportsVoiceSelection: true);
#else
        return new SpeechEngineCapabilities(
            SupportsSeek: true,
            SupportsPause: true,
            SupportsWaveform: true,
            SupportsStreamingRecognition: false,
            SupportsOfflineMode: false,
            SupportsVoiceSelection: false);
#endif
    }

    private static ITextToSpeechService CreateTextToSpeech(IServiceProvider serviceProvider)
    {
#if WINDOWS
        return new WindowsTextToSpeechService(
            serviceProvider.GetRequiredService<SegmentedSpeechPlanner>(),
            serviceProvider.GetRequiredService<WaveformFactory>());
#else
        return new FallbackTextToSpeechService(
            serviceProvider.GetRequiredService<SegmentedSpeechPlanner>(),
            serviceProvider.GetRequiredService<WaveformFactory>());
#endif
    }

    private static IAudioPlaybackController CreatePlaybackController(IServiceProvider serviceProvider)
    {
#if WINDOWS
        return new WindowsAudioPlaybackController();
#else
        return new FallbackSpeechPlaybackController();
#endif
    }

    private static ISpeechRecognitionService CreateRecognitionService()
    {
#if WINDOWS
        return new WindowsSpeechRecognitionService();
#else
        return new UnsupportedSpeechRecognitionService("Speech recognition is implemented only for Windows in this MVP.");
#endif
    }
}
