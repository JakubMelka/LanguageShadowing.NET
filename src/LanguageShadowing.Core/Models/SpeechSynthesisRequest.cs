namespace LanguageShadowing.Core.Models;

public sealed record SpeechSynthesisRequest(
    string Text,
    VoiceInfo? Voice,
    double Rate,
    double Pitch,
    double Volume);
