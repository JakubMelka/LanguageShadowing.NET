namespace LanguageShadowing.Core.Models;

public sealed record WaveformData(
    IReadOnlyList<float> Samples,
    bool IsEstimated)
{
    public static WaveformData Empty { get; } = new(Array.Empty<float>(), true);
}
