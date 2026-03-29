using System.Text;
using System.Text.RegularExpressions;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Application.Analysis;

/// <summary>
/// Compares the source text with the recognized transcript by using tokenization and a longest-common-subsequence pass.
/// </summary>
/// <remarks>
/// The implementation intentionally favors determinism and transparency over linguistic sophistication.
/// The app needs a lightweight score that can be recalculated on every recognition update without introducing async work,
/// allocations for large intermediate objects, or platform-specific dependencies.
/// </remarks>
public sealed class ShadowingAnalyzer : IShadowingAnalyzer
{
    private static readonly Regex TokenRegex = new("[\\p{L}\\p{N}']+", RegexOptions.Compiled);

    /// <summary>
    /// Produces a score and mismatch summary for the current recognition output.
    /// </summary>
    public ShadowingAssessment Assess(string sourceText, string recognizedText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return ShadowingAssessment.Unsupported;
        }

        var sourceTokens = Tokenize(sourceText);
        var recognizedTokens = Tokenize(recognizedText);

        if (recognizedTokens.Count == 0)
        {
            return new ShadowingAssessment(0, "No speech has been recognized yet.", sourceTokens.Take(6).ToArray(), Array.Empty<string>());
        }

        var lcs = BuildLcs(sourceTokens, recognizedTokens);
        var missing = new List<string>();
        var extra = new List<string>();

        var i = 0;
        var j = 0;
        while (i < sourceTokens.Count && j < recognizedTokens.Count)
        {
            if (sourceTokens[i] == recognizedTokens[j])
            {
                i++;
                j++;
                continue;
            }

            if (lcs[i + 1, j] >= lcs[i, j + 1])
            {
                missing.Add(sourceTokens[i]);
                i++;
            }
            else
            {
                extra.Add(recognizedTokens[j]);
                j++;
            }
        }

        while (i < sourceTokens.Count)
        {
            missing.Add(sourceTokens[i++]);
        }

        while (j < recognizedTokens.Count)
        {
            extra.Add(recognizedTokens[j++]);
        }

        var lcsLength = lcs[0, 0];
        var baseline = Math.Max(sourceTokens.Count, recognizedTokens.Count);
        var score = baseline == 0 ? 100 : (int)Math.Round(100d * lcsLength / baseline);

        var summary = BuildSummary(score, missing, extra);
        return new ShadowingAssessment(score, summary, missing.Take(10).ToArray(), extra.Take(10).ToArray());
    }

    private static List<string> Tokenize(string text)
    {
        return TokenRegex.Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .ToList();
    }

    private static int[,] BuildLcs(IReadOnlyList<string> source, IReadOnlyList<string> recognized)
    {
        var matrix = new int[source.Count + 1, recognized.Count + 1];

        for (var i = source.Count - 1; i >= 0; i--)
        {
            for (var j = recognized.Count - 1; j >= 0; j--)
            {
                matrix[i, j] = source[i] == recognized[j]
                    ? matrix[i + 1, j + 1] + 1
                    : Math.Max(matrix[i + 1, j], matrix[i, j + 1]);
            }
        }

        return matrix;
    }

    private static string BuildSummary(int score, IReadOnlyList<string> missing, IReadOnlyList<string> extra)
    {
        var builder = new StringBuilder();
        builder.Append($"Score {score}/100.");

        if (missing.Count > 0)
        {
            builder.Append($" Missing: {string.Join(", ", missing.Take(4))}.");
        }

        if (extra.Count > 0)
        {
            builder.Append($" Extra: {string.Join(", ", extra.Take(4))}.");
        }

        if (missing.Count == 0 && extra.Count == 0)
        {
            builder.Append(" The match looks very good.");
        }

        return builder.ToString();
    }
}
