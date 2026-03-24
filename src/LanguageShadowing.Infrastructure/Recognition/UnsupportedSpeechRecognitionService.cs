using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Infrastructure.Recognition;

public sealed class UnsupportedSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly string _message;

    public UnsupportedSpeechRecognitionService(string message)
    {
        _message = message;
        CurrentStatus = RecognitionStatus.Unsupported;
    }

    public bool IsAvailable => false;

    public RecognitionStatus CurrentStatus { get; private set; }

    public event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    public event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        PublishState(RecognitionStatus.Unsupported, _message);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        PublishState(RecognitionStatus.Unsupported, _message);
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        RecognitionUpdated?.Invoke(this, new RecognitionUpdatedEventArgs(new RecognitionUpdate(string.Empty, string.Empty, true)));
        PublishState(RecognitionStatus.Unsupported, _message);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private void PublishState(RecognitionStatus status, string message)
    {
        CurrentStatus = status;
        StateChanged?.Invoke(this, new RecognitionStateChangedEventArgs(status, message));
    }
}

