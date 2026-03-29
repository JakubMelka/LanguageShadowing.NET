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
/// This is the central coordinator of the whole app. It does not synthesize speech itself, it does not play audio
/// itself, and it does not recognize speech itself. Instead, it tells the individual services when to do their work
/// and then translates their results into UI-friendly properties.
/// </para>
/// <para>
/// The hardest part of this type is not data binding; it is asynchronous orchestration. A single user action like
/// pressing Play fans out into several independent asynchronous operations:
/// </para>
/// <list type="number">
/// <item><description>the view model may need to initialize settings and available voices,</description></item>
/// <item><description>it may need to synthesize fresh audio,</description></item>
/// <item><description>it then starts playback,</description></item>
/// <item><description>and separately it may start or stop dictation based on the dedicated microphone toggle.</description></item>
/// </list>
/// <para>
/// Meanwhile, playback and recognition continue to publish their own events from outside the command flow. Those events
/// may arrive later, on other threads, and in a different order than the original button click that triggered them.
/// </para>
/// <para>
/// To keep that manageable, the class uses two important coordination mechanisms:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IAppDispatcher"/> folds external callbacks back onto the UI thread before bindable properties are changed.</description></item>
/// <item><description>Playback orchestration and dictation orchestration are handled independently so microphone lifetime is no longer tied to transport buttons.</description></item>
/// </list>
/// <para>
/// In short: this class is the place where user intent, long-running tasks, and event-driven platform services are made
/// to behave as one coherent session model.
/// </para>
/// </remarks>
public sealed class MainViewModel : ObservableObject
{
    private const string AutomaticThemeOption = "Automatic";
    private const string LightThemeOption = "Light";
    private const string DarkThemeOption = "Dark";

    private static readonly IReadOnlyList<string> AvailableThemeOptions = new[]
    {
        AutomaticThemeOption,
        LightThemeOption,
        DarkThemeOption
    };

    private readonly ISpeechEngine _engine;
    private readonly IShadowingSettingsStore _settingsStore;
    private readonly IShadowingAnalyzer _analyzer;
    private readonly IAppDispatcher _dispatcher;
    private readonly IAppThemeService _themeService;
    private readonly ISettingsLauncher _settingsLauncher;
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
    private double _positionSeconds;
    private double _durationSeconds;
    private string _statusMessage = "Ready.";
    private string _recognitionAvailabilityText = "Dictation is off.";
    private string _shadowingSummary = "The score will appear after recognition starts.";
    private string _scoreColorHex = "#A0A7B8";
    private int? _shadowingScore;
    private PlaybackStatus _currentPlaybackStatus = PlaybackStatus.Idle;
    private RecognitionStatus _currentRecognitionStatus = RecognitionStatus.Idle;

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

    public ReadOnlyObservableCollection<VoiceInfo> Voices { get; }

    public ReadOnlyObservableCollection<string> Engines { get; }

    public IReadOnlyList<string> ThemeOptions { get; }

    public AsyncRelayCommand InitializeCommand { get; }

    public AsyncRelayCommand PlayCommand { get; }

    public AsyncRelayCommand PauseCommand { get; }

    public AsyncRelayCommand StopCommand { get; }

    public AsyncRelayCommand RewindCommand { get; }

    public AsyncRelayCommand ResetCommand { get; }

    public AsyncRelayCommand ToggleDictationCommand { get; }

    public AsyncRelayCommand OpenSpeechPrivacySettingsCommand { get; }

