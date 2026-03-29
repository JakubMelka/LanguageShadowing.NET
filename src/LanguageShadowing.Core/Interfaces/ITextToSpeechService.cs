using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

/// <summary>
/// Creates a platform-specific speech payload from a high-level synthesis request.
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// Prepares speech audio and metadata needed by playback and waveform visualization.
    /// </summary>
    Task<SpeechSynthesisResult> PrepareAsync(SpeechSynthesisRequest request, CancellationToken cancellationToken = default);
}
