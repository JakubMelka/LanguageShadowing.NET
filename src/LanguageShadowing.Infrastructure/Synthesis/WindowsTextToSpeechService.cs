// MIT License
//
// Copyright (c) 2026 Jakub Melka and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using LanguageShadowing.Infrastructure.Playback;
using Windows.Media.SpeechSynthesis;

namespace LanguageShadowing.Infrastructure.Synthesis;

/// <summary>
/// Windows implementation that uses <see cref="SpeechSynthesizer"/> to generate real audio payloads.
/// </summary>
public sealed class WindowsTextToSpeechService : ITextToSpeechService
{
    private readonly SegmentedSpeechPlanner _planner;
    private readonly WaveformFactory _waveformFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsTextToSpeechService"/> class.
    /// </summary>
    public WindowsTextToSpeechService(SegmentedSpeechPlanner planner, WaveformFactory waveformFactory)
    {
        _planner = planner;
        _waveformFactory = waveformFactory;
    }

    /// <inheritdoc />
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
        var hasWaveform = _waveformFactory.TryCreateFromWave(bytes, WaveformFactory.SampleCount, out var waveform, out var actualDuration);
        if (!hasWaveform)
        {
            // Some platform payloads are valid for playback but awkward to analyze. In that case the application still
            // prefers real audio for playback and falls back only for visualization metadata.
            waveform = _waveformFactory.CreateEstimated(segments, WaveformFactory.SampleCount);
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
