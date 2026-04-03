#if WINDOWS
using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace LanguageShadowing.Infrastructure.Playback;

public sealed class WindowsAudioPlaybackController : IAudioPlaybackController
{
    private MediaPlayer _player;
    private InMemoryRandomAccessStream? _currentStream;
    private SpeechSynthesisResult? _loaded;
    private PlaybackState _state = PlaybackState.Idle;

    public WindowsAudioPlaybackController()
    {
        _player = CreatePlayer();
    }

    public PlaybackState CurrentState => _state;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    public async Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default)
    {
        DisposePlayer();
        _player = CreatePlayer();
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

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return Task.CompletedTask;
        }

        Publish(_state with
        {
            Status = PlaybackStatus.Playing,
            Position = _player.PlaybackSession.Position,
            Duration = _loaded.Duration,
            IsBusy = false,
            Message = "Playing synthesized speech."
        });
        _player.Play();
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return Task.CompletedTask;
        }

        _player.Pause();
        Publish(_state with
        {
            Status = PlaybackStatus.Paused,
            Position = _player.PlaybackSession.Position,
            Duration = _loaded.Duration,
            IsBusy = false,
            Message = "Playback paused."
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return Task.CompletedTask;
        }

        _player.Pause();
        _player.PlaybackSession.Position = TimeSpan.Zero;
        Publish(_state with { Status = PlaybackStatus.Stopped, Position = TimeSpan.Zero, IsBusy = false, Message = "Playback stopped." });
        return Task.CompletedTask;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        _loaded = null;
        DisposePlayer();
        _player = CreatePlayer();
        await DisposeStreamAsync().ConfigureAwait(false);
        Publish(PlaybackState.Idle);
    }

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

    public async ValueTask DisposeAsync()
    {
        _loaded = null;
        DisposePlayer();
        await DisposeStreamAsync().ConfigureAwait(false);
    }

    private void OnPositionChanged(MediaPlaybackSession sender, object args)
    {
        if (_loaded is null)
        {
            return;
        }

        Publish(_state with
        {
            Status = GetEffectiveStatus(sender),
            Position = sender.Position,
            Duration = _loaded.Duration
        });
    }

    private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
    {
        if (_loaded is null)
        {
            return;
        }

        var duration = sender.NaturalDuration > TimeSpan.Zero ? sender.NaturalDuration : _loaded.Duration;
        Publish(_state with
        {
            Status = GetEffectiveStatus(sender),
            Duration = duration
        });
    }

    private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        if (_loaded is null)
        {
            return;
        }

        Publish(_state with
        {
            Status = GetEffectiveStatus(sender),
            Position = sender.Position,
            Duration = sender.NaturalDuration > TimeSpan.Zero ? sender.NaturalDuration : _loaded.Duration
        });
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        if (_loaded is null)
        {
            return;
        }

        Publish(_state with { Status = PlaybackStatus.Completed, Position = _loaded.Duration, IsBusy = false, Message = "Playback completed." });
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        if (_loaded is null)
        {
            return;
        }

        Publish(_state with { Status = PlaybackStatus.Error, IsBusy = false, Message = args.ErrorMessage });
    }

    private MediaPlayer CreatePlayer()
    {
        var player = new MediaPlayer();
        player.MediaEnded += OnMediaEnded;
        player.MediaFailed += OnMediaFailed;
        player.PlaybackSession.PositionChanged += OnPositionChanged;
        player.PlaybackSession.NaturalDurationChanged += OnNaturalDurationChanged;
        player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
        return player;
    }

    private void DisposePlayer()
    {
        _player.MediaEnded -= OnMediaEnded;
        _player.MediaFailed -= OnMediaFailed;
        _player.PlaybackSession.PositionChanged -= OnPositionChanged;
        _player.PlaybackSession.NaturalDurationChanged -= OnNaturalDurationChanged;
        _player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
        _player.Source = null;
        _player.Dispose();
    }

    private PlaybackStatus GetEffectiveStatus(MediaPlaybackSession session)
    {
        return session.PlaybackState switch
        {
            MediaPlaybackState.Playing => PlaybackStatus.Playing,
            MediaPlaybackState.Paused when _state.Status is PlaybackStatus.Stopped or PlaybackStatus.Completed or PlaybackStatus.Error
                => _state.Status,
            MediaPlaybackState.Paused => PlaybackStatus.Paused,
            MediaPlaybackState.None when _state.Status is PlaybackStatus.Stopped or PlaybackStatus.Completed or PlaybackStatus.Error
                => _state.Status,
            MediaPlaybackState.None => _loaded is null ? PlaybackStatus.Idle : PlaybackStatus.Ready,
            _ => _state.Status
        };
    }

    private void Publish(PlaybackState state)
    {
        _state = state;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(state));
    }

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
