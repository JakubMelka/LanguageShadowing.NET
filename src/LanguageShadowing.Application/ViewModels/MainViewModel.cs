using System.Collections.ObjectModel;
using LanguageShadowing.Application.Abstractions;
using LanguageShadowing.Application.Analysis;
using LanguageShadowing.Application.Common;
using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Interfaces;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Application.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ISpeechEngine _engine;
    private readonly IShadowingSettingsStore _settingsStore;
    private readonly IShadowingAnalyzer _analyzer;
    private readonly IAppDispatcher _dispatcher;
    private readonly ObservableCollection<VoiceInfo> _voices = new();
    private IReadOnlyList<float> _waveformSamples = Array.Empty<float>();
    private SpeechEngineCapabilities _capabilities = new(false, false, false, false, false, false);
    private SpeechSynthesisResult? _preparedSynthesis;
    private string? _preparedSignature;
    private bool _isInitialized;
    private bool _isBusy;
    private string _sourceText = string.Empty;
    private string _recognizedText = string.Empty;
    private VoiceInfo? _selectedVoice;
    private double _speechRate = 1.0;
    private double _positionSeconds;
    private double _durationSeconds;
    private string _statusMessage = "Ready.";
    private string _shadowingSummary = "The score will appear after recognition starts.";
    private string _scoreColorHex = "#A0A7B8";
    private int? _shadowingScore;
    private PlaybackStatus _currentPlaybackStatus = PlaybackStatus.Idle;
    private RecognitionStatus _currentRecognitionStatus = RecognitionStatus.Idle;

    public MainViewModel(
        ISpeechEngine engine,
        IShadowingSettingsStore settingsStore,
        IShadowingAnalyzer analyzer,
        IAppDispatcher dispatcher)
    {
        _engine = engine;
        _settingsStore = settingsStore;
        _analyzer = analyzer;
        _dispatcher = dispatcher;
        Voices = AsReadOnly(_voices);

        InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !_isInitialized && !IsBusy);
        PlayCommand = new AsyncRelayCommand(PlayAsync, () => !IsBusy);
        PauseCommand = new AsyncRelayCommand(PauseAsync, () => !IsBusy && CurrentPlaybackStatus == PlaybackStatus.Playing && Capabilities.SupportsPause);
        StopCommand = new AsyncRelayCommand(StopAsync, () => !IsBusy && CurrentPlaybackStatus is PlaybackStatus.Playing or PlaybackStatus.Paused);
        RewindCommand = new AsyncRelayCommand(RewindAsync, () => !IsBusy && CanSeek);
        ResetCommand = new AsyncRelayCommand(ResetAsync, () => !IsBusy);
        ClearRecognizedCommand = new RelayCommand(ClearRecognizedText);

        _engine.Playback.StateChanged += OnPlaybackStateChanged;
        _engine.Recognition.StateChanged += OnRecognitionStateChanged;
        _engine.Recognition.RecognitionUpdated += OnRecognitionUpdated;
    }

    public ReadOnlyObservableCollection<VoiceInfo> Voices { get; }

    public AsyncRelayCommand InitializeCommand { get; }

    public AsyncRelayCommand PlayCommand { get; }

    public AsyncRelayCommand PauseCommand { get; }

    public AsyncRelayCommand StopCommand { get; }

    public AsyncRelayCommand RewindCommand { get; }

    public AsyncRelayCommand ResetCommand { get; }

    public RelayCommand ClearRecognizedCommand { get; }

    public SpeechEngineCapabilities Capabilities
    {
        get => _capabilities;
        private set
        {
            if (SetProperty(ref _capabilities, value))
            {
                OnPropertyChanged(nameof(CanSeek), nameof(RecognitionAvailabilityText));
                NotifyCommands();
            }
        }
    }

    public VoiceInfo? SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            if (SetProperty(ref _selectedVoice, value))
            {
                _preparedSignature = null;
                PersistSettings();
            }
        }
    }

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
                ClearRecognizedText();
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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
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
                OnPropertyChanged(nameof(IsPlaying), nameof(IsPaused));
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
                OnPropertyChanged(nameof(IsRecognizing), nameof(RecognitionAvailabilityText));
            }
        }
    }

    public bool IsPlaying => CurrentPlaybackStatus == PlaybackStatus.Playing;

    public bool IsPaused => CurrentPlaybackStatus == PlaybackStatus.Paused;

    public bool IsRecognizing => CurrentRecognitionStatus == RecognitionStatus.Listening || CurrentRecognitionStatus == RecognitionStatus.Starting;

    public bool CanSeek => Capabilities.SupportsSeek && DurationSeconds > 0;

    public string RecognitionAvailabilityText => _engine.Recognition.IsAvailable
        ? (IsRecognizing ? "Microphone is listening" : "Microphone is ready")
        : "Speech recognition is not available on this platform";

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
            var settings = await _settingsStore.LoadAsync().ConfigureAwait(false);
            SpeechRate = settings.SpeechRate is >= 0.5 and <= 2.0 ? settings.SpeechRate : 1.0;

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

            StatusMessage = _voices.Count == 0
                ? "No voices were found."
                : "Choose a voice, enter text, and start playback.";
            _isInitialized = true;
            InitializeCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

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
                ClearRecognizedText();
                await _engine.Recognition.ResetAsync().ConfigureAwait(false);
            }

            if (requiresPreparation)
            {
                StatusMessage = "Generating speech output...";
                var request = new SpeechSynthesisRequest(SourceText.Trim(), SelectedVoice, SpeechRate);
                _preparedSynthesis = await _engine.TextToSpeech.PrepareAsync(request).ConfigureAwait(false);
                _preparedSignature = requestSignature;
                await _engine.Playback.LoadAsync(_preparedSynthesis).ConfigureAwait(false);
                ApplySynthesisResult(_preparedSynthesis);
            }

            await _engine.Playback.PlayAsync().ConfigureAwait(false);
            StatusMessage = _engine.Recognition.IsAvailable
                ? "Playback is running and the microphone is connecting."
                : "Playback is running. Speech recognition is not available on this platform.";
            await StartRecognitionIfAvailableAsync().ConfigureAwait(false);
            PersistSettings();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PauseAsync()
    {
        IsBusy = true;
        try
        {
            await _engine.Playback.PauseAsync().ConfigureAwait(false);
            await StopRecognitionIfRunningAsync().ConfigureAwait(false);
            StatusMessage = "Playback is paused.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StopAsync()
    {
        IsBusy = true;
        try
        {
            await _engine.Playback.StopAsync().ConfigureAwait(false);
            await StopRecognitionIfRunningAsync().ConfigureAwait(false);
            StatusMessage = "Playback stopped.";
        }
        finally
        {
            IsBusy = false;
        }
    }

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

    public async Task ResetAsync()
    {
        IsBusy = true;
        try
        {
            await _engine.Playback.ResetAsync().ConfigureAwait(false);
            await _engine.Recognition.ResetAsync().ConfigureAwait(false);
            _preparedSynthesis = null;
            _preparedSignature = null;
            SourceText = string.Empty;
            ClearRecognizedText();
            StatusMessage = "Everything was reset.";
        }
        finally
        {
            IsBusy = false;
        }
    }

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

    private async Task StartRecognitionIfAvailableAsync()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        await _engine.Recognition.StartAsync().ConfigureAwait(false);
    }

    private async Task StopRecognitionIfRunningAsync()
    {
        if (!_engine.Recognition.IsAvailable)
        {
            return;
        }

        if (CurrentRecognitionStatus is RecognitionStatus.Listening or RecognitionStatus.Starting)
        {
            await _engine.Recognition.StopAsync().ConfigureAwait(false);
        }
    }

    private void ClearRecognizedText()
    {
        RecognizedText = string.Empty;
        ShadowingScore = null;
        ShadowingSummary = "The score will appear after recognition starts.";
        ScoreColorHex = "#A0A7B8";
    }

    private string BuildRequestSignature()
    {
        return string.Join("|", SourceText.Trim(), SelectedVoice?.Id ?? string.Empty, SpeechRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
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

        _ = _settingsStore.SaveAsync(new ShadowingSettings(SelectedVoice?.Id, SpeechRate));
    }

    private void NotifyCommands()
    {
        InitializeCommand.NotifyCanExecuteChanged();
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RewindCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        ClearRecognizedCommand.NotifyCanExecuteChanged();
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        _ = _dispatcher.RunAsync(() =>
        {
            CurrentPlaybackStatus = e.State.Status;
            PositionSeconds = e.State.Position.TotalSeconds;
            DurationSeconds = Math.Max(DurationSeconds, e.State.Duration.TotalSeconds);

            if (!string.IsNullOrWhiteSpace(e.State.Message))
            {
                StatusMessage = e.State.Message;
            }
        });
    }

    private void OnRecognitionStateChanged(object? sender, RecognitionStateChangedEventArgs e)
    {
        _ = _dispatcher.RunAsync(() =>
        {
            CurrentRecognitionStatus = e.Status;
            if (!string.IsNullOrWhiteSpace(e.Message))
            {
                StatusMessage = e.Message;
            }
        });
    }

    private void OnRecognitionUpdated(object? sender, RecognitionUpdatedEventArgs e)
    {
        _ = _dispatcher.RunAsync(() =>
        {
            RecognizedText = e.Update.FullText;
            UpdateAssessment();
        });
    }
}

