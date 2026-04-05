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

using System.Collections.ObjectModel;
using System.Globalization;
using LanguageShadowing.Application.Abstractions;
using LanguageShadowing.Application.Analysis;
using LanguageShadowing.Application.Common;
using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Application.ViewModels;

/// <summary>
/// Main orchestration view model for the application.
/// </summary>
/// <remarks>
/// <para>
/// This type is the session coordinator for the entire MAUI front end. It does not synthesize speech, play audio,
/// or run recognition by itself. Instead, it translates user intent into calls to the platform services exposed by
/// <see cref="ISpeechEngine"/> and then projects their results back into bindable UI state.
/// </para>
/// <para>
/// The most important design constraint in this class is not plain MVVM binding, but coordination of asynchronous,
/// event-driven workflows. Playback, speech synthesis, and recognition all operate independently and can publish state
/// changes from callbacks that do not naturally arrive in UI order. To avoid races between commands and service events,
/// all session mutations are serialized through <see cref="_sessionQueue"/>.
/// </para>
/// <para>
/// In practical terms, button commands and external service callbacks both flow into one ordered session pipeline,
/// while actual bindable property updates are marshalled back to the UI thread via <see cref="IAppDispatcher"/>.
/// </para>
/// </remarks>
public sealed class MainViewModel : ObservableObject
{
    private const string AutomaticThemeOption = "Automatic";
    private const string LightThemeOption = "Light";
    private const string DarkThemeOption = "Dark";

    private static readonly IReadOnlyList<string> AvailableThemeOptions =
    [
        AutomaticThemeOption,
        LightThemeOption,
        DarkThemeOption
    ];

    private readonly ISpeechEngine _engine;
    private readonly IShadowingSettingsStore _settingsStore;
    private readonly IShadowingAnalyzer _analyzer;
    private readonly IAppDispatcher _dispatcher;
    private readonly IAppThemeService _themeService;
    private readonly ISettingsLauncher _settingsLauncher;
    /// <summary>
    /// Serializes all session mutations so commands and external callbacks cannot update the same state concurrently.
    /// </summary>
    private readonly SerialTaskQueue _sessionQueue = new();
    private readonly ObservableCollection<VoiceInfo> _voices = new();
    private readonly ObservableCollection<string> _engines = new();
    private IReadOnlyList<float> _waveformSamples = Array.Empty<float>();
    private SpeechEngineCapabilities _capabilities = new(false, false, false, false, false, false);
    private SpeechSynthesisResult? _preparedSynthesis;
    private string? _preparedSignature;
    private bool _isInitialized;
    private bool _isBusy;
    private bool _isDictationEnabled;
    private string _sourceText = string.Empty;
    private string _recognizedText = string.Empty;
    private VoiceInfo? _selectedVoice;
    private string? _selectedEngineName;
    private string _selectedThemeOption = AutomaticThemeOption;
    private double _speechRate = 1.0;
    private double _speechPitch = 1.0;
    private double _speechVolume = 1.0;
    private string _statusMessage = "Ready.";
    private PlaybackState _playback = PlaybackState.Idle;
    private string _recognitionAvailabilityText = "Dictation is off.";
    private string _shadowingSummary = "The score will appear after recognition starts.";
    private string _scoreColorHex = "#A0A7B8";
    private int? _shadowingScore;
    private RecognitionStatus _currentRecognitionStatus = RecognitionStatus.Idle;
    /// <summary>
    /// Monotonic version used to invalidate stale recognition refresh requests.
    /// </summary>
    private int _recognitionRefreshVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="engine">Aggregates the active speech synthesis, playback, recognition, and voice catalog services.</param>
    /// <param name="settingsStore">Persists user-selected voice, theme, playback profile, and dictation preference.</param>
    /// <param name="analyzer">Computes a lightweight shadowing score from source and recognized text.</param>
    /// <param name="dispatcher">Executes UI-bound state changes on the application's main thread.</param>
    /// <param name="themeService">Applies the selected visual theme to the app shell.</param>
    /// <param name="settingsLauncher">Opens operating system privacy settings when speech permissions need attention.</param>
    public MainViewModel(
        ISpeechEngine engine,
        IShadowingSettingsStore settingsStore,
        IShadowingAnalyzer analyzer,
        IAppDispatcher dispatcher,
        IAppThemeService themeService,
        ISettingsLauncher settingsLauncher)
    {
        _engine = engine;
        _settingsStore = settingsStore;
        _analyzer = analyzer;
        _dispatcher = dispatcher;
        _themeService = themeService;
        _settingsLauncher = settingsLauncher;
        Voices = AsReadOnly(_voices);
        Engines = AsReadOnly(_engines);
        ThemeOptions = AvailableThemeOptions;

        _engines.Add(_engine.Name);
        _selectedEngineName = _engine.Name;
        _recognitionAvailabilityText = _engine.Recognition.IsAvailable
            ? "Dictation is off."
            : "Speech recognition is not available on this platform";

        InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !_isInitialized && !IsBusy);
        PlayCommand = new AsyncRelayCommand(PlayAsync);
        PauseCommand = new AsyncRelayCommand(PauseAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        RewindCommand = new AsyncRelayCommand(RewindAsync);
        ResetCommand = new AsyncRelayCommand(ResetAsync);
        ToggleDictationCommand = new AsyncRelayCommand(ToggleDictationAsync);
        OpenSpeechPrivacySettingsCommand = new AsyncRelayCommand(OpenSpeechPrivacySettingsAsync, () => ShowRecognitionSettingsButtons);
        OpenMicrophonePrivacySettingsCommand = new AsyncRelayCommand(OpenMicrophonePrivacySettingsAsync, () => ShowRecognitionSettingsButtons);
        ClearRecognizedCommand = new AsyncRelayCommand(ClearRecognizedAsync);

        _engine.Playback.StateChanged += OnPlaybackStateChanged;
        _engine.Recognition.StateChanged += OnRecognitionStateChanged;
        _engine.Recognition.RecognitionUpdated += OnRecognitionUpdated;
    }

