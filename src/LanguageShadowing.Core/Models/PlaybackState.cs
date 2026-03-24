using LanguageShadowing.Core.Enums;

namespace LanguageShadowing.Core.Models;

public sealed record PlaybackState(
    PlaybackStatus Status,
    TimeSpan Position,
    TimeSpan Duration,
    bool CanSeek,
    bool IsBusy,
    string? Message = null)
{
    public static PlaybackState Idle { get; } = new(PlaybackStatus.Idle, TimeSpan.Zero, TimeSpan.Zero, false, false);
}
