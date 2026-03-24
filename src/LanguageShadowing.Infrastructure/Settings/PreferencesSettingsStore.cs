using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.Storage;

namespace LanguageShadowing.Infrastructure.Settings;

public sealed class PreferencesSettingsStore : IShadowingSettingsStore
{
    private const string VoiceIdKey = "preferred_voice_id";
    private const string SpeechRateKey = "speech_rate";

    public Task<ShadowingSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var voiceId = Preferences.Default.Get<string?>(VoiceIdKey, null);
        var speechRate = Preferences.Default.Get(SpeechRateKey, 1.0);
        return Task.FromResult(new ShadowingSettings(voiceId, speechRate));
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
        return Task.CompletedTask;
    }
}
