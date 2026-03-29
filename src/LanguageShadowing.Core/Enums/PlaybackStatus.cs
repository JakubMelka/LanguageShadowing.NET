namespace LanguageShadowing.Core.Enums;

/// <summary>
/// Describes the coarse playback lifecycle used by the view model and UI.
/// </summary>
public enum PlaybackStatus
{
    /// <summary>No audio is loaded.</summary>
    Idle,

    /// <summary>Audio is prepared and can be started.</summary>
    Ready,

    /// <summary>Playback is currently advancing.</summary>
    Playing,

    /// <summary>Playback is paused but can be resumed.</summary>
    Paused,

    /// <summary>Playback was explicitly stopped and rewound.</summary>
    Stopped,

    /// <summary>Playback reached the natural end of the media.</summary>
    Completed,

    /// <summary>An unrecoverable playback error occurred.</summary>
    Error
}
