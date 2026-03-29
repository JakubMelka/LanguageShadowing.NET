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
