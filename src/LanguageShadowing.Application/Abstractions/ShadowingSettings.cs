namespace LanguageShadowing.Application.Abstractions;

public sealed record ShadowingSettings(
    string? PreferredVoiceId,
    double SpeechRate);
