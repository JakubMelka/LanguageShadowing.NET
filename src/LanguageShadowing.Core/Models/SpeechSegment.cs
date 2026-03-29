namespace LanguageShadowing.Core.Models;

/// <summary>
/// Represents one planned fragment of synthesized speech.
/// </summary>
/// <param name="Text">The text spoken by the segment.</param>
/// <param name="Start">The start offset of the segment within the full utterance.</param>
/// <param name="Duration">The estimated or measured duration of the segment.</param>
/// <param name="StartIndex">The starting character index in the normalized source text.</param>
/// <param name="Length">The segment length in characters.</param>
public sealed record SpeechSegment(
    string Text,
    TimeSpan Start,
    TimeSpan Duration,
    int StartIndex,
    int Length);
