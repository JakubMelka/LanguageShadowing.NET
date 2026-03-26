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
    private readonly StringBuilder _committedText = new();
    private SpeechRecognizer? _recognizer;
    private string _hypothesis = string.Empty;

    public bool IsAvailable => true;

    public RecognitionStatus CurrentStatus { get; private set; } = RecognitionStatus.Idle;

    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    public event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            PublishState(RecognitionStatus.Starting, "Requesting microphone access and initializing speech recognition...");
            var permission = await Permissions.RequestAsync<Permissions.Microphone>();
            if (permission != PermissionStatus.Granted)
            {
                PublishState(RecognitionStatus.Error, "Microphone access was denied.");
                return;
            }

            await EnsureRecognizerAsync().ConfigureAwait(false);
            if (_recognizer is null)
            {
                return;
            }

            if (_recognizer.State == SpeechRecognizerState.Idle)
            {
                await _recognizer.ContinuousRecognitionSession.StartAsync().AsTask(cancellationToken).ConfigureAwait(false);
                PublishState(RecognitionStatus.Listening, "Microphone is listening.");
            }
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
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_recognizer is not null && _recognizer.State != SpeechRecognizerState.Idle)
            {
                PublishState(RecognitionStatus.Stopping, "Stopping recognition...");
                await _recognizer.ContinuousRecognitionSession.StopAsync().AsTask(cancellationToken).ConfigureAwait(false);
            }

            PublishState(RecognitionStatus.Completed, "Recognition stopped.");
        }
        catch (COMException ex)
        {
            PublishState(RecognitionStatus.Error, MapComException(ex));
        }
        catch (Exception ex)
        {
            PublishState(RecognitionStatus.Error, $"Could not stop speech recognition: {ex.Message}");
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_recognizer is not null && _recognizer.State != SpeechRecognizerState.Idle)
            {
                await _recognizer.ContinuousRecognitionSession.CancelAsync().AsTask(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (COMException)
        {
            // Ignore reset-time recognition COM failures and continue clearing local state.
        }
        catch (Exception)
        {
            // Ignore reset-time failures and keep the app responsive.
        }

        _committedText.Clear();
        _hypothesis = string.Empty;
        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        PublishState(RecognitionStatus.Idle, "Transcript cleared.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_recognizer is not null)
        {
            try
            {
                if (_recognizer.State != SpeechRecognizerState.Idle)
                {
                    await _recognizer.ContinuousRecognitionSession.CancelAsync();
                }
            }
            catch (Exception)
            {
                // Swallow disposal-time failures.
            }

            _recognizer.Dispose();
            _recognizer = null;
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
        PublishState(RecognitionStatus.Completed, args.Status == SpeechRecognitionResultStatus.Success
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
