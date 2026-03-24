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
    private readonly MediaPlayer _player;
    private readonly System.Timers.Timer _positionTimer;
    private InMemoryRandomAccessStream? _currentStream;
    private SpeechSynthesisResult? _loaded;
    private PlaybackState _state = PlaybackState.Idle;

    public WindowsAudioPlaybackController()
    {
        _player = new MediaPlayer();
        _player.MediaEnded += OnMediaEnded;
        _player.MediaFailed += OnMediaFailed;

        _positionTimer = new System.Timers.Timer(150);
        _positionTimer.Elapsed += (_, _) => Publish(_state with
        {
            Position = _player.PlaybackSession.Position,
            Duration = _loaded?.Duration ?? _state.Duration
        });
    }

    public PlaybackState CurrentState => _state;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    public async Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default)
    {
        await DisposeStreamAsync().ConfigureAwait(false);
        _loaded = synthesisResult;

        if (!synthesisResult.HasAudioPayload)
        {
            Publish(new PlaybackState(PlaybackStatus.Error, TimeSpan.Zero, synthesisResult.Duration, false, false, "Windows prehravac nedostal audio data."));
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
        Publish(new PlaybackState(PlaybackStatus.Ready, TimeSpan.Zero, synthesisResult.Duration, true, false, "Audio pripraveno."));
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return Task.CompletedTask;
        }

        _player.Play();
        _positionTimer.Start();
        Publish(_state with { Status = PlaybackStatus.Playing, IsBusy = false, Message = "Prehravam syntetizovanou rec." });
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _player.Pause();
        _positionTimer.Stop();
        Publish(_state with { Status = PlaybackStatus.Paused, Position = _player.PlaybackSession.Position, IsBusy = false, Message = "Prehravani pozastaveno." });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _player.Pause();
        _player.PlaybackSession.Position = TimeSpan.Zero;
        _positionTimer.Stop();
        Publish(_state with { Status = PlaybackStatus.Stopped, Position = TimeSpan.Zero, IsBusy = false, Message = "Prehravani zastaveno." });
        return Task.CompletedTask;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        _player.Source = null;
        await DisposeStreamAsync().ConfigureAwait(false);
        _loaded = null;
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
        Publish(_state with { Position = clamped, Duration = _loaded.Duration, Message = "Pozice aktualizovana." });
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _positionTimer.Stop();
        _positionTimer.Dispose();
        _player.Dispose();
        await DisposeStreamAsync().ConfigureAwait(false);
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        _positionTimer.Stop();
        Publish(_state with { Status = PlaybackStatus.Completed, Position = _loaded?.Duration ?? TimeSpan.Zero, IsBusy = false, Message = "Prehravani dokonceno." });
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _positionTimer.Stop();
        Publish(_state with { Status = PlaybackStatus.Error, IsBusy = false, Message = args.ErrorMessage });
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
