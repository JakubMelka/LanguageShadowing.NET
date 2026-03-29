using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Application.Analysis;

/// <summary>
/// Compares the expected source text with the recognized transcript and produces a score.
/// </summary>
public interface IShadowingAnalyzer
{
    /// <summary>
    /// Computes a shadowing assessment for the current transcript.
    /// </summary>
    ShadowingAssessment Assess(string sourceText, string recognizedText);
}
