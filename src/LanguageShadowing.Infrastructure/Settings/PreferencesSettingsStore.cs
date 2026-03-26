using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.Storage;

namespace LanguageShadowing.Infrastructure.Settings;

public sealed class PreferencesSettingsStore : IShadowingSettingsStore
{
    private const string VoiceIdKey = "preferred_voice_id";
    private const string SpeechRateKey = "speech_rate";
    private const string SpeechPitchKey = "speech_pitch";
    private const string SpeechVolumeKey = "speech_volume";
    private const string ThemePreferenceKey = "theme_preference";

    public Task<ShadowingSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var voiceId = Preferences.Default.Get<string?>(VoiceIdKey, null);
        var speechRate = Preferences.Default.Get(SpeechRateKey, 1.0);
        var speechPitch = Preferences.Default.Get(SpeechPitchKey, 1.0);
        var speechVolume = Preferences.Default.Get(SpeechVolumeKey, 1.0);
        var themePreference = Preferences.Default.Get(ThemePreferenceKey, nameof(ThemePreferenceMode.System));
        var parsedTheme = Enum.TryParse<ThemePreferenceMode>(themePreference, out var value)
            ? value
            : ThemePreferenceMode.System;

        return Task.FromResult(new ShadowingSettings(voiceId, speechRate, speechPitch, speechVolume, parsedTheme));
    }

    public Task SaveAsync(ShadowingSettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.PreferredVoiceId))
        {
            Preferences.Default.Remove(VoiceIdKey);
        }
        else
        {
            Preferences.Default.Set(VoiceIdKey, settings.PreferredVoiceId);
        }

        Preferences.Default.Set(SpeechRateKey, settings.SpeechRate);
        Preferences.Default.Set(SpeechPitchKey, settings.SpeechPitch);
        Preferences.Default.Set(SpeechVolumeKey, settings.SpeechVolume);
        Preferences.Default.Set(ThemePreferenceKey, settings.ThemePreference.ToString());
        return Task.CompletedTask;
    }
}
