using LanguageShadowing.Core.Enums;

namespace LanguageShadowing.Core.Models;

public sealed class RecognitionStateChangedEventArgs : EventArgs
{
    public RecognitionStateChangedEventArgs(RecognitionStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }

    public RecognitionStatus Status { get; }

    public string? Message { get; }
}
