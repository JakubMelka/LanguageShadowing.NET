namespace LanguageShadowing.Core.Models;

/// <summary>
/// Describes a text-to-speech request before it is translated into platform-specific audio.
/// </summary>
/// <param name="Text">The text to synthesize.</param>
/// <param name="Voice">The selected voice, or <see langword="null"/> to use the platform default.</param>
/// <param name="Rate">The desired speaking rate, normalized to the application's UI range.</param>
/// <param name="Pitch">The desired pitch multiplier.</param>
/// <param name="Volume">The desired output volume in the range 0-1.</param>
public sealed record SpeechSynthesisRequest(
    string Text,
    VoiceInfo? Voice,
    double Rate,
    double Pitch,
    double Volume);
