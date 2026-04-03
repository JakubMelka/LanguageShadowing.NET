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
