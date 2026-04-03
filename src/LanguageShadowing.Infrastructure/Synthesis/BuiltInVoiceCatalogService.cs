// MIT License
//
// Copyright (c) 2026 Jakub Melka and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;
using Microsoft.Maui.Media;

namespace LanguageShadowing.Infrastructure.Synthesis;

/// <summary>
/// Retrieves the voices exposed by the current platform.
/// </summary>
public sealed class BuiltInVoiceCatalogService : IVoiceCatalogService
{
    /// <inheritdoc />
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
