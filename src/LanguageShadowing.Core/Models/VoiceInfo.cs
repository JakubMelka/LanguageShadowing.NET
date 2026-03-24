namespace LanguageShadowing.Core.Models;

public sealed record VoiceInfo(
    string Id,
    string DisplayName,
    string Locale,
    string? Gender = null,
    bool IsDefault = false);
