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

using System.Text.RegularExpressions;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Infrastructure.Playback;

/// <summary>
/// Splits text into coarse speech segments and estimates their timing.
/// </summary>
public sealed class SegmentedSpeechPlanner
{
    private static readonly Regex SentenceSeparatorRegex = new("(?<=[\\.!?;:])\\s+|\\r?\\n+", RegexOptions.Compiled);

    /// <summary>
    /// Creates segments from the supplied text.
    /// </summary>
    public IReadOnlyList<SpeechSegment> CreateSegments(string text, double speechRate)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<SpeechSegment>();
        }

        var fragments = SentenceSeparatorRegex.Split(normalized)
            .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
            .Select(fragment => fragment.Trim())
            .ToList();

        if (fragments.Count == 0)
        {
            fragments.Add(normalized);
        }

        var segments = new List<SpeechSegment>(fragments.Count);
        var offset = 0;
        var start = TimeSpan.Zero;
        foreach (var fragment in fragments)
        {
            var safeRate = Math.Clamp(speechRate, 0.5, 2.0);
            var wordCount = Math.Max(1, fragment.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            var wordsPerMinute = 145d * safeRate;
            var durationSeconds = Math.Max(0.55, wordCount / wordsPerMinute * 60d + 0.12);
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var textIndex = normalized.IndexOf(fragment, offset, StringComparison.Ordinal);
            if (textIndex < 0)
            {
                textIndex = offset;
            }

            segments.Add(new SpeechSegment(fragment, start, duration, textIndex, fragment.Length));
            offset = textIndex + fragment.Length;
            start += duration;
        }

        return segments;
    }

    /// <summary>
    /// Estimates the total duration from the final segment end.
    /// </summary>
    public TimeSpan EstimateDuration(IReadOnlyList<SpeechSegment> segments)
    {
        if (segments.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var last = segments[^1];
        return last.Start + last.Duration;
    }
}
