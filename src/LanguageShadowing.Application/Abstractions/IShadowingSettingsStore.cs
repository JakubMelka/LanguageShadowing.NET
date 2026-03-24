namespace LanguageShadowing.Application.Abstractions;

public interface IShadowingSettingsStore
{
    Task<ShadowingSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ShadowingSettings settings, CancellationToken cancellationToken = default);
}
