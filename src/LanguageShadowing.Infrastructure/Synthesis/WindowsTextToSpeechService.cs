#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using LanguageShadowing.Infrastructure.Playback;
using Windows.Media.SpeechSynthesis;

namespace LanguageShadowing.Infrastructure.Synthesis;

public sealed class WindowsTextToSpeechService : ITextToSpeechService
{
    private readonly SegmentedSpeechPlanner _planner;
    private readonly WaveformFactory _waveformFactory;

    public WindowsTextToSpeechService(SegmentedSpeechPlanner planner, WaveformFactory waveformFactory)
    {
        _planner = planner;
        _waveformFactory = waveformFactory;
    }

    public async Task<SpeechSynthesisResult> PrepareAsync(SpeechSynthesisRequest request, CancellationToken cancellationToken = default)
    {
        var segments = _planner.CreateSegments(request.Text, request.Rate);
        var estimatedDuration = _planner.EstimateDuration(segments);

        using var synthesizer = new SpeechSynthesizer();
        if (!string.IsNullOrWhiteSpace(request.Voice?.Id))
        {
            var selectedVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(voice => voice.Id == request.Voice.Id);
            if (selectedVoice is not null)
            {
                synthesizer.Voice = selectedVoice;
            }
        }

        synthesizer.Options.SpeakingRate = Math.Clamp(request.Rate, 0.5, 2.0);
        synthesizer.Options.AudioPitch = Math.Clamp(request.Pitch, 0.0, 2.0);
        synthesizer.Options.AudioVolume = Math.Clamp(request.Volume, 0.0, 1.0);
        var stream = await synthesizer.SynthesizeTextToStreamAsync(request.Text);
        using var managedStream = stream.AsStreamForRead();
        using var buffer = new MemoryStream();
        await managedStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        var bytes = buffer.ToArray();
        var hasWaveform = _waveformFactory.TryCreateFromWave(bytes, 144, out var waveform, out var actualDuration);
        if (!hasWaveform)
        {
            waveform = _waveformFactory.CreateEstimated(segments);
            actualDuration = estimatedDuration;
        }

        return new SpeechSynthesisResult(
            request,
            segments,
            actualDuration,
            waveform,
            bytes,
            stream.ContentType,
            IsEstimated: !hasWaveform);
    }
}
#endif
