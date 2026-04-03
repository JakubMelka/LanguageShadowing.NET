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

using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

/// <summary>
/// Aggregates the platform services required for a full shadowing session.
/// </summary>
public interface ISpeechEngine
{
    /// <summary>
    /// Gets the human-readable engine name shown in the UI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the static feature flags supported by this engine.
    /// </summary>
    SpeechEngineCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the voice catalog used to populate the voice picker.
    /// </summary>
    IVoiceCatalogService VoiceCatalog { get; }

    /// <summary>
    /// Gets the text-to-speech service that prepares speech output.
    /// </summary>
    ITextToSpeechService TextToSpeech { get; }

    /// <summary>
    /// Gets the speech recognition service used while shadowing is active.
    /// </summary>
    ISpeechRecognitionService Recognition { get; }

    /// <summary>
    /// Gets the playback controller that renders prepared speech.
    /// </summary>
    IAudioPlaybackController Playback { get; }
}
