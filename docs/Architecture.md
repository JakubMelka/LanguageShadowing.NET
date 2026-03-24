# Language Shadowing - Architecture Notes

## Overview

The solution is split into four layers:

- `LanguageShadowing.Core`: speech contracts, capability model, playback/recognition state models, DTOs.
- `LanguageShadowing.Application`: lightweight MVVM infrastructure, settings abstractions, transcript comparison, and `MainViewModel`.
- `LanguageShadowing.Infrastructure`: built-in speech engine, waveform generation, settings persistence, Windows adapters, and extension points for future non-Windows fallbacks.
- `LanguageShadowing.App`: .NET MAUI single-project UI, styles, XAML views, and the custom waveform control.

## Speech Abstraction

The UI never talks directly to platform APIs. It only sees these interfaces from `LanguageShadowing.Core`:

- `ISpeechEngine`
- `IVoiceCatalogService`
- `ITextToSpeechService`
- `IAudioPlaybackController`
- `ISpeechRecognitionService`

### Contracts

`ISpeechEngine` is a composition root around the currently active provider.

`ITextToSpeechService` prepares a `SpeechSynthesisResult`, which contains:

- the original request
- playback segments
- overall duration
- waveform data
- optional raw audio payload
- a flag that says whether the result is estimated or based on real audio

`IAudioPlaybackController` handles transport commands only:

- `LoadAsync`
- `PlayAsync`
- `PauseAsync`
- `StopAsync`
- `SeekAsync`
- `ResetAsync`

`ISpeechRecognitionService` is event-driven and publishes:

- recognition status changes
- incremental transcript updates

The capability model is explicit via `SpeechEngineCapabilities`, so the UI can adjust when the active engine cannot provide a feature with the same quality on every platform.

## Built-in Engine

### Windows

The Windows provider uses Microsoft platform APIs:

- `Windows.Media.SpeechSynthesis.SpeechSynthesizer` for voice enumeration and TTS synthesis
- `Windows.Media.Playback.MediaPlayer` for pause/stop/seek on synthesized audio
- `Windows.Media.SpeechRecognition.SpeechRecognizer` for streaming microphone transcription

This path provides the strongest MVP feature set:

- actual voice list
- synthesized audio payload
- waveform derived from generated WAV data when available
- continuous transcription events

### Future non-Windows fallback

The infrastructure still keeps a fallback path conceptually separated, but the current app target is Windows-only:

- speech is planned as sentence segments
- playback is executed through `TextToSpeech.Default`
- seek snaps to sentence boundaries
- waveform is estimated from the playback plan
- speech recognition is marked unsupported in MVP

## UI Structure

`MainPage` is composed of four blocks:

1. top toolbar with voice and rate selection
2. editable source text card
3. playback card with transport buttons, progress slider, and waveform
4. recognized transcript card with quality score badge

The waveform is rendered by a custom `GraphicsView`-based control (`WaveformView`) with no third-party charting library.

## Transcript Scoring

`ShadowingAnalyzer` performs a lightweight token comparison using an LCS-based alignment.

It outputs:

- score `0-100`
- short summary
- missing tokens
- extra tokens

The ViewModel maps the score to a red-to-green color so the lower card can show a quick quality indicator.

## Persisted Settings

Only the following values are persisted between launches:

- selected voice id
- speech rate

Source text is intentionally not persisted, following the assignment.

## Supported Platforms in This MVP

Implemented structure:

- Windows: primary implementation

Not targeted in this MVP:

- macOS / Mac Catalyst
- Android
- iOS
- Azure Speech engine

## Known MVP Limits

- No build or runtime validation was executed in this repository snapshot.
- The app project is currently Windows-only.
- The fallback seek model is sentence-based, not true sample-accurate audio seek.
- The score is a simple lexical similarity heuristic, not phonetic evaluation.
- Windows STT/TTS still depends on platform permissions and installed system voices.
- The waveform falls back to a synthetic envelope when raw audio analysis is unavailable.
