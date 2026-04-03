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

#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;
using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Microsoft.Maui.ApplicationModel;
using Windows.Media.SpeechRecognition;

namespace LanguageShadowing.Infrastructure.Recognition;

public sealed class WindowsSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly StringBuilder _committedText = new();
    private SpeechRecognizer? _recognizer;
    private string _hypothesis = string.Empty;
    private bool _suppressCompletedEvent;
    private string? _completionMessageOverride;

    public bool IsAvailable => true;

    public RecognitionStatus CurrentStatus { get; private set; } = RecognitionStatus.Idle;

    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    public event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _suppressCompletedEvent = false;
            _completionMessageOverride = null;
            PublishState(RecognitionStatus.Starting, "Requesting microphone access and initializing speech recognition...");
            var permission = await Permissions.RequestAsync<Permissions.Microphone>();
            cancellationToken.ThrowIfCancellationRequested();

            if (permission != PermissionStatus.Granted)
            {
                PublishState(RecognitionStatus.Error, "Microphone access was denied.");
                return;
            }

            await EnsureRecognizerAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (_recognizer is null)
            {
                return;
            }

            if (_recognizer.State == SpeechRecognizerState.Idle)
            {
                await _recognizer.ContinuousRecognitionSession.StartAsync().AsTask(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                PublishState(RecognitionStatus.Listening, "Microphone is listening.");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (COMException ex)
        {
            PublishState(RecognitionStatus.Error, MapComException(ex));
        }
        catch (UnauthorizedAccessException)
        {
            PublishState(RecognitionStatus.Error, "The microphone is unavailable or access was denied by the system.");
        }
        catch (Exception ex)
        {
            PublishState(RecognitionStatus.Error, $"Could not start speech recognition: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_recognizer is not null && _recognizer.State != SpeechRecognizerState.Idle)
            {
                _suppressCompletedEvent = false;
                _completionMessageOverride = "Recognition stopped.";
                PublishState(RecognitionStatus.Stopping, "Stopping recognition...");
                await _recognizer.ContinuousRecognitionSession.StopAsync().AsTask(cancellationToken).ConfigureAwait(false);
                return;
            }

            _suppressCompletedEvent = false;
            _completionMessageOverride = null;
            PublishState(RecognitionStatus.Completed, "Recognition stopped.");
        }
        catch (COMException ex)
        {
            _completionMessageOverride = null;
            PublishState(RecognitionStatus.Error, MapComException(ex));
        }
        catch (Exception ex)
        {
            _completionMessageOverride = null;
            PublishState(RecognitionStatus.Error, $"Could not stop speech recognition: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                _completionMessageOverride = null;
                _suppressCompletedEvent = _recognizer is not null && _recognizer.State != SpeechRecognizerState.Idle;

                if (_suppressCompletedEvent)
                {
                    await _recognizer!.ContinuousRecognitionSession.CancelAsync().AsTask(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (COMException)
            {
            }
            catch (Exception)
            {
            }

            _committedText.Clear();
            _hypothesis = string.Empty;
            RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
            PublishState(RecognitionStatus.Idle, "Transcript cleared.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearTranscriptAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _committedText.Clear();
            _hypothesis = string.Empty;
            RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_recognizer is null)
            {
                return;
            }

            try
            {
                _completionMessageOverride = null;
                _suppressCompletedEvent = _recognizer.State != SpeechRecognizerState.Idle;

                if (_suppressCompletedEvent)
                {
                    await _recognizer.ContinuousRecognitionSession.CancelAsync();
                }
            }
            catch (Exception)
            {
            }

            _recognizer.Dispose();
            _recognizer = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureRecognizerAsync()
    {
        if (_recognizer is not null)
        {
            return;
        }

        try
        {
            _recognizer = new SpeechRecognizer();
            _recognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(8);
            _recognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(1.2);
            _recognizer.HypothesisGenerated += OnHypothesisGenerated;
            _recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
            _recognizer.ContinuousRecognitionSession.Completed += OnCompleted;
            _recognizer.Constraints.Add(new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation"));

            var compilation = await _recognizer.CompileConstraintsAsync();
            if (compilation.Status != SpeechRecognitionResultStatus.Success)
            {
                PublishState(RecognitionStatus.Error, $"Could not prepare speech recognition: {compilation.Status}.");
                _recognizer.Dispose();
                _recognizer = null;
            }
        }
        catch
        {
            if (_recognizer is not null)
            {
                _recognizer.Dispose();
                _recognizer = null;
            }

            throw;
        }
    }

    private void OnHypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
    {
        _hypothesis = args.Hypothesis.Text?.Trim() ?? string.Empty;
        PublishRecognition(_hypothesis, isFinal: false, confidence: null);
    }

    private void OnResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
    {
        var text = args.Result.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (_committedText.Length > 0)
        {
            _committedText.Append(' ');
        }

        _committedText.Append(text);
        _hypothesis = string.Empty;
        PublishRecognition(text, isFinal: true, confidence: (double)args.Result.RawConfidence);
    }

    private void OnCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
    {
        if (_suppressCompletedEvent)
        {
            _suppressCompletedEvent = false;
            _completionMessageOverride = null;
            return;
        }

        var completionMessage = _completionMessageOverride;
        _completionMessageOverride = null;
        PublishState(RecognitionStatus.Completed, !string.IsNullOrWhiteSpace(completionMessage)
            ? completionMessage
            : args.Status == SpeechRecognitionResultStatus.Success
                ? "Recognition completed."
                : $"Recognition ended with status {args.Status}.");
    }

    private void PublishRecognition(string latestText, bool isFinal, double? confidence)
    {
        var fullText = string.IsNullOrWhiteSpace(_hypothesis)
            ? _committedText.ToString()
            : string.Join(' ', new[] { _committedText.ToString(), _hypothesis }.Where(part => !string.IsNullOrWhiteSpace(part)));

        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(fullText, latestText, isFinal, confidence)));
    }

    private void PublishState(RecognitionStatus status, string message)
    {
        CurrentStatus = status;
        StateChanged?.Invoke(this, new RecognitionStateChangedEventArgs(status, message));
    }

    private static string MapComException(COMException exception)
    {
        var message = exception.Message ?? string.Empty;
        if (message.Contains("speech privacy policy", StringComparison.OrdinalIgnoreCase))
        {
            return "Speech recognition is disabled in Windows privacy settings. Enable online speech recognition in Windows Settings to use the microphone.";
        }

        return $"Speech recognition is unavailable: {message}";
    }
}
#endif
