using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using LanguageShadowing.Infrastructure.Playback;

namespace LanguageShadowing.Infrastructure.Synthesis;

public sealed class FallbackTextToSpeechService : ITextToSpeechService
{
    private readonly SegmentedSpeechPlanner _planner;
    private readonly WaveformFactory _waveformFactory;

    public FallbackTextToSpeechService(SegmentedSpeechPlanner planner, WaveformFactory waveformFactory)
    {
        _planner = planner;
        _waveformFactory = waveformFactory;
    }

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
