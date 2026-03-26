namespace LanguageShadowing.Application.Abstractions;

public sealed record ShadowingSettings(
    string? PreferredVoiceId,
    double SpeechRate,
    double SpeechPitch,
    double SpeechVolume,
    ThemePreferenceMode ThemePreference);
