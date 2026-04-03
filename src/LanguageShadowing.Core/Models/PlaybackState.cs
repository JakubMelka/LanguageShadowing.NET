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

using LanguageShadowing.Core.Enums;

namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents a point-in-time playback snapshot emitted by an <see cref="Interfaces.IAudioPlaybackController"/>.
/// </summary>
/// <param name="Status">The coarse playback lifecycle state.</param>
/// <param name="Position">The current playback position.</param>
/// <param name="Duration">The known total duration.</param>
/// <param name="CanSeek">Indicates whether the current payload supports seeking.</param>
/// <param name="IsBusy">Indicates whether the underlying controller is in a transient busy state.</param>
/// <param name="Message">An optional human-readable status message suitable for diagnostics.</param>
public sealed record PlaybackState(
    PlaybackStatus Status,
    TimeSpan Position,
    TimeSpan Duration,
    bool CanSeek,
    bool IsBusy,
    string? Message = null)
{
    /// <summary>
    /// Gets a reusable idle snapshot.
    /// </summary>
    public static PlaybackState Idle { get; } = new(PlaybackStatus.Idle, TimeSpan.Zero, TimeSpan.Zero, false, false);
}


