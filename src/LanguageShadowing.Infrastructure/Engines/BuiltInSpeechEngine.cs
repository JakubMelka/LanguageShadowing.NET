using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Infrastructure.Engines;

public sealed class BuiltInSpeechEngine : ISpeechEngine
{
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

    public string Name { get; }

    public SpeechEngineCapabilities Capabilities { get; }

    public IVoiceCatalogService VoiceCatalog { get; }

    public ITextToSpeechService TextToSpeech { get; }

    public ISpeechRecognitionService Recognition { get; }

    public IAudioPlaybackController Playback { get; }
}
