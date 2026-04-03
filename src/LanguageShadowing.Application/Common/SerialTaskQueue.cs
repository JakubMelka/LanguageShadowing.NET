using System;
using System.Threading.Tasks;

namespace LanguageShadowing.Application.Common;

/// <summary>
/// Executes asynchronous work items one after another in submission order.
/// </summary>
public sealed class SerialTaskQueue
{
    private readonly object _sync = new();
    private Task _tail = Task.CompletedTask;

    public Task WhenCurrentCompleted()
    {
        lock (_sync)
        {
            return _tail;
        }
    }

    public Task Enqueue(Func<Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        lock (_sync)
        {
            _tail = RunAsync(_tail, workItem);
            return _tail;
        }
    }

    private static async Task RunAsync(Task previous, Func<Task> workItem)
    {
        try
        {
            await previous.ConfigureAwait(false);
        }
        catch
        {
        }

        await workItem().ConfigureAwait(false);
    }
}

