using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Microsoft.Maui.Media;

namespace LanguageShadowing.Infrastructure.Playback;

/// <summary>
/// Estimated playback controller used when the platform cannot provide a real audio stream for the synthesized text.
/// </summary>
/// <remarks>
/// <para>
/// This controller does not play pre-generated audio bytes. Instead it walks through the already planned speech segments
/// and asks MAUI's <see cref="TextToSpeech"/> API to speak each segment one after another.
/// </para>
/// <para>
/// Because MAUI TTS does not expose a true playback clock, the controller can only estimate progress. It therefore
/// publishes the start and end of each segment as coarse playback positions. That is sufficient for this application,
/// whose UI only needs to show approximate forward movement rather than sample-accurate timing.
/// </para>
/// <para>
/// The asynchronous playback loop runs in the background after <see cref="PlayAsync"/> returns. That design keeps the UI
/// responsive and lets transport commands like Pause, Stop, and Seek cancel the old loop and start a new one if needed.
/// </para>
/// </remarks>
public sealed class FallbackSpeechPlaybackController : IAudioPlaybackController
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _playbackCts;
    private SpeechSynthesisResult? _loaded;
    private int _currentSegmentIndex;
    private PlaybackState _state = PlaybackState.Idle;

    /// <inheritdoc />
    public PlaybackState CurrentState => _state;

    /// <inheritdoc />
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default)
    {
        _loaded = synthesisResult;
        _currentSegmentIndex = 0;
        Publish(new PlaybackState(PlaybackStatus.Ready, TimeSpan.Zero, synthesisResult.Duration, synthesisResult.Segments.Count > 0, false));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
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
            Publish(_state with { Status = PlaybackStatus.Playing, IsBusy = true, Message = "Playing in estimated fallback mode." });
            _ = RunPlaybackAsync(_loaded, _playbackCts.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _playbackCts?.Cancel();
        Publish(_state with { Status = PlaybackStatus.Paused, IsBusy = false, Message = "Pause returns to the start of the current sentence in fallback mode." });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _playbackCts?.Cancel();
        _currentSegmentIndex = 0;
        Publish(_state with { Status = PlaybackStatus.Stopped, Position = TimeSpan.Zero, IsBusy = false, Message = "Playback stopped." });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _playbackCts?.Cancel();
        _currentSegmentIndex = 0;
        _loaded = null;
        Publish(PlaybackState.Idle);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        if (_loaded is null)
        {
            return;
        }

        var wasPlaying = _state.Status == PlaybackStatus.Playing;
        _playbackCts?.Cancel();
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

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _playbackCts?.Cancel();
        _playbackCts?.Dispose();
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Runs the background segment-by-segment playback loop.
    /// </summary>
    /// <remarks>
    /// This is intentionally not awaited by <see cref="PlayAsync"/>. The caller only needs playback to start, not to
    /// wait until every segment has finished. Cancellation stops the current loop and lets another transport command
    /// replace it with a newer loop.
    /// </remarks>
    private async Task RunPlaybackAsync(SpeechSynthesisResult result, CancellationToken cancellationToken)
    {
        try
        {
            var options = await CreateSpeechOptionsAsync(result.Request.Voice?.Locale).ConfigureAwait(false);
            for (; _currentSegmentIndex < result.Segments.Count; _currentSegmentIndex++)
            {
                var segment = result.Segments[_currentSegmentIndex];

                // Publish the segment start before speech begins so the UI can move immediately.
                Publish(_state with { Status = PlaybackStatus.Playing, Position = segment.Start, Duration = result.Duration, IsBusy = true });
                await TextToSpeech.Default.SpeakAsync(segment.Text, options, cancellationToken).ConfigureAwait(false);

                // Publish the estimated segment end after speech finishes. This is not exact audio timing, but it gives
                // the user a stable feeling of progress through the utterance.
                Publish(_state with { Status = PlaybackStatus.Playing, Position = segment.Start + segment.Duration, Duration = result.Duration, IsBusy = true });
            }

            Publish(_state with { Status = PlaybackStatus.Completed, Position = result.Duration, Duration = result.Duration, IsBusy = false, Message = "Playback completed." });
            _currentSegmentIndex = 0;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Publish(_state with { Status = PlaybackStatus.Error, IsBusy = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Resolves a MAUI speech locale from the requested voice locale when possible.
    /// </summary>
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
