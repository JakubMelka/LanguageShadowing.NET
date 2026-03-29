using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

/// <summary>
/// Provides the voices that can be selected for speech synthesis.
/// </summary>
public interface IVoiceCatalogService
{
    /// <summary>
    /// Retrieves the voices currently exposed by the underlying platform.
    /// </summary>
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default);
}
