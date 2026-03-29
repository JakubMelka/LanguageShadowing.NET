namespace LanguageShadowing.Application.Abstractions;

/// <summary>
/// Persists user-customizable shadowing settings.
/// </summary>
public interface IShadowingSettingsStore
{
    /// <summary>
    /// Loads previously saved settings.
    /// </summary>
    Task<ShadowingSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current settings snapshot.
    /// </summary>
    Task SaveAsync(ShadowingSettings settings, CancellationToken cancellationToken = default);
}
