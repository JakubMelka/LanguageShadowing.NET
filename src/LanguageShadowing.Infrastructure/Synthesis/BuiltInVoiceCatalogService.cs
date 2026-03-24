using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Microsoft.Maui.Media;

namespace LanguageShadowing.Infrastructure.Synthesis;

public sealed class BuiltInVoiceCatalogService : IVoiceCatalogService
{
    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        await Task.Yield();
        return Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
            .Select(voice => new VoiceInfo(
                voice.Id,
                voice.DisplayName,
                voice.Language,
                voice.Gender.ToString(),
                voice.Id == Windows.Media.SpeechSynthesis.SpeechSynthesizer.DefaultVoice?.Id))
            .ToArray();
#else
        var locales = await TextToSpeech.Default.GetLocalesAsync().ConfigureAwait(false);
        return locales
            .Select(locale => new VoiceInfo(
                locale.Name,
                string.IsNullOrWhiteSpace(locale.Country) ? locale.Name : $"{locale.Name} ({locale.Country})",
                locale.Language,
                null,
                false))
            .ToArray();
#endif
    }
}
