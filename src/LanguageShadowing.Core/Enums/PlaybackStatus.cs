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

namespace LanguageShadowing.Core.Enums;

/// <summary>
/// Describes the coarse playback lifecycle used by the view model and UI.
/// </summary>
public enum PlaybackStatus
{
    /// <summary>No audio is loaded.</summary>
    Idle,

    /// <summary>Audio is prepared and can be started.</summary>
    Ready,

    /// <summary>Playback is currently advancing.</summary>
    Playing,

    /// <summary>Playback is paused but can be resumed.</summary>
    Paused,

    /// <summary>Playback was explicitly stopped and rewound.</summary>
    Stopped,

    /// <summary>Playback reached the natural end of the media.</summary>
    Completed,

    /// <summary>An unrecoverable playback error occurred.</summary>
    Error
}
