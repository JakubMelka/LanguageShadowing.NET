namespace LanguageShadowing.Core.Models;

/// <summary>
/// Describes the capabilities of a concrete speech engine implementation.
/// </summary>
public sealed record SpeechEngineCapabilities(
    bool SupportsSeek,
    bool SupportsPause,
    bool SupportsWaveform,
    bool SupportsStreamingRecognition,
    bool SupportsOfflineMode,
    bool SupportsVoiceSelection);
