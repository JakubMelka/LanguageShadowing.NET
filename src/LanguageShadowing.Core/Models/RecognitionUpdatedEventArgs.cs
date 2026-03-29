namespace LanguageShadowing.Core.Models;

/// <summary>
/// Event arguments carrying a transcript update.
/// </summary>
public sealed class RecognitionUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecognitionUpdatedEventArgs"/> class.
    /// </summary>
    public RecognitionUpdatedEventArgs(RecognitionUpdate update)
    {
        Update = update;
    }

    /// <summary>
    /// Gets the transcript update produced by the recognizer.
    /// </summary>
    public RecognitionUpdate Update { get; }
}
