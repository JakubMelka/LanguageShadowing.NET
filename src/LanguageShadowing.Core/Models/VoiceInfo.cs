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

using System.Globalization;

namespace LanguageShadowing.Core.Models;

/// <summary>
/// Describes one selectable synthesis voice.
/// </summary>
/// <param name="Id">The platform-specific identifier used to select the voice.</param>
/// <param name="DisplayName">The human-readable voice name.</param>
/// <param name="Locale">The BCP-47 locale exposed by the platform.</param>
/// <param name="Gender">An optional gender label exposed by the platform.</param>
/// <param name="IsDefault">Indicates whether the voice matches the platform default voice.</param>
public sealed record VoiceInfo(
    string Id,
    string DisplayName,
    string Locale,
    string? Gender = null,
    bool IsDefault = false)
{
    /// <summary>
    /// Gets a localized display name for <see cref="Locale"/> when the locale can be resolved by <see cref="CultureInfo"/>.
    /// </summary>
    public string LanguageDisplay => TryGetLanguageDisplay(Locale);

    /// <summary>
    /// Gets the label shown in the voice picker.
    /// </summary>
    public string SelectionLabel => string.Join(" | ", new[]
    {
        DisplayName,
        string.IsNullOrWhiteSpace(Gender) ? "Unknown" : Gender,
        string.IsNullOrWhiteSpace(LanguageDisplay) ? "Unknown language" : LanguageDisplay
    });

    private static string TryGetLanguageDisplay(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return string.Empty;
        }

        try
        {
            return CultureInfo.GetCultureInfo(locale).DisplayName;
        }
        catch (CultureNotFoundException)
        {
            return locale;
        }
    }
}
