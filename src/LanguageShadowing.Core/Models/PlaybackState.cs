using LanguageShadowing.Core.Enums;

namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents a point-in-time playback snapshot emitted by an <see cref="Interfaces.IAudioPlaybackController"/>.
/// </summary>
/// <param name="Status">The coarse playback lifecycle state.</param>
/// <param name="Position">The current playback position.</param>
/// <param name="Duration">The known total duration.</param>
/// <param name="CanSeek">Indicates whether the current payload supports seeking.</param>
/// <param name="IsBusy">Indicates whether the underlying controller is in a transient busy state.</param>
/// <param name="Message">An optional human-readable status message suitable for diagnostics.</param>
public sealed record PlaybackState(
    PlaybackStatus Status,
    TimeSpan Position,
    TimeSpan Duration,
    bool CanSeek,
    bool IsBusy,
    string? Message = null)
{
    /// <summary>
    /// Gets a reusable idle snapshot.
    /// </summary>
    public static PlaybackState Idle { get; } = new(PlaybackStatus.Idle, TimeSpan.Zero, TimeSpan.Zero, false, false);
}
