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
/// Describes the recognizer lifecycle exposed to the UI.
/// </summary>
public enum RecognitionStatus
{
    /// <summary>The current platform does not implement speech recognition.</summary>
    Unsupported,

    /// <summary>The recognizer is idle and not listening.</summary>
    Idle,

    /// <summary>The recognizer is acquiring permissions or native resources.</summary>
    Starting,

    /// <summary>The recognizer is actively listening and may emit transcript updates.</summary>
    Listening,

    /// <summary>A graceful stop has been requested and is still completing.</summary>
    Stopping,

    /// <summary>The recognizer has completed a recognition session.</summary>
    Completed,

    /// <summary>The recognizer failed and needs user action or restart.</summary>
    Error
}