    public AsyncRelayCommand OpenMicrophonePrivacySettingsCommand { get; }

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
        ? ""
        : SelectedVoice!.Gender!;

    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (SetProperty(ref _sourceText, value))
            {
                _preparedSignature = null;
                _preparedSynthesis = null;
                PositionSeconds = 0;
                DurationSeconds = 0;
                WaveformSamples = Array.Empty<float>();
                ClearRecognizedTextState();
                RequestRecognizedTranscriptClear();
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

    public double PositionSeconds
    {
        get => _positionSeconds;
        private set
        {
            if (SetProperty(ref _positionSeconds, value))
            {
                OnPropertyChanged(nameof(PositionText), nameof(ProgressRatio));
            }
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        private set
        {
            if (SetProperty(ref _durationSeconds, value))
            {
                OnPropertyChanged(nameof(DurationText), nameof(ProgressMaximum), nameof(ProgressRatio), nameof(CanSeek));
            }
        }
    }

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

    public PlaybackStatus CurrentPlaybackStatus
    {
        get => _currentPlaybackStatus;
        private set
        {
            if (SetProperty(ref _currentPlaybackStatus, value))
            {
                OnPropertyChanged(nameof(IsPlaying), nameof(IsPaused), nameof(CanEditSourceText));
                NotifyCommands();
            }
        }
    }

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

    public bool IsRecognizing => CurrentRecognitionStatus == RecognitionStatus.Listening || CurrentRecognitionStatus == RecognitionStatus.Starting;

    public string DictationButtonText => IsDictationEnabled ? "Dictation: On" : "Dictation: Off";

    public bool CanToggleDictation => _engine.Recognition.IsAvailable
        && CurrentRecognitionStatus is not (RecognitionStatus.Starting or RecognitionStatus.Stopping);

    public bool CanSeek => Capabilities.SupportsSeek && DurationSeconds > 0;

    public bool CanEditSourceText => !IsBusy
        && _engine.Playback.CurrentState.Status is not (PlaybackStatus.Playing or PlaybackStatus.Paused);

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
    /// Loads persisted settings, engine capabilities, and available voices on first use.
    /// </summary>
    /// <remarks>
    /// This method is intentionally lazy. The application waits until the page is shown or the user starts interacting
    /// with playback before touching platform services. That keeps construction cheap and avoids doing unnecessary work
    /// when the app has not fully appeared yet.
    /// </remarks>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Loading voices and engine capabilities...";

        try
        {
            Capabilities = _engine.Capabilities;
            SelectedEngineName = _engine.Name;
            var settings = await _settingsStore.LoadAsync().ConfigureAwait(false);
            SelectedThemeOption = MapThemePreferenceToOption(settings.ThemePreference);
            SpeechRate = settings.SpeechRate is >= 0.5 and <= 2.0 ? settings.SpeechRate : 1.0;
            SpeechPitch = settings.SpeechPitch is >= 0.0 and <= 2.0 ? settings.SpeechPitch : 1.0;
            SpeechVolume = settings.SpeechVolume is >= 0.0 and <= 1.0 ? settings.SpeechVolume : 1.0;
            IsDictationEnabled = settings.IsDictationEnabled && _engine.Recognition.IsAvailable;

            var voices = await _engine.VoiceCatalog.GetVoicesAsync().ConfigureAwait(false);
            await _dispatcher.RunAsync(() =>
            {
                _voices.Clear();
                foreach (var voice in voices.OrderBy(v => v.DisplayName))
                {
                    _voices.Add(voice);
                }

                SelectedVoice = _voices.FirstOrDefault(v => v.Id == settings.PreferredVoiceId)
                    ?? _voices.FirstOrDefault(v => v.IsDefault)
                    ?? _voices.FirstOrDefault();
            }).ConfigureAwait(false);

            RecognitionAvailabilityText = BuildRecognitionAvailabilityText(CurrentRecognitionStatus, null);

            StatusMessage = _voices.Count == 0
                ? "No voices were found."
                : "Choose a voice, enter text, and start playback.";
            _isInitialized = true;
            InitializeCommand.NotifyCanExecuteChanged();

            if (IsDictationEnabled)
            {
                await EnsureDictationMatchesPreferenceAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Starts or resumes a shadowing session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is the most important async workflow in the application.
    /// </para>
    /// <para>
    /// It does not simply call "play" on a media service. Instead it has to decide whether the current request means
    /// "resume paused playback" or "prepare completely new audio". That decision is based on a signature derived from
    /// the current text, voice, rate, pitch, and volume. If any of those changed, the previous prepared synthesis is no
    /// longer valid and must be replaced.
    /// </para>
    /// <para>
    /// Dictation is no longer started from Play. Playback should be free to start immediately while the microphone
    /// follows its own dedicated toggle and persisted preference.
    /// </para>
    /// </remarks>
    public async Task PlayAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceText))
        {
            StatusMessage = "Enter the source text first.";
            return;
        }

        if (!_isInitialized)
        {
            await InitializeAsync().ConfigureAwait(false);
        }

        var requestSignature = BuildRequestSignature();
        var isResume = requestSignature == _preparedSignature && CurrentPlaybackStatus == PlaybackStatus.Paused;
        var requiresPreparation = requestSignature != _preparedSignature || _preparedSynthesis is null;

        IsBusy = true;
        try
        {
            if (!isResume)
            {
                await ClearRecognizedAsync().ConfigureAwait(false);
            }

            if (requiresPreparation)
            {
                StatusMessage = "Generating speech output...";
                var request = new SpeechSynthesisRequest(SourceText.Trim(), SelectedVoice, SpeechRate, SpeechPitch, SpeechVolume);
                _preparedSynthesis = await _engine.TextToSpeech.PrepareAsync(request).ConfigureAwait(false);
                _preparedSignature = requestSignature;
                await _engine.Playback.LoadAsync(_preparedSynthesis).ConfigureAwait(false);
                ApplySynthesisResult(_preparedSynthesis);
            }

            await _engine.Playback.PlayAsync().ConfigureAwait(false);
            StatusMessage = _engine.Recognition.IsAvailable && IsDictationEnabled
                ? "Playback is running. Dictation stays active independently."
                : "Playback is running.";
            PersistSettings();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Pauses playback without affecting the independent dictation toggle.
    /// </summary>
    public async Task PauseAsync()
    {
        IsBusy = true;
        try
        {
            await _engine.Playback.PauseAsync().ConfigureAwait(false);
            StatusMessage = "Playback is paused.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Stops playback and rewinds to the beginning without changing dictation state.
    /// </summary>
    public async Task StopAsync()
    {
        IsBusy = true;
        try
        {
            await _engine.Playback.StopAsync().ConfigureAwait(false);
            StatusMessage = "Playback stopped.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Seeks the prepared audio back to the beginning.
    /// </summary>
    public async Task RewindAsync()
    {
        if (!CanSeek)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _engine.Playback.SeekAsync(TimeSpan.Zero).ConfigureAwait(false);
            StatusMessage = "Position reset to the beginning.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears playback, transcript state, prepared synthesis, and editable text.
    /// </summary>
    /// <remarks>
    /// Reset is stronger than Stop. It not only ends the current session but also discards cached synthesis artifacts and
    /// transcript state so the next Play command starts from a completely fresh baseline.
    /// </remarks>
    public async Task ResetAsync()
    {
        IsBusy = true;
        try
        {
            await _engine.Playback.ResetAsync().ConfigureAwait(false);
            await ClearRecognizedAsync().ConfigureAwait(false);
            _preparedSynthesis = null;
            _preparedSignature = null;
            SourceText = string.Empty;
            StatusMessage = "Everything was reset.";

            if (IsDictationEnabled)
            {
                await EnsureDictationMatchesPreferenceAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Seeks playback to the requested position in seconds.
    /// </summary>
    /// <remarks>
    /// The public API uses seconds because that is the natural unit for the UI slider. The playback service itself uses
    /// <see cref="TimeSpan"/>, so this method is the conversion point between view-friendly and service-friendly units.
    /// </remarks>
    public async Task SeekAsync(double positionSeconds)
    {
        if (!CanSeek)
        {
            return;
        }

        var target = TimeSpan.FromSeconds(Math.Clamp(positionSeconds, 0, DurationSeconds));
        await _engine.Playback.SeekAsync(target).ConfigureAwait(false);
    }

    private void ApplySynthesisResult(SpeechSynthesisResult synthesisResult)
    {
        PositionSeconds = 0;
        DurationSeconds = Math.Max(0, synthesisResult.Duration.TotalSeconds);
        WaveformSamples = synthesisResult.Waveform.Samples;
        StatusMessage = synthesisResult.IsEstimated
            ? "Audio prepared in estimated mode."
            : "Audio prepared.";
    }

    private async Task ToggleDictationAsync()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        if (IsDictationEnabled)
        {
            IsDictationEnabled = false;
            await StopDictationIfRunningAsync().ConfigureAwait(false);
            StatusMessage = "Dictation turned off.";
            return;
        }

        IsDictationEnabled = true;
        await EnsureDictationMatchesPreferenceAsync().ConfigureAwait(false);
        StatusMessage = CurrentRecognitionStatus == RecognitionStatus.Listening
            ? "Dictation is on."
            : "Dictation is enabled and the microphone is starting.";
    }

    private async Task ClearRecognizedAsync()
    {
        ClearRecognizedTextState();

        if (_engine.Recognition.IsAvailable)
        {
            await _engine.Recognition.ClearTranscriptAsync().ConfigureAwait(false);
        }
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

    private void RequestRecognizedTranscriptClear()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        _ = _engine.Recognition.ClearTranscriptAsync();
    }

    private async Task OpenSpeechPrivacySettingsAsync()
    {
        await _settingsLauncher.OpenAsync("ms-settings:privacy-speech").ConfigureAwait(false);
    }

    private async Task OpenMicrophonePrivacySettingsAsync()
    {
        await _settingsLauncher.OpenAsync("ms-settings:privacy-microphone").ConfigureAwait(false);
    }

    private void ClearRecognizedTextState()
    {
        RecognizedText = string.Empty;
        ShadowingScore = null;
        ShadowingSummary = "The score will appear after recognition starts.";
        ScoreColorHex = "#A0A7B8";
    }

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

    private void UpdateAssessment()
    {
        var assessment = _analyzer.Assess(SourceText, RecognizedText);
        ShadowingScore = assessment.Score;
        ShadowingSummary = assessment.Summary;
        ScoreColorHex = ComputeScoreColor(assessment.Score);
    }

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

    private Task ApplyPlaybackSnapshotAsync(PlaybackState state)
    {
        return _dispatcher.RunAsync(() => ApplyPlaybackSnapshot(state));
    }

    private void ApplyPlaybackSnapshot(PlaybackState state)
    {
        CurrentPlaybackStatus = state.Status;
        PositionSeconds = state.Position.TotalSeconds;
        DurationSeconds = Math.Max(DurationSeconds, state.Duration.TotalSeconds);

        if (!string.IsNullOrWhiteSpace(state.Message)
            && state.Status is PlaybackStatus.Ready or PlaybackStatus.Completed or PlaybackStatus.Error)
        {
            StatusMessage = state.Message;
        }
    }

    /// <summary>
    /// Applies playback snapshots on the UI thread.
    /// </summary>
    /// <remarks>
    /// Playback services publish immutable state snapshots. This handler is responsible for translating those snapshots
    /// into individual bindable properties such as position, duration, and playback status. The dispatcher call is not an
    /// implementation detail; it is required because playback callbacks may arrive from native threads.
    /// </remarks>
    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        _ = ApplyPlaybackSnapshotAsync(e.State);
    }

    /// <summary>
    /// Applies recognition lifecycle updates on the UI thread.
    /// </summary>
    /// <remarks>
    /// Recognition status is kept separate from the main status message on purpose. Recognition callbacks can arrive at
    /// different times than playback callbacks, and merging the two streams blindly would make the transport status label
    /// flicker between unrelated meanings.
    /// </remarks>
    private void OnRecognitionStateChanged(object? sender, RecognitionStateChangedEventArgs e)
    {
        _ = _dispatcher.RunAsync(() =>
        {
            CurrentRecognitionStatus = e.Status;
            RecognitionAvailabilityText = BuildRecognitionAvailabilityText(e.Status, e.Message);
        });
    }

    /// <summary>
    /// Applies transcript updates and recomputes the score on the UI thread.
    /// </summary>
    /// <remarks>
    /// The analyzer is synchronous, so it is cheap enough to run immediately after every recognition update. That keeps
    /// the score and mismatch summary feeling live while the recognizer is still producing text.
    /// </remarks>
    private void OnRecognitionUpdated(object? sender, RecognitionUpdatedEventArgs e)
    {
        _ = _dispatcher.RunAsync(() =>
        {
            RecognizedText = e.Update.FullText;
            UpdateAssessment();
        });
    }

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





