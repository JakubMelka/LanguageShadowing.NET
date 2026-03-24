using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using LanguageShadowing.Infrastructure.Playback;

namespace LanguageShadowing.Infrastructure.Synthesis;

public sealed class FallbackTextToSpeechService : ITextToSpeechService
{
    private readonly SegmentedSpeechPlanner _planner;

    public FallbackTextToSpeechService(SegmentedSpeechPlanner planner)
    {
        _planner = planner;
    }

    public Task<SpeechSynthesisResult> PrepareAsync(SpeechSynthesisRequest request, CancellationToken cancellationToken = default)
    {
        var segments = _planner.CreateSegments(request.Text, request.Rate);
        var duration = _planner.EstimateDuration(segments);
        var waveform = new WaveformFactory().CreateEstimated(segments);

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
