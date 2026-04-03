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
