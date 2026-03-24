namespace LanguageShadowing.Core.Models;

public sealed record SpeechSynthesisResult(
    SpeechSynthesisRequest Request,
    IReadOnlyList<SpeechSegment> Segments,
    TimeSpan Duration,
    WaveformData Waveform,
    byte[]? AudioBytes,
    string? AudioContentType,
    bool IsEstimated)
{
    public bool HasAudioPayload => AudioBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(AudioContentType);
}
