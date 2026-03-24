namespace LanguageShadowing.Core.Models;

public sealed record ShadowingAssessment(
    int? Score,
    string Summary,
    IReadOnlyList<string> MissingWords,
    IReadOnlyList<string> ExtraWords)
{
    public static ShadowingAssessment Unsupported { get; } =
        new(null, "Score is unavailable.", Array.Empty<string>(), Array.Empty<string>());
}

