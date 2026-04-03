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
using Microsoft.Maui.Storage;

namespace LanguageShadowing.Infrastructure.Settings;

/// <summary>
/// Stores user settings in MAUI preferences.
/// </summary>
public sealed class PreferencesSettingsStore : IShadowingSettingsStore
{
    private const string VoiceIdKey = "preferred_voice_id";
    private const string SpeechRateKey = "speech_rate";
    private const string SpeechPitchKey = "speech_pitch";
    private const string SpeechVolumeKey = "speech_volume";
    private const string DictationEnabledKey = "dictation_enabled";
    private const string ThemePreferenceKey = "theme_preference";

    /// <summary>
    /// Loads the persisted settings snapshot.
    /// </summary>
    public Task<ShadowingSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var voiceId = Preferences.Default.Get<string?>(VoiceIdKey, null);
        var speechRate = Preferences.Default.Get(SpeechRateKey, 1.0);
        var speechPitch = Preferences.Default.Get(SpeechPitchKey, 1.0);
        var speechVolume = Preferences.Default.Get(SpeechVolumeKey, 1.0);
        var isDictationEnabled = Preferences.Default.Get(DictationEnabledKey, false);
        var themePreference = Preferences.Default.Get(ThemePreferenceKey, nameof(ThemePreferenceMode.System));
        var parsedTheme = Enum.TryParse<ThemePreferenceMode>(themePreference, out var value)
            ? value
            : ThemePreferenceMode.System;

        return Task.FromResult(new ShadowingSettings(voiceId, speechRate, speechPitch, speechVolume, isDictationEnabled, parsedTheme));
    }

    /// <summary>
    /// Saves the supplied settings snapshot.
    /// </summary>
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
        Preferences.Default.Set(DictationEnabledKey, settings.IsDictationEnabled);
        Preferences.Default.Set(ThemePreferenceKey, settings.ThemePreference.ToString());
        return Task.CompletedTask;
    }
}


