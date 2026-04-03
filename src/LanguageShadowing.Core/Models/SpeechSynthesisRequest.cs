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
/// Describes a text-to-speech request before it is translated into platform-specific audio.
/// </summary>
/// <param name="Text">The text to synthesize.</param>
/// <param name="Voice">The selected voice, or <see langword="null"/> to use the platform default.</param>
/// <param name="Rate">The desired speaking rate, normalized to the application's UI range.</param>
/// <param name="Pitch">The desired pitch multiplier.</param>
/// <param name="Volume">The desired output volume in the range 0-1.</param>
public sealed record SpeechSynthesisRequest(
    string Text,
    VoiceInfo? Voice,
    double Rate,
    double Pitch,
    double Volume);
