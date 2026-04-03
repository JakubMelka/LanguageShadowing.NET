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

