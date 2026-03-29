using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

/// <summary>
/// Controls playback of prepared speech audio and exposes immutable playback snapshots via <see cref="StateChanged"/>.
/// </summary>
/// <remarks>
/// Implementations are responsible for translating native platform events into coarse-grained playback states.
/// The view model intentionally reacts to <see cref="CurrentState"/> and <see cref="StateChanged"/> instead of
/// talking to platform objects directly so that asynchronous playback behavior stays testable and platform-neutral.
/// </remarks>
public interface IAudioPlaybackController : IAsyncDisposable
{
    /// <summary>
    /// Gets the most recent playback snapshot known to the controller.
    /// </summary>
    PlaybackState CurrentState { get; }

    /// <summary>
    /// Raised whenever the controller observes a meaningful playback state change.
    /// </summary>
    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Loads synthesized speech into the playback pipeline.
    /// </summary>
    Task LoadAsync(SpeechSynthesisResult synthesisResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts playback or resumes it when the platform supports pause and resume.
    /// </summary>
    Task PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Temporarily pauses playback without discarding the prepared media when the platform supports it.
    /// </summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops playback and moves the playback position back to the beginning of the current item.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the controller to a clean idle state and releases any prepared media.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves playback to a specific position within the prepared media.
    /// </summary>
    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);
}
