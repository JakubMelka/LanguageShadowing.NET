using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Application.Analysis;

public interface IShadowingAnalyzer
{
    ShadowingAssessment Assess(string sourceText, string recognizedText);
}
