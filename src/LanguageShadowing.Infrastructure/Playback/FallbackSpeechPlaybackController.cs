using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Microsoft.Maui.Media;

namespace LanguageShadowing.Infrastructure.Playback;

public sealed class FallbackSpeechPlaybackController : IAudioPlaybackController
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _playbackCts;
    private SpeechSynthesisResult? _loaded;
    private int _currentSegmentIndex;
    private int _playbackVersion;
    private PlaybackState _state = PlaybackState.Idle;

    public PlaybackState CurrentState => _state;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    public Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default)
    {
        _loaded = synthesisResult;
        _currentSegmentIndex = 0;
        Interlocked.Increment(ref _playbackVersion);
        Publish(new PlaybackState(PlaybackStatus.Ready, TimeSpan.Zero, synthesisResult.Duration, synthesisResult.Segments.Count > 0, false));
        return Task.CompletedTask;
    }

    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_loaded is null || _loaded.Segments.Count == 0)
            {
                Publish(new PlaybackState(PlaybackStatus.Error, TimeSpan.Zero, TimeSpan.Zero, false, false, "No audio plan is prepared."));
                return;
            }

            if (_state.Status == PlaybackStatus.Playing)
            {
                return;
            }

            _playbackCts?.Cancel();
            _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var playbackVersion = Interlocked.Increment(ref _playbackVersion);
            Publish(_state with { Status = PlaybackStatus.Playing, IsBusy = true, Message = "Playing in estimated fallback mode." });
            _ = RunPlaybackAsync(_loaded, playbackVersion, _playbackCts.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _playbackCts?.Cancel();
        Interlocked.Increment(ref _playbackVersion);
        Publish(_state with { Status = PlaybackStatus.Paused, IsBusy = false, Message = "Pause returns to the start of the current sentence in fallback mode." });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _playbackCts?.Cancel();
        Interlocked.Increment(ref _playbackVersion);
        _currentSegmentIndex = 0;
        Publish(_state with { Status = PlaybackStatus.Stopped, Position = TimeSpan.Zero, IsBusy = false, Message = "Playback stopped." });
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _playbackCts?.Cancel();
        Interlocked.Increment(ref _playbackVersion);
        _currentSegmentIndex = 0;
        _loaded = null;
        Publish(PlaybackState.Idle);
        return Task.CompletedTask;
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return;
        }

        var wasPlaying = _state.Status == PlaybackStatus.Playing;
        _playbackCts?.Cancel();
        Interlocked.Increment(ref _playbackVersion);
        _currentSegmentIndex = Math.Max(0, _loaded.Segments
            .Select((segment, index) => new { segment, index })
            .LastOrDefault(entry => entry.segment.Start <= position)?.index ?? 0);

        var segment = _loaded.Segments[_currentSegmentIndex];
        Publish(_state with { Position = segment.Start, Message = "Seek snaps to sentence boundaries in fallback mode." });

        if (wasPlaying)
        {
            await PlayAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask DisposeAsync()
    {
        _playbackCts?.Cancel();
        _playbackCts?.Dispose();
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task RunPlaybackAsync(SpeechSynthesisResult result, int playbackVersion, CancellationToken cancellationToken)
    {
        try
        {
            var options = await CreateSpeechOptionsAsync(result.Request.Voice?.Locale).ConfigureAwait(false);
            for (; _currentSegmentIndex < result.Segments.Count; _currentSegmentIndex++)
            {
                if (playbackVersion != Volatile.Read(ref _playbackVersion))
                {
                    return;
                }

                var segment = result.Segments[_currentSegmentIndex];
                Publish(_state with { Status = PlaybackStatus.Playing, Position = segment.Start, Duration = result.Duration, IsBusy = true });
                await TextToSpeech.Default.SpeakAsync(segment.Text, options, cancellationToken).ConfigureAwait(false);

                if (playbackVersion != Volatile.Read(ref _playbackVersion))
                {
                    return;
                }

                Publish(_state with { Status = PlaybackStatus.Playing, Position = segment.Start + segment.Duration, Duration = result.Duration, IsBusy = true });
            }

            if (playbackVersion != Volatile.Read(ref _playbackVersion))
            {
                return;
            }

            Publish(_state with { Status = PlaybackStatus.Completed, Position = result.Duration, Duration = result.Duration, IsBusy = false, Message = "Playback completed." });
            _currentSegmentIndex = 0;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (playbackVersion != Volatile.Read(ref _playbackVersion))
            {
                return;
            }

            Publish(_state with { Status = PlaybackStatus.Error, IsBusy = false, Message = ex.Message });
        }
    }

    private static async Task<SpeechOptions> CreateSpeechOptionsAsync(string? localeCode)
    {
        var options = new SpeechOptions
        {
            Pitch = 1f,
            Volume = 1f
        };

        if (string.IsNullOrWhiteSpace(localeCode))
        {
            return options;
        }

        var locale = (await TextToSpeech.Default.GetLocalesAsync().ConfigureAwait(false))
            .FirstOrDefault(item => string.Equals(item.Language, localeCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Id, localeCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Name, localeCode, StringComparison.OrdinalIgnoreCase));

        if (locale is not null)
        {
            options.Locale = locale;
        }

        return options;
    }

    private void Publish(PlaybackState state)
    {
        _state = state;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(state));
    }
}
