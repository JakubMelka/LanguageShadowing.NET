namespace LanguageShadowing.Core.Models;

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackStateChangedEventArgs(PlaybackState state)
    {
        State = state;
    }

    public PlaybackState State { get; }
}
