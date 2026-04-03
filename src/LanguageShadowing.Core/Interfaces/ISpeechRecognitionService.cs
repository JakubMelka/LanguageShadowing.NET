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
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

/// <summary>
/// Represents a streaming speech recognition engine used by the shadowing workflow.
/// </summary>
/// <remarks>
/// The service exposes two different asynchronous event streams:
/// one for coarse status transitions (<see cref="StateChanged"/>) and one for transcript updates
/// (<see cref="RecognitionUpdated"/>). The view model consumes both streams independently because a recognizer
/// can keep producing transcript fragments while its lifecycle state remains unchanged.
/// </remarks>
public interface ISpeechRecognitionService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether speech recognition is available on the current platform.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the last lifecycle status published by the service.
    /// </summary>
    RecognitionStatus CurrentStatus { get; }

    /// <summary>
    /// Raised whenever the recognizer lifecycle changes state.
    /// </summary>
    event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Raised whenever the recognizer produces a partial or final transcript update.
    /// </summary>
    event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    /// <summary>
    /// Starts recognition.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a graceful stop of recognition.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears local transcript state and returns the recognizer to an idle baseline.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the accumulated transcript without forcing the recognizer to stop listening.
    /// </summary>
    Task ClearTranscriptAsync(CancellationToken cancellationToken = default);
}