    /// <summary>
    /// Gets the available voices exposed by the active speech engine.
    /// </summary>
    public ReadOnlyObservableCollection<VoiceInfo> Voices { get; }

    /// <summary>
    /// Gets the list of engine names shown in the UI.
    /// </summary>
    /// <remarks>
    /// The current application uses a single concrete engine, but the property still exposes a list because the UI is
    /// designed like a normal picker and the abstraction leaves room for future engine selection.
    /// </remarks>
    public ReadOnlyObservableCollection<string> Engines { get; }

    /// <summary>
    /// Gets the user-facing theme options displayed in the appearance picker.
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; }

    /// <summary>
    /// Gets the command that lazily loads persisted settings, voices, and engine capabilities.
    /// </summary>
    public AsyncRelayCommand InitializeCommand { get; }

    /// <summary>
    /// Gets the command that starts playback or resumes a paused session.
    /// </summary>
    public AsyncRelayCommand PlayCommand { get; }

    /// <summary>
    /// Gets the command that pauses playback when the active engine supports pause.
    /// </summary>
    public AsyncRelayCommand PauseCommand { get; }

    /// <summary>
    /// Gets the command that stops playback and rewinds the prepared media to the beginning.
    /// </summary>
    public AsyncRelayCommand StopCommand { get; }

    /// <summary>
    /// Gets the command that seeks the prepared media to the start without discarding the session.
    /// </summary>
    public AsyncRelayCommand RewindCommand { get; }

    /// <summary>
    /// Gets the command that clears playback, cached synthesis, transcript state, and source text.
    /// </summary>
    public AsyncRelayCommand ResetCommand { get; }

    /// <summary>
    /// Gets the command that toggles the independent dictation preference on or off.
    /// </summary>
    public AsyncRelayCommand ToggleDictationCommand { get; }

    /// <summary>
    /// Gets the command that opens operating system speech privacy settings.
    /// </summary>
    public AsyncRelayCommand OpenSpeechPrivacySettingsCommand { get; }

    /// <summary>
    /// Gets the command that opens operating system microphone privacy settings.
    /// </summary>
    public AsyncRelayCommand OpenMicrophonePrivacySettingsCommand { get; }

    /// <summary>
    /// Gets the command that clears the recognized transcript and refreshes recognition state.
    /// </summary>
    public AsyncRelayCommand ClearRecognizedCommand { get; }

    public SpeechEngineCapabilities Capabilities
    {
        get => _capabilities;
        private set
        {
            if (SetProperty(ref _capabilities, value))
            {
                OnPropertyChanged(nameof(CanSeek));
                NotifyCommands();
            }
        }
    }

    public string? SelectedEngineName
    {
        get => _selectedEngineName;
        set => SetProperty(ref _selectedEngineName, value ?? _engine.Name);
    }

    /// <summary>
    /// Gets or sets the currently selected synthesis voice.
    /// </summary>
    /// <remarks>
    /// Changing the voice invalidates the prepared synthesis signature because any cached audio is no longer reusable.
    /// </remarks>
    public VoiceInfo? SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            if (SetProperty(ref _selectedVoice, value))
            {
                _preparedSignature = null;
                OnPropertyChanged(nameof(SelectedVoiceLanguage), nameof(SelectedVoiceGender));
                PersistSettings();
            }
        }
    }

    public string SelectedVoiceLanguage => SelectedVoice?.LanguageDisplay ?? "No voice selected";

    public string SelectedVoiceGender => string.IsNullOrWhiteSpace(SelectedVoice?.Gender)
        ? string.Empty
        : SelectedVoice!.Gender!;

    /// <summary>
    /// Gets or sets the source text that will be synthesized and used as the comparison target for scoring.
    /// </summary>
    /// <remarks>
    /// Updating the source text is a session reset boundary. The current prepared synthesis becomes invalid, playback
    /// returns to idle, the waveform is cleared, and the current recognition transcript is reset because it no longer
    /// corresponds to the same source material.
    /// </remarks>
    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (SetProperty(ref _sourceText, value))
            {
                _preparedSignature = null;
                _preparedSynthesis = null;
                Playback = PlaybackState.Idle;
                WaveformSamples = Array.Empty<float>();
                ClearRecognizedTextState();
                RequestRecognitionRefresh();
                StatusMessage = string.IsNullOrWhiteSpace(value)
                    ? "Enter text to start a shadowing session."
                    : "Text updated. Press Play to prepare fresh audio.";
                NotifyCommands();
            }
        }
    }

    public string RecognizedText
    {
        get => _recognizedText;
        private set => SetProperty(ref _recognizedText, value);
    }

    public double SpeechRate
    {
        get => _speechRate;
        set
        {
            var clamped = Math.Clamp(value, 0.5, 2.0);
            if (SetProperty(ref _speechRate, clamped))
            {
                _preparedSignature = null;
                OnPropertyChanged(nameof(SpeechRateDisplay));
                PersistSettings();
            }
        }
    }

    public string SpeechRateDisplay => $"{SpeechRate:0.0}x";

    public double SpeechPitch
    {
        get => _speechPitch;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 2.0);
            if (SetProperty(ref _speechPitch, clamped))
            {
                _preparedSignature = null;
                OnPropertyChanged(nameof(SpeechPitchDisplay));
                PersistSettings();
            }
        }
    }

    public string SpeechPitchDisplay => $"{SpeechPitch:0.00}x";

    public double SpeechVolume
    {
        get => _speechVolume;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetProperty(ref _speechVolume, clamped))
            {
                _preparedSignature = null;
                OnPropertyChanged(nameof(SpeechVolumeDisplay));
                PersistSettings();
            }
        }
    }

    public string SpeechVolumeDisplay => $"{SpeechVolume * 100:0}%";

    public string SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            var normalized = NormalizeThemeOption(value);
            var changed = SetProperty(ref _selectedThemeOption, normalized);
            _themeService.ApplyTheme(MapThemeOption(normalized));

            if (changed)
            {
                PersistSettings();
            }
        }
    }

    public IReadOnlyList<float> WaveformSamples
    {
        get => _waveformSamples;
        private set => SetProperty(ref _waveformSamples, value);
    }

    /// <summary>
    /// Gets the latest immutable playback snapshot applied to the UI.
    /// </summary>
    public PlaybackState Playback
    {
        get => _playback;
        private set
        {
            if (SetProperty(ref _playback, value))
            {
                OnPropertyChanged(
                    nameof(CurrentPlaybackStatus),
                    nameof(PositionSeconds),
                    nameof(DurationSeconds),
                    nameof(PositionText),
                    nameof(DurationText),
                    nameof(ProgressMaximum),
                    nameof(ProgressRatio),
                    nameof(IsPlaying),
                    nameof(IsPaused),
                    nameof(CanSeek),
                    nameof(CanEditSourceText));
                NotifyCommands();
            }
        }
    }

    public double PositionSeconds => Playback.Position.TotalSeconds;

    public double DurationSeconds => Playback.Duration.TotalSeconds;

    public double ProgressMaximum => DurationSeconds <= 0 ? 1 : DurationSeconds;

    public double ProgressRatio => DurationSeconds <= 0 ? 0 : PositionSeconds / DurationSeconds;

    public string PositionText => TimeSpan.FromSeconds(PositionSeconds).ToString(@"mm\:ss");

    public string DurationText => TimeSpan.FromSeconds(DurationSeconds).ToString(@"mm\:ss");

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string RecognitionAvailabilityText
    {
        get => _recognitionAvailabilityText;
        private set => SetProperty(ref _recognitionAvailabilityText, value);
    }

    public bool ShowRecognitionSettingsButtons => _engine.Recognition.IsAvailable;

    /// <summary>
    /// Gets a value indicating whether dictation is currently enabled as a user preference.
    /// </summary>
    /// <remarks>
    /// This flag represents the desired microphone state, not a guarantee that recognition is actively listening.
    /// </remarks>
    public bool IsDictationEnabled
    {
        get => _isDictationEnabled;
        private set
        {
            if (SetProperty(ref _isDictationEnabled, value))
            {
                OnPropertyChanged(nameof(DictationButtonText), nameof(CanToggleDictation));
                RecognitionAvailabilityText = BuildRecognitionAvailabilityText(CurrentRecognitionStatus, null);
                PersistSettings();
                NotifyCommands();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanEditSourceText));
                NotifyCommands();
            }
        }
    }

    public PlaybackStatus CurrentPlaybackStatus => Playback.Status;

    public RecognitionStatus CurrentRecognitionStatus
    {
        get => _currentRecognitionStatus;
        private set
        {
            if (SetProperty(ref _currentRecognitionStatus, value))
            {
                OnPropertyChanged(nameof(IsRecognizing), nameof(CanToggleDictation));
                NotifyCommands();
            }
        }
    }

    public bool IsPlaying => CurrentPlaybackStatus == PlaybackStatus.Playing;

    public bool IsPaused => CurrentPlaybackStatus == PlaybackStatus.Paused;

    public bool IsRecognizing => CurrentRecognitionStatus is RecognitionStatus.Listening or RecognitionStatus.Starting;

    public string DictationButtonText => IsDictationEnabled ? "Dictation: On" : "Dictation: Off";

    public bool CanToggleDictation => _engine.Recognition.IsAvailable
        && CurrentRecognitionStatus is not (RecognitionStatus.Starting or RecognitionStatus.Stopping);

    public bool CanSeek => Capabilities.SupportsSeek && DurationSeconds > 0 && Playback.CanSeek;

    public bool CanEditSourceText => !IsBusy
        && CurrentPlaybackStatus is not (PlaybackStatus.Playing or PlaybackStatus.Paused);

    public int? ShadowingScore
    {
        get => _shadowingScore;
        private set
        {
            if (SetProperty(ref _shadowingScore, value))
            {
                OnPropertyChanged(nameof(ScoreText));
            }
        }
    }

    public string ScoreText => ShadowingScore?.ToString() ?? "?";

    public string ScoreColorHex
    {
        get => _scoreColorHex;
        private set => SetProperty(ref _scoreColorHex, value);
    }

    public string ShadowingSummary
    {
        get => _shadowingSummary;
        private set => SetProperty(ref _shadowingSummary, value);
    }

    /// <summary>
    /// Lazily initializes the session model by loading persisted settings, engine capabilities, and available voices.
    /// </summary>
    /// <remarks>
    /// The public entry point simply routes the operation through the serialized session queue so initialization cannot
    /// race with playback or recognition updates.
    /// </remarks>
    public Task InitializeAsync() => EnqueueSessionAsync(InitializeCoreAsync);

    /// <summary>
    /// Starts playback or resumes it from pause when a compatible prepared synthesis already exists.
    /// </summary>
    public Task PlayAsync() => EnqueueSessionAsync(PlayCoreAsync);

    /// <summary>
    /// Pauses playback when the active engine supports pause.
    /// </summary>
    public Task PauseAsync() => EnqueueSessionAsync(PauseCoreAsync);

    /// <summary>
    /// Stops playback and rewinds the prepared media to the beginning.
    /// </summary>
    public Task StopAsync() => EnqueueSessionAsync(StopCoreAsync);

    /// <summary>
    /// Seeks playback to the beginning without discarding the current prepared synthesis.
    /// </summary>
    public Task RewindAsync() => EnqueueSessionAsync(RewindCoreAsync);

    /// <summary>
    /// Fully resets the current shadowing session.
    /// </summary>
    public Task ResetAsync() => EnqueueSessionAsync(ResetCoreAsync);

    /// <summary>
    /// Seeks playback to the requested position in seconds.
    /// </summary>
    /// <param name="positionSeconds">The target playback position expressed in seconds.</param>
    public Task SeekAsync(double positionSeconds) => EnqueueSessionAsync(() => SeekCoreAsync(positionSeconds));

    /// <summary>
    /// Performs the one-time initialization workflow behind <see cref="InitializeAsync"/>.
    /// </summary>
    private async Task InitializeCoreAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await RunOnUiAsync(() =>
        {
            IsBusy = true;
            StatusMessage = "Loading voices and engine capabilities...";
            Capabilities = _engine.Capabilities;
            SelectedEngineName = _engine.Name;
        }).ConfigureAwait(false);

        try
        {
            var settings = await _settingsStore.LoadAsync().ConfigureAwait(false);
            await RunOnUiAsync(() =>
            {
                SelectedThemeOption = MapThemePreferenceToOption(settings.ThemePreference);
                SpeechRate = settings.SpeechRate is >= 0.5 and <= 2.0 ? settings.SpeechRate : 1.0;
                SpeechPitch = settings.SpeechPitch is >= 0.0 and <= 2.0 ? settings.SpeechPitch : 1.0;
                SpeechVolume = settings.SpeechVolume is >= 0.0 and <= 1.0 ? settings.SpeechVolume : 1.0;
                IsDictationEnabled = settings.IsDictationEnabled && _engine.Recognition.IsAvailable;
            }).ConfigureAwait(false);

            var voices = await _engine.VoiceCatalog.GetVoicesAsync().ConfigureAwait(false);
            await RunOnUiAsync(() =>
            {
                _voices.Clear();
                foreach (var voice in voices.OrderBy(v => v.DisplayName))
                {
                    _voices.Add(voice);
                }

                SelectedVoice = _voices.FirstOrDefault(v => v.Id == settings.PreferredVoiceId)
                    ?? _voices.FirstOrDefault(v => v.IsDefault)
                    ?? _voices.FirstOrDefault();
                RecognitionAvailabilityText = BuildRecognitionAvailabilityText(CurrentRecognitionStatus, null);
                StatusMessage = _voices.Count == 0
                    ? "No voices were found."
                    : "Choose a voice, enter text, and start playback.";
                _isInitialized = true;
                InitializeCommand.NotifyCanExecuteChanged();
            }).ConfigureAwait(false);

            if (IsDictationEnabled)
            {
                await EnsureDictationMatchesPreferenceAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs the playback start/resume workflow behind <see cref="PlayAsync"/>.
    /// </summary>
    /// <remarks>
    /// The method decides whether it can resume from pause or must generate fresh audio. That decision is based on a
    /// signature built from source text, selected voice, and synthesis settings. If any of those inputs changed, the
    /// cached synthesis is discarded and replaced before playback starts.
    /// </remarks>
    private async Task PlayCoreAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceText))
        {
            await RunOnUiAsync(() => StatusMessage = "Enter the source text first.").ConfigureAwait(false);
            return;
        }

        if (!_isInitialized)
        {
            await InitializeCoreAsync().ConfigureAwait(false);
        }

        if (CurrentPlaybackStatus == PlaybackStatus.Playing)
        {
            return;
        }

        var requestSignature = BuildRequestSignature();
        var isResume = requestSignature == _preparedSignature && CurrentPlaybackStatus == PlaybackStatus.Paused;
        var requiresPreparation = requestSignature != _preparedSignature || _preparedSynthesis is null;

        await RunOnUiAsync(() => IsBusy = true).ConfigureAwait(false);
        try
        {
            if (!isResume)
            {
                await ClearRecognizedCoreAsync().ConfigureAwait(false);
            }

            if (requiresPreparation)
            {
                await RunOnUiAsync(() => StatusMessage = "Generating speech output...").ConfigureAwait(false);
                var request = new SpeechSynthesisRequest(SourceText.Trim(), SelectedVoice, SpeechRate, SpeechPitch, SpeechVolume);
                _preparedSynthesis = await _engine.TextToSpeech.PrepareAsync(request).ConfigureAwait(false);
                _preparedSignature = requestSignature;
                await _engine.Playback.LoadAsync(_preparedSynthesis).ConfigureAwait(false);
                await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
                await RunOnUiAsync(() => ApplySynthesisResult(_preparedSynthesis)).ConfigureAwait(false);
            }

            await _engine.Playback.PlayAsync().ConfigureAwait(false);
            await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
            await RunOnUiAsync(() => StatusMessage = _engine.Recognition.IsAvailable && IsDictationEnabled
                ? "Playback is running. Dictation stays active independently."
                : "Playback is running.").ConfigureAwait(false);
            PersistSettings();
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false).ConfigureAwait(false);
        }
    }

    private async Task PauseCoreAsync()
    {
        if (CurrentPlaybackStatus != PlaybackStatus.Playing || !Capabilities.SupportsPause)
        {
            return;
        }

        await RunOnUiAsync(() => IsBusy = true).ConfigureAwait(false);
        try
        {
            await _engine.Playback.PauseAsync().ConfigureAwait(false);
            await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
            await RunOnUiAsync(() => StatusMessage = "Playback is paused.").ConfigureAwait(false);
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false).ConfigureAwait(false);
        }
    }

    private async Task StopCoreAsync()
    {
        if (CurrentPlaybackStatus is not (PlaybackStatus.Playing or PlaybackStatus.Paused))
        {
            return;
        }

        await RunOnUiAsync(() => IsBusy = true).ConfigureAwait(false);
        try
        {
            await _engine.Playback.StopAsync().ConfigureAwait(false);
            await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
            await RunOnUiAsync(() => StatusMessage = "Playback stopped.").ConfigureAwait(false);
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false).ConfigureAwait(false);
        }
    }

    private async Task RewindCoreAsync()
    {
        if (!CanSeek)
        {
            return;
        }

        await RunOnUiAsync(() => IsBusy = true).ConfigureAwait(false);
        try
        {
            await _engine.Playback.SeekAsync(TimeSpan.Zero).ConfigureAwait(false);
            await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
            await RunOnUiAsync(() => StatusMessage = "Position reset to the beginning.").ConfigureAwait(false);
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs the strong reset workflow behind <see cref="ResetAsync"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="StopCoreAsync"/>, reset discards cached synthesis and clears the editable source text so the
    /// next session begins from a fully clean baseline.
    /// </remarks>
    private async Task ResetCoreAsync()
    {
        await RunOnUiAsync(() => IsBusy = true).ConfigureAwait(false);
        try
        {
            await _engine.Playback.ResetAsync().ConfigureAwait(false);
            await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
            _preparedSynthesis = null;
            _preparedSignature = null;
            await RunOnUiAsync(() =>
            {
                SourceText = string.Empty;
                StatusMessage = "Everything was reset.";
            }).ConfigureAwait(false);
        }
        finally
        {
            await RunOnUiAsync(() => IsBusy = false).ConfigureAwait(false);
        }
    }

    private async Task SeekCoreAsync(double positionSeconds)
    {
        if (!CanSeek)
        {
            return;
        }

        var target = TimeSpan.FromSeconds(Math.Clamp(positionSeconds, 0, DurationSeconds));
        await _engine.Playback.SeekAsync(target).ConfigureAwait(false);
        await ApplyPlaybackSnapshotAsync(_engine.Playback.CurrentState).ConfigureAwait(false);
    }

    private void ApplySynthesisResult(SpeechSynthesisResult synthesisResult)
    {
        WaveformSamples = synthesisResult.Waveform.Samples;
        StatusMessage = synthesisResult.IsEstimated
            ? "Audio prepared in estimated mode."
            : "Audio prepared.";
    }

    private Task ToggleDictationAsync() => EnqueueSessionAsync(ToggleDictationCoreAsync);

    private Task ClearRecognizedAsync() => EnqueueSessionAsync(ClearRecognizedCoreAsync);

    /// <summary>
    /// Performs the dictation toggle workflow behind <see cref="ToggleDictationAsync"/>.
    /// </summary>
    /// <remarks>
    /// Dictation is managed independently from playback transport. Enabling playback does not automatically start the
    /// microphone; enabling dictation expresses a persistent preference that the view model then reconciles with the
    /// current recognition service state.
    /// </remarks>
    private async Task ToggleDictationCoreAsync()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        if (IsDictationEnabled)
        {
            await RunOnUiAsync(() => IsDictationEnabled = false).ConfigureAwait(false);
            Interlocked.Increment(ref _recognitionRefreshVersion);
            await StopDictationIfRunningAsync().ConfigureAwait(false);
            await RunOnUiAsync(() => StatusMessage = "Dictation turned off.").ConfigureAwait(false);
            return;
        }

        await RunOnUiAsync(() => IsDictationEnabled = true).ConfigureAwait(false);
        Interlocked.Increment(ref _recognitionRefreshVersion);
        await EnsureDictationMatchesPreferenceAsync().ConfigureAwait(false);
        await RunOnUiAsync(() => StatusMessage = CurrentRecognitionStatus == RecognitionStatus.Listening
            ? "Dictation is on."
            : "Dictation is enabled and the microphone is starting.").ConfigureAwait(false);
    }

    private async Task ClearRecognizedCoreAsync()
    {
        await RunOnUiAsync(ClearRecognizedTextState).ConfigureAwait(false);

        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        var refreshVersion = Interlocked.Increment(ref _recognitionRefreshVersion);
        await RefreshRecognitionAsync(refreshVersion).ConfigureAwait(false);
    }

    private async Task EnsureDictationMatchesPreferenceAsync()
    {
        if (!_engine.Recognition.IsAvailable || !IsDictationEnabled)
        {
            return;
        }

        if (_engine.Recognition.CurrentStatus is RecognitionStatus.Listening or RecognitionStatus.Starting)
        {
            return;
        }

        await _engine.Recognition.StartAsync().ConfigureAwait(false);
    }

    private async Task StopDictationIfRunningAsync()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        if (_engine.Recognition.CurrentStatus is RecognitionStatus.Listening or RecognitionStatus.Starting or RecognitionStatus.Stopping)
        {
            await _engine.Recognition.StopAsync().ConfigureAwait(false);
        }
    }

    private void RequestRecognitionRefresh()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        var refreshVersion = Interlocked.Increment(ref _recognitionRefreshVersion);
        _ = EnqueueSessionAsync(() => RefreshRecognitionAsync(refreshVersion));
    }

    /// <summary>
    /// Resets recognition state and restarts listening when the latest preference still requires it.
    /// </summary>
    /// <param name="refreshVersion">The captured refresh version that must still match the latest request.</param>
    private async Task RefreshRecognitionAsync(int refreshVersion)
    {
        if (refreshVersion != Volatile.Read(ref _recognitionRefreshVersion) || !_engine.Recognition.IsAvailable)
        {
            return;
        }

        await _engine.Recognition.ResetAsync().ConfigureAwait(false);

        if (refreshVersion != Volatile.Read(ref _recognitionRefreshVersion) || !IsDictationEnabled)
        {
            return;
        }

        await EnsureDictationMatchesPreferenceAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the supplied action on the application's main UI thread.
    /// </summary>
    /// <param name="action">The UI-bound action to execute.</param>
    private Task RunOnUiAsync(Action action)
    {
        return _dispatcher.RunAsync(action);
    }

    private async Task OpenSpeechPrivacySettingsAsync()
    {
        await _settingsLauncher.OpenAsync("ms-settings:privacy-speech").ConfigureAwait(false);
    }

    private async Task OpenMicrophonePrivacySettingsAsync()
    {
        await _settingsLauncher.OpenAsync("ms-settings:privacy-microphone").ConfigureAwait(false);
    }

    /// <summary>
    /// Clears transcript-derived UI state without touching recognition service internals.
    /// </summary>
    private void ClearRecognizedTextState()
    {
        RecognizedText = string.Empty;
        ShadowingScore = null;
        ShadowingSummary = "The score will appear after recognition starts.";
        ScoreColorHex = "#A0A7B8";
    }

    /// <summary>
    /// Builds a cache key for the current synthesis inputs.
    /// </summary>
    /// <returns>A stable string signature that changes whenever synthesis-relevant inputs change.</returns>
    private string BuildRequestSignature()
    {
        return string.Join(
            "|",
            SourceText.Trim(),
            SelectedVoice?.Id ?? string.Empty,
            SpeechRate.ToString("0.00", CultureInfo.InvariantCulture),
            SpeechPitch.ToString("0.00", CultureInfo.InvariantCulture),
            SpeechVolume.ToString("0.00", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Recomputes the current shadowing assessment from the source and recognized text.
    /// </summary>
    private void UpdateAssessment()
    {
        var assessment = _analyzer.Assess(SourceText, RecognizedText);
        ShadowingScore = assessment.Score;
        ShadowingSummary = assessment.Summary;
        ScoreColorHex = ComputeScoreColor(assessment.Score);
    }

    /// <summary>
    /// Maps a score to a red-to-green color used by the transcript score badge.
    /// </summary>
    /// <param name="score">The score to convert.</param>
    /// <returns>A hexadecimal RGB color string.</returns>
    private static string ComputeScoreColor(int? score)
    {
        if (score is null)
        {
            return "#A0A7B8";
        }

        var ratio = Math.Clamp(score.Value / 100d, 0, 1);
        var red = (byte)Math.Round(235 - (143 * ratio));
        var green = (byte)Math.Round(92 + (110 * ratio));
        var blue = (byte)Math.Round(99 - (34 * ratio));
        return $"#{red:X2}{green:X2}{blue:X2}";
    }

    /// <summary>
    /// Persists the latest user-facing settings snapshot when initialization already completed.
    /// </summary>
    /// <remarks>
    /// The method intentionally does nothing before initialization so the initial settings load does not immediately
    /// write the same values back to storage while the first snapshot is still being applied.
    /// </remarks>
    private void PersistSettings()
    {
        if (!_isInitialized)
        {
            return;
        }

        _ = _settingsStore.SaveAsync(new ShadowingSettings(
            SelectedVoice?.Id,
            SpeechRate,
            SpeechPitch,
            SpeechVolume,
            IsDictationEnabled,
            MapThemeOption(SelectedThemeOption)));
    }

    private void NotifyCommands()
    {
        InitializeCommand.NotifyCanExecuteChanged();
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RewindCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        ToggleDictationCommand.NotifyCanExecuteChanged();
        OpenSpeechPrivacySettingsCommand.NotifyCanExecuteChanged();
        OpenMicrophonePrivacySettingsCommand.NotifyCanExecuteChanged();
        ClearRecognizedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Enqueues a unit of session work so it runs in-order with every other command or callback mutation.
    /// </summary>
    /// <param name="workItem">The asynchronous work item to serialize.</param>
    private Task EnqueueSessionAsync(Func<Task> workItem)
    {
        return _sessionQueue.Enqueue(workItem);
    }

    private Task ApplyPlaybackSnapshotAsync(PlaybackState state)
    {
        return RunOnUiAsync(() => ApplyPlaybackSnapshot(state));
    }

    private void ApplyPlaybackSnapshot(PlaybackState state)
    {
        Playback = state;
        if (!string.IsNullOrWhiteSpace(state.Message)
            && state.Status is PlaybackStatus.Ready or PlaybackStatus.Completed or PlaybackStatus.Error)
        {
            StatusMessage = state.Message;
        }
    }

    /// <summary>
    /// Receives playback state updates from the engine and serializes them into the main session flow.
    /// </summary>
    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        _ = EnqueueSessionAsync(() => ApplyPlaybackSnapshotAsync(e.State));
    }

    /// <summary>
    /// Receives recognition lifecycle updates from the engine and applies them on the UI thread in queue order.
    /// </summary>
    private void OnRecognitionStateChanged(object? sender, RecognitionStateChangedEventArgs e)
    {
        _ = EnqueueSessionAsync(() => RunOnUiAsync(() =>
        {
            CurrentRecognitionStatus = e.Status;
            RecognitionAvailabilityText = BuildRecognitionAvailabilityText(e.Status, e.Message);
        }));
    }

    /// <summary>
    /// Receives recognition transcript updates and recomputes the shadowing assessment.
    /// </summary>
    private void OnRecognitionUpdated(object? sender, RecognitionUpdatedEventArgs e)
    {
        _ = EnqueueSessionAsync(() => RunOnUiAsync(() =>
        {
            RecognizedText = e.Update.FullText;
            UpdateAssessment();
        }));
    }

    /// <summary>
    /// Builds the user-facing recognition availability/status text from the current lifecycle state.
    /// </summary>
    /// <param name="status">The recognition lifecycle status.</param>
    /// <param name="message">An optional engine-provided diagnostic or status message.</param>
    /// <returns>A short user-facing description suitable for the recognition controls area.</returns>
    private string BuildRecognitionAvailabilityText(RecognitionStatus status, string? message)
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return "Speech recognition is not available on this platform";
        }

        return status switch
        {
            RecognitionStatus.Starting => string.IsNullOrWhiteSpace(message)
                ? "Requesting microphone access and initializing speech recognition..."
                : message,
            RecognitionStatus.Listening => string.IsNullOrWhiteSpace(message)
                ? "Microphone is listening."
                : message,
            RecognitionStatus.Stopping => string.IsNullOrWhiteSpace(message)
                ? "Stopping recognition..."
                : message,
            RecognitionStatus.Completed => string.IsNullOrWhiteSpace(message)
                ? "Recognition stopped."
                : message,
            RecognitionStatus.Error => string.IsNullOrWhiteSpace(message)
                ? "Speech recognition is unavailable."
                : message,
            _ => IsDictationEnabled ? "Dictation is enabled, but the microphone is idle." : "Dictation is off."
        };
    }

    private static string NormalizeThemeOption(string? option)
    {
        return option switch
        {
            LightThemeOption => LightThemeOption,
            DarkThemeOption => DarkThemeOption,
            _ => AutomaticThemeOption
        };
    }

    private static ThemePreferenceMode MapThemeOption(string option)
    {
        return option switch
        {
            LightThemeOption => ThemePreferenceMode.Light,
            DarkThemeOption => ThemePreferenceMode.Dark,
            _ => ThemePreferenceMode.System
        };
    }

    private static string MapThemePreferenceToOption(ThemePreferenceMode preference)
    {
        return preference switch
        {
            ThemePreferenceMode.Light => LightThemeOption,
            ThemePreferenceMode.Dark => DarkThemeOption,
            _ => AutomaticThemeOption
        };
    }
}





