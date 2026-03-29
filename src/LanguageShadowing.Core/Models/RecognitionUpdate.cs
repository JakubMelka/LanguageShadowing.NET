namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents a partial or final transcript fragment produced by the recognizer.
/// </summary>
/// <param name="FullText">The full transcript assembled so far.</param>
/// <param name="LatestText">The latest partial or final text fragment.</param>
/// <param name="IsFinal">Indicates whether <paramref name="LatestText"/> is stable.</param>
/// <param name="Confidence">An optional engine-specific confidence score.</param>
public sealed record RecognitionUpdate(
    string FullText,
    string LatestText,
    bool IsFinal,
    double? Confidence = null);
