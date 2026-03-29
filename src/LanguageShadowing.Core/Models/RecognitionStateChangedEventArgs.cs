using LanguageShadowing.Core.Enums;

namespace LanguageShadowing.Core.Models;

/// <summary>
/// Event arguments carrying a recognizer lifecycle transition.
/// </summary>
public sealed class RecognitionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecognitionStateChangedEventArgs"/> class.
    /// </summary>
    public RecognitionStateChangedEventArgs(RecognitionStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }

    /// <summary>
    /// Gets the new recognizer status.
    /// </summary>
    public RecognitionStatus Status { get; }

    /// <summary>
    /// Gets an optional human-readable diagnostic message.
    /// </summary>
    public string? Message { get; }
}
