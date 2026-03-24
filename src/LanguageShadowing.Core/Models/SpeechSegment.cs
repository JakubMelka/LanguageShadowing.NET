namespace LanguageShadowing.Core.Models;

public sealed record SpeechSegment(
    string Text,
    TimeSpan Start,
    TimeSpan Duration,
    int StartIndex,
    int Length);
