using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

public interface IAudioPlaybackController : IAsyncDisposable
{
    PlaybackState CurrentState { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default);

    Task PlayAsync(CancellationToken cancellationToken = default);

    Task PauseAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);

    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);
}
