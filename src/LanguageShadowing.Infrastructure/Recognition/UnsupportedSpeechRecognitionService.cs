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
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Infrastructure.Recognition;

/// <summary>
/// Placeholder recognizer used on platforms where speech recognition is not implemented.
/// </summary>
public sealed class UnsupportedSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly string _message;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedSpeechRecognitionService"/> class.
    /// </summary>
    public UnsupportedSpeechRecognitionService(string message)
    {
        _message = message;
        CurrentStatus = RecognitionStatus.Unsupported;
    }

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public RecognitionStatus CurrentStatus { get; private set; }

    /// <inheritdoc />
    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        PublishState(RecognitionStatus.Unsupported, _message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        PublishState(RecognitionStatus.Unsupported, _message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        PublishState(RecognitionStatus.Unsupported, _message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearTranscriptAsync(CancellationToken cancellationToken = default)
    {
        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private void PublishState(RecognitionStatus status, string message)
    {
        CurrentStatus = status;
        StateChanged?.Invoke(this, new RecognitionStateChangedEventArgs(status, message));
    }
}


