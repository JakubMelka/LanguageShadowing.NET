using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

public interface ITextToSpeechService
{
    Task<SpeechSynthesisResult> PrepareAsync(SpeechSynthesisRequest request, CancellationToken cancellationToken = default);
}
