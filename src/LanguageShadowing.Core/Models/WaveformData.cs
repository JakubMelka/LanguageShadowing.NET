namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents the waveform preview shown next to playback controls.
/// </summary>
/// <param name="Samples">Normalized amplitude samples in display order.</param>
/// <param name="IsEstimated">Indicates whether the waveform was derived heuristically rather than from decoded audio.</param>
public sealed record WaveformData(
    IReadOnlyList<float> Samples,
    bool IsEstimated)
{
    /// <summary>
    /// Gets a reusable empty waveform.
    /// </summary>
    public static WaveformData Empty { get; } = new(Array.Empty<float>(), true);
}
