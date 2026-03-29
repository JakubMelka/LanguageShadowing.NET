using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using LanguageShadowing.Infrastructure.Playback;

namespace LanguageShadowing.Infrastructure.Synthesis;

/// <summary>
/// Fallback synthesizer used when native audio generation is unavailable.
/// </summary>
/// <remarks>
/// This implementation never produces raw audio. Instead it returns estimated timing and waveform metadata so the rest
/// of the application can still render a believable preview and drive the fallback playback controller.
/// </remarks>
public sealed class FallbackTextToSpeechService : ITextToSpeechService
{
    private readonly SegmentedSpeechPlanner _planner;
    private readonly WaveformFactory _waveformFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackTextToSpeechService"/> class.
    /// </summary>
    public FallbackTextToSpeechService(SegmentedSpeechPlanner planner, WaveformFactory waveformFactory)
    {
        _planner = planner;
        _waveformFactory = waveformFactory;
    }

    /// <inheritdoc />
    public Task<SpeechSynthesisResult> PrepareAsync(SpeechSynthesisRequest request, CancellationToken cancellationToken = default)
    {
        var segments = _planner.CreateSegments(request.Text, request.Rate);
        var duration = _planner.EstimateDuration(segments);
        var waveform = _waveformFactory.CreateEstimated(segments, WaveformFactory.SampleCount);

        return Task.FromResult(new SpeechSynthesisResult(
            request,
            segments,
            duration,
            waveform,
            AudioBytes: null,
            AudioContentType: null,
            IsEstimated: true));
    }
}
