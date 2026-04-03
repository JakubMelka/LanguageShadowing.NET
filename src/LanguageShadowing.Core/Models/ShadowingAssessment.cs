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
/// Represents the text comparison result between the source phrase and the recognized transcript.
/// </summary>
/// <param name="Score">A normalized score in the range 0-100, or <see langword="null"/> when unavailable.</param>
/// <param name="Summary">A compact explanation suitable for the UI.</param>
/// <param name="MissingWords">Words found in the source text but missing from the recognized transcript.</param>
/// <param name="ExtraWords">Words present in the recognized transcript but not in the source text.</param>
public sealed record ShadowingAssessment(
    int? Score,
    string Summary,
    IReadOnlyList<string> MissingWords,
    IReadOnlyList<string> ExtraWords)
{
    /// <summary>
    /// Gets a reusable placeholder result used when scoring cannot be produced.
    /// </summary>
    public static ShadowingAssessment Unsupported { get; } =
        new(null, "Score is unavailable.", Array.Empty<string>(), Array.Empty<string>());
}
