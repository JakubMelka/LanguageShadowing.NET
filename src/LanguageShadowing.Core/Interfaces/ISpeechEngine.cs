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
