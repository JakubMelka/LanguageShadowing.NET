using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

public interface IVoiceCatalogService
{
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default);
}
