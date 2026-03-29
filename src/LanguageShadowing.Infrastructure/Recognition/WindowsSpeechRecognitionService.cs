#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;
using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Microsoft.Maui.ApplicationModel;
using Windows.Media.SpeechRecognition;

namespace LanguageShadowing.Infrastructure.Recognition;

/// <summary>
/// Windows speech-recognition service backed by <see cref="SpeechRecognizer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class converts a native, callback-driven API into the application's simpler model built from tasks, status
/// snapshots, and transcript update events.
/// </para>
/// <para>
/// Three different kinds of asynchrony meet here:
/// </para>
/// <list type="bullet">
/// <item><description>explicit method calls such as <see cref="StartAsync"/>, <see cref="StopAsync"/>, and <see cref="ResetAsync"/>,</description></item>
/// <item><description>native callbacks that arrive whenever the recognizer has a hypothesis, final result, or completion event, and</description></item>
/// <item><description>cancellation requests from the view model that invalidate an older start attempt.</description></item>
/// </list>
/// <para>
/// The extra completion-control fields (<c>_suppressCompletedEvent</c> and <c>_completionMessageOverride</c>) exist because
/// native completion callbacks may arrive after the application has already decided that the session should be treated as
/// stopped or reset. Without that coordination the UI would occasionally jump backward into an outdated status.
/// </para>
/// </remarks>
public sealed class WindowsSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly StringBuilder _committedText = new();
    private SpeechRecognizer? _recognizer;
    private string _hypothesis = string.Empty;
    private bool _suppressCompletedEvent;
    private string? _completionMessageOverride;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public RecognitionStatus CurrentStatus { get; private set; } = RecognitionStatus.Idle;

    /// <inheritdoc />
    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
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
            // Cancellation here means that a newer user action has already replaced this start attempt. The service does
            // not publish a special cancelled state because the caller is already in the middle of driving a newer state.
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

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
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
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
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
            // Reset is best-effort. Even if Windows reports a recognition-specific failure here, the application still
            // wants to clear its local transcript and present a clean idle state to the user.
        }
        catch (Exception)
        {
            // Same reasoning as above: local cleanup is more important than surfacing a reset-time exception.
        }

        _committedText.Clear();
        _hypothesis = string.Empty;
        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        PublishState(RecognitionStatus.Idle, "Transcript cleared.");
    }

    /// <inheritdoc />
    public Task ClearTranscriptAsync(CancellationToken cancellationToken = default)
    {
        _committedText.Clear();
        _hypothesis = string.Empty;
        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_recognizer is not null)
        {
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
                // Disposal should never throw into app shutdown code.
            }

            _recognizer.Dispose();
            _recognizer = null;
        }
    }

    /// <summary>
    /// Lazily creates and configures the native recognizer instance.
    /// </summary>
    /// <remarks>
    /// The recognizer is intentionally initialized only once and then reused across sessions. That keeps the startup
    /// overhead concentrated in the first use and avoids repeated native allocation churn.
    /// </remarks>
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

    /// <summary>
    /// Handles partial recognition hypotheses.
    /// </summary>
    private void OnHypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
    {
        _hypothesis = args.Hypothesis.Text?.Trim() ?? string.Empty;
        PublishRecognition(_hypothesis, isFinal: false, confidence: null);
    }

    /// <summary>
    /// Handles final recognition results.
    /// </summary>
    /// <remarks>
    /// Final results are appended to <c>_committedText</c>, while the transient hypothesis buffer is cleared. This split
    /// lets the application display a "stable transcript plus current partial phrase" view without losing intermediate text.
    /// </remarks>
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

    /// <summary>
    /// Handles native recognition-session completion.
    /// </summary>
    /// <remarks>
    /// Completion is the most delicate callback in the type. By the time Windows raises it, the application may already
    /// have called Stop or Reset and moved on. The suppression/override flags ensure that late completion does not undo
    /// a newer state transition decided by the view model.
    /// </remarks>
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

    /// <summary>
    /// Publishes a recognition update composed from committed text and the current transient hypothesis.
    /// </summary>
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


