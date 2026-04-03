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

namespace LanguageShadowing.Infrastructure.Engines;

/// <summary>
/// Immutable composition root object that exposes the selected platform services as one speech engine.
/// </summary>
public sealed class BuiltInSpeechEngine : ISpeechEngine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltInSpeechEngine"/> class.
    /// </summary>
    public BuiltInSpeechEngine(
        string name,
        SpeechEngineCapabilities capabilities,
        IVoiceCatalogService voiceCatalog,
        ITextToSpeechService textToSpeech,
        ISpeechRecognitionService recognition,
        IAudioPlaybackController playback)
    {
        Name = name;
        Capabilities = capabilities;
        VoiceCatalog = voiceCatalog;
        TextToSpeech = textToSpeech;
        Recognition = recognition;
        Playback = playback;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public SpeechEngineCapabilities Capabilities { get; }

    /// <inheritdoc />
    public IVoiceCatalogService VoiceCatalog { get; }

    /// <inheritdoc />
    public ITextToSpeechService TextToSpeech { get; }

    /// <inheritdoc />
    public ISpeechRecognitionService Recognition { get; }

    /// <inheritdoc />
    public IAudioPlaybackController Playback { get; }
}
