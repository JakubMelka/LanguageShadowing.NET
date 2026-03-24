using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

public interface ISpeechEngine
{
    string Name { get; }

    SpeechEngineCapabilities Capabilities { get; }

    IVoiceCatalogService VoiceCatalog { get; }

    ITextToSpeechService TextToSpeech { get; }

    ISpeechRecognitionService Recognition { get; }

    IAudioPlaybackController Playback { get; }
}
