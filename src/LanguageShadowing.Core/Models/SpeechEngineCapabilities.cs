namespace LanguageShadowing.Core.Models;

public sealed record SpeechEngineCapabilities(
    bool SupportsSeek,
    bool SupportsPause,
    bool SupportsWaveform,
    bool SupportsStreamingRecognition,
    bool SupportsOfflineMode,
    bool SupportsVoiceSelection);
