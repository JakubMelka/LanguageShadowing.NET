namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents the text comparison result between the source phrase and the recognized transcript.
/// </summary>
/// <param name="Score">A normalized score in the range 0-100, or <see langword="null"/> when unavailable.</param>
/// <param name="Summary">A compact explanation suitable for the UI.</param>
/// <param name="MissingWords">Words found in the source text but missing from the recognized transcript.</param>
/// <param name="ExtraWords">Words present in the recognized transcript but not in the source text.</param>
public sealed record ShadowingAssessment(
    int? Score,
    string Summary,
    IReadOnlyList<string> MissingWords,
    IReadOnlyList<string> ExtraWords)
{
    /// <summary>
    /// Gets a reusable placeholder result used when scoring cannot be produced.
    /// </summary>
    public static ShadowingAssessment Unsupported { get; } =
        new(null, "Score is unavailable.", Array.Empty<string>(), Array.Empty<string>());
}
