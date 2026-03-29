namespace LanguageShadowing.Core.Models;

/// <summary>
/// Event arguments carrying a new <see cref="PlaybackState"/> snapshot.
/// </summary>
public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStateChangedEventArgs"/> class.
    /// </summary>
    public PlaybackStateChangedEventArgs(PlaybackState state)
    {
        State = state;
    }

    /// <summary>
    /// Gets the latest playback snapshot.
    /// </summary>
    public PlaybackState State { get; }
}
