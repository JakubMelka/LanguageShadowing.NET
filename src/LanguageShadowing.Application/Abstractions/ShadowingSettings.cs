namespace LanguageShadowing.Application.Abstractions;

/// <summary>
/// Stores the user preferences required to restore the shadowing UI state between launches.
/// </summary>
public sealed record ShadowingSettings(
    string? PreferredVoiceId,
    double SpeechRate,
    double SpeechPitch,
    double SpeechVolume,
    bool IsDictationEnabled,
    ThemePreferenceMode ThemePreference);


