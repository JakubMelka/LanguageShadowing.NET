using LanguageShadowing.Core.Enums;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Core.Interfaces;

/// <summary>
/// Represents a streaming speech recognition engine used by the shadowing workflow.
/// </summary>
/// <remarks>
/// The service exposes two different asynchronous event streams:
/// one for coarse status transitions (<see cref="StateChanged"/>) and one for transcript updates
/// (<see cref="RecognitionUpdated"/>). The view model consumes both streams independently because a recognizer
/// can keep producing transcript fragments while its lifecycle state remains unchanged.
/// </remarks>
public interface ISpeechRecognitionService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether speech recognition is available on the current platform.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the last lifecycle status published by the service.
    /// </summary>
    RecognitionStatus CurrentStatus { get; }

    /// <summary>
    /// Raised whenever the recognizer lifecycle changes state.
    /// </summary>
    event EventHandler<RecognitionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Raised whenever the recognizer produces a partial or final transcript update.
    /// </summary>
    event EventHandler<RecognitionUpdatedEventArgs>? RecognitionUpdated;

    /// <summary>
    /// Starts recognition.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a graceful stop of recognition.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears local transcript state and returns the recognizer to an idle baseline.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the accumulated transcript without forcing the recognizer to stop listening.
    /// </summary>
    Task ClearTranscriptAsync(CancellationToken cancellationToken = default);
}


