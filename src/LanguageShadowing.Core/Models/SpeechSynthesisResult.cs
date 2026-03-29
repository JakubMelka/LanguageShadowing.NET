namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents the prepared output of a text-to-speech request.
/// </summary>
/// <remarks>
/// The result always contains timing metadata and waveform data. On platforms with native synthesis support it may
/// also carry raw audio bytes that can be fed directly to a playback engine. Fallback implementations can omit the
/// raw audio payload and still provide enough metadata for estimated playback and waveform rendering.
/// </remarks>
/// <param name="Request">The original synthesis request.</param>
/// <param name="Segments">The planned speech segments used by fallback playback and score alignment.</param>
/// <param name="Duration">The total estimated or measured duration.</param>
/// <param name="Waveform">Waveform data used by the custom waveform control.</param>
/// <param name="AudioBytes">Optional raw audio bytes encoded in the platform format.</param>
/// <param name="AudioContentType">Optional MIME type describing <paramref name="AudioBytes"/>.</param>
/// <param name="IsEstimated">Indicates whether duration or waveform information had to be estimated.</param>
public sealed record SpeechSynthesisResult(
    SpeechSynthesisRequest Request,
    IReadOnlyList<SpeechSegment> Segments,
    TimeSpan Duration,
    WaveformData Waveform,
    byte[]? AudioBytes,
    string? AudioContentType,
    bool IsEstimated)
{
    /// <summary>
    /// Gets a value indicating whether the result contains a playable audio payload.
    /// </summary>
    public bool HasAudioPayload => AudioBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(AudioContentType);
}
