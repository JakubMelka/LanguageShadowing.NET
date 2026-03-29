using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace LanguageShadowing.Infrastructure.Settings;

/// <summary>
/// MAUI implementation of <see cref="IAppDispatcher"/>.
/// </summary>
public sealed class MauiAppDispatcher : IAppDispatcher
{
    /// <summary>
    /// Executes the supplied action on the main UI thread.
    /// </summary>
    public Task RunAsync(Action action)
    {
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}
