#if WINDOWS
using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace LanguageShadowing.Infrastructure.Playback;

/// <summary>
/// Windows playback controller backed by <see cref="MediaPlayer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the Windows-specific translation layer between native media playback and the application's neutral
/// <see cref="PlaybackState"/> model.
/// </para>
/// <para>
/// The rest of the application should never have to know about <see cref="MediaPlayer"/>, playback sessions, random
/// access streams, or native event timing. Instead, the controller exposes one current snapshot and one event that says
/// "here is the newest playback state I know about".
/// </para>
/// <para>
/// The class is intentionally event-driven. Windows playback position changes are discovered through
/// <see cref="MediaPlaybackSession.PositionChanged"/>, not by polling. That keeps the progress pipeline responsive while
/// avoiding an extra timer in application code.
/// </para>
/// </remarks>
public sealed class WindowsAudioPlaybackController : IAudioPlaybackController
{
    private readonly MediaPlayer _player;
    private InMemoryRandomAccessStream? _currentStream;
    private SpeechSynthesisResult? _loaded;
    private PlaybackState _state = PlaybackState.Idle;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsAudioPlaybackController"/> class and subscribes to native playback events.
    /// </summary>
    public WindowsAudioPlaybackController()
    {
        _player = new MediaPlayer();
        _player.MediaEnded += OnMediaEnded;
        _player.MediaFailed += OnMediaFailed;
        _player.PlaybackSession.PositionChanged += OnPositionChanged;
        _player.PlaybackSession.NaturalDurationChanged += OnNaturalDurationChanged;
    }

    /// <inheritdoc />
    public PlaybackState CurrentState => _state;

    /// <inheritdoc />
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default)
    {
        await DisposeStreamAsync().ConfigureAwait(false);
        _loaded = synthesisResult;

        if (!synthesisResult.HasAudioPayload)
        {
            Publish(new PlaybackState(PlaybackStatus.Error, TimeSpan.Zero, synthesisResult.Duration, false, false, "The Windows player did not receive audio data."));
            return;
        }

        _currentStream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(_currentStream))
        {
            writer.WriteBytes(synthesisResult.AudioBytes!);
            await writer.StoreAsync();
            writer.DetachStream();
        }

        _currentStream.Seek(0);
        _player.Source = MediaSource.CreateFromStream(_currentStream, synthesisResult.AudioContentType!);
        Publish(new PlaybackState(PlaybackStatus.Ready, TimeSpan.Zero, synthesisResult.Duration, true, false, "Audio prepared."));
    }

    /// <inheritdoc />
    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return Task.CompletedTask;
        }

        _player.Play();
        Publish(_state with
        {
            Status = PlaybackStatus.Playing,
            Position = _player.PlaybackSession.Position,
            Duration = _loaded.Duration,
            IsBusy = false,
            Message = "Playing synthesized speech."
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _player.Pause();
        Publish(_state with
        {
            Status = PlaybackStatus.Paused,
            Position = _player.PlaybackSession.Position,
            Duration = _loaded?.Duration ?? _state.Duration,
            IsBusy = false,
            Message = "Playback paused."
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _player.Pause();
        _player.PlaybackSession.Position = TimeSpan.Zero;
        Publish(_state with { Status = PlaybackStatus.Stopped, Position = TimeSpan.Zero, IsBusy = false, Message = "Playback stopped." });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        _player.Source = null;
        await DisposeStreamAsync().ConfigureAwait(false);
        _loaded = null;
        Publish(PlaybackState.Idle);
    }

    /// <inheritdoc />
    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return Task.CompletedTask;
        }

        var clamped = position < TimeSpan.Zero
            ? TimeSpan.Zero
            : position > _loaded.Duration
                ? _loaded.Duration
                : position;

        _player.PlaybackSession.Position = clamped;
        Publish(_state with { Position = clamped, Duration = _loaded.Duration, Message = "Position updated." });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _player.PlaybackSession.PositionChanged -= OnPositionChanged;
        _player.PlaybackSession.NaturalDurationChanged -= OnNaturalDurationChanged;
        _player.Dispose();
        await DisposeStreamAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reacts to native position updates and republishes them as immutable application snapshots.
    /// </summary>
    private void OnPositionChanged(MediaPlaybackSession sender, object args)
    {
        if (_loaded is null)
        {
            return;
        }

        Publish(_state with
        {
            Position = sender.Position,
            Duration = _loaded.Duration
        });
    }

    /// <summary>
    /// Updates the known duration when Windows reports a more accurate natural media duration.
    /// </summary>
    private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
    {
        if (_loaded is null)
        {
            return;
        }

        var duration = sender.NaturalDuration > TimeSpan.Zero ? sender.NaturalDuration : _loaded.Duration;
        Publish(_state with { Duration = duration });
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        Publish(_state with { Status = PlaybackStatus.Completed, Position = _loaded?.Duration ?? TimeSpan.Zero, IsBusy = false, Message = "Playback completed." });
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Publish(_state with { Status = PlaybackStatus.Error, IsBusy = false, Message = args.ErrorMessage });
    }

    /// <summary>
    /// Stores the latest playback snapshot and broadcasts it to consumers.
    /// </summary>
    private void Publish(PlaybackState state)
    {
        _state = state;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(state));
    }

    /// <summary>
    /// Releases the current in-memory audio stream, if any.
    /// </summary>
    private async Task DisposeStreamAsync()
    {
        if (_currentStream is not null)
        {
            await _currentStream.FlushAsync();
            _currentStream.Dispose();
            _currentStream = null;
        }
    }
}
#endif
