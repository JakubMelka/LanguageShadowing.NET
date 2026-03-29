namespace LanguageShadowing.Core.Enums;

/// <summary>
/// Describes the recognizer lifecycle exposed to the UI.
/// </summary>
public enum RecognitionStatus
{
    /// <summary>The current platform does not implement speech recognition.</summary>
    Unsupported,

    /// <summary>The recognizer is idle and not listening.</summary>
    Idle,

    /// <summary>The recognizer is acquiring permissions or native resources.</summary>
    Starting,

    /// <summary>The recognizer is actively listening and may emit transcript updates.</summary>
    Listening,

    /// <summary>A graceful stop has been requested and is still completing.</summary>
    Stopping,

    /// <summary>The recognizer has completed a recognition session.</summary>
    Completed,

    /// <summary>The recognizer failed and needs user action or restart.</summary>
    Error
}
