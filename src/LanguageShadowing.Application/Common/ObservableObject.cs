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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LanguageShadowing.Application.Common;

/// <summary>
/// Base class for bindable objects that need thread-safe property notifications for MAUI.
/// </summary>
/// <remarks>
/// <para>
/// The application receives updates from several sources that do not naturally execute on the UI thread:
/// speech synthesis preparation, media playback callbacks, speech recognition callbacks, and background task
/// continuations resumed after <c>await</c>. MAUI bindings, however, expect <see cref="PropertyChanged"/> to behave as
/// if it came from the UI thread.
/// </para>
/// <para>
/// This base class solves that mismatch once. It captures the current <see cref="SynchronizationContext"/> when the
/// object is created and uses that context later to deliver property notifications. As a result, the rest of the view
/// model code can call <see cref="SetProperty{T}"/> from asynchronous continuations without repeating the same thread
/// marshalling logic in every setter.
/// </para>
/// </remarks>
public abstract class ObservableObject : INotifyPropertyChanged
{
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

    /// <summary>
    /// Raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Assigns a property value and raises <see cref="PropertyChanged"/> only when the value actually changed.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="storage">The backing field to update.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property name, usually supplied by the compiler.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for one property.
    /// </summary>
    /// <remarks>
    /// If the current code is already running on the captured synchronization context, the event is raised immediately.
    /// Otherwise the event is posted back to the captured context so MAUI bindings observe the change from the expected thread.
    /// </remarks>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return;
        }

        _synchronizationContext.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)), null);
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for multiple dependent properties.
    /// </summary>
    protected void OnPropertyChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// Wraps a mutable collection in a read-only adapter for public exposure.
    /// </summary>
    protected static ReadOnlyObservableCollection<T> AsReadOnly<T>(ObservableCollection<T> collection)
    {
        return new ReadOnlyObservableCollection<T>(collection);
    }
}
