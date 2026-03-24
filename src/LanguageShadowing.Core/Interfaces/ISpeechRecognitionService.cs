using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

public interface ISpeechRecognitionService : IAsyncDisposable
{
    bool IsAvailable { get; }

    RecognitionStatus CurrentStatus { get; }

    event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}
