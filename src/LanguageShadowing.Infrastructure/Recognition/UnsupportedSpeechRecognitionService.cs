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


