using LanguageShadowing.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace LanguageShadowing.Infrastructure.Settings;

public sealed class MauiAppDispatcher : IAppDispatcher
{
    public Task RunAsync(Action action)
    {
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}
