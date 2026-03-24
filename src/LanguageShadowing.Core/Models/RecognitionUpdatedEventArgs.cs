namespace LanguageShadowing.Core.Models;

public sealed class RecognitionUpdatedEventArgs : EventArgs
{
    public RecognitionUpdatedEventArgs(RecognitionUpdate update)
    {
        Update = update;
    }

    public RecognitionUpdate Update { get; }
}
