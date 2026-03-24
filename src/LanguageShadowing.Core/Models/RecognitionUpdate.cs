namespace LanguageShadowing.Core.Models;

public sealed record RecognitionUpdate(
    string FullText,
    string LatestText,
    bool IsFinal,
    double? Confidence = null);
