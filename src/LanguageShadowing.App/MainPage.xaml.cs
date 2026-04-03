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

using System.ComponentModel;
using LanguageShadowing.Application.ViewModels;

namespace LanguageShadowing.App;

/// <summary>
/// Code-behind for the application's main page.
/// </summary>
/// <remarks>
/// <para>
/// Nearly all behavior lives in <see cref="MainViewModel"/>. This code-behind keeps only view-specific mechanics that
/// are awkward or unreliable to express through pure XAML binding on Windows.
/// </para>
/// <para>
/// The most important example is the progress slider thumb. The application updates playback position very frequently,
/// and in this project the native slider visual did not reliably redraw its thumb from bindings alone. The page therefore
/// listens to position-related property changes and pushes the current value into the slider explicitly on the UI thread.
/// </para>
/// </remarks>
public partial class MainPage : ContentPage
{
    private bool _initialized;
    private bool _isProgressDragActive;
    private MainViewModel? _subscribedViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="viewModel">The singleton view model that drives the page.</param>
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HookViewModel(viewModel);
        SyncProgressSlider();
    }

    /// <summary>
    /// Performs one-time page initialization when the page becomes visible for the first time.
    /// </summary>
    /// <remarks>
    /// Initialization is deferred to the first appearance so the MAUI host can finish constructing the page before the
    /// app starts talking to platform services such as voice catalogs and recognition availability.
    /// </remarks>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (BindingContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
            SyncProgressSlider();
        }
    }

    /// <summary>
    /// Rebinds the property-changed subscription when the page receives a different binding context.
    /// </summary>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        HookViewModel(BindingContext as MainViewModel);
        SyncProgressSlider();
    }

    private void HookViewModel(MainViewModel? viewModel)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _subscribedViewModel = viewModel;

        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Reacts only to the view-model properties that affect the transport slider.
    /// </summary>
    /// <remarks>
    /// The callback does not update the slider directly. It posts the update back to the main thread so the page stays
    /// safe even if the property change ultimately came from a background continuation or native callback.
    /// </remarks>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.PositionSeconds) or nameof(MainViewModel.DurationSeconds))
        {
            MainThread.BeginInvokeOnMainThread(SyncProgressSlider);
        }
    }

    /// <summary>
    /// Updates the slider's value and maximum from the current playback snapshot.
    /// </summary>
    /// <remarks>
    /// Slider synchronization is intentionally suspended while the user is dragging the thumb. Without that guard, live
    /// playback updates would fight the user's gesture and make the thumb jump unpredictably.
    /// </remarks>
    private void SyncProgressSlider()
    {
        if (_isProgressDragActive || BindingContext is not MainViewModel viewModel)
        {
            return;
        }

        ProgressSlider.Maximum = viewModel.ProgressMaximum;
        ProgressSlider.Value = Math.Clamp(viewModel.PositionSeconds, ProgressSlider.Minimum, ProgressSlider.Maximum);
    }

    private void OnProgressDragStarted(object? sender, EventArgs e)
    {
        _isProgressDragActive = true;
    }

    /// <summary>
    /// Applies the user-selected playback position after the drag gesture finishes.
    /// </summary>
    private async void OnProgressDragCompleted(object? sender, EventArgs e)
    {
        _isProgressDragActive = false;

        if (BindingContext is MainViewModel viewModel)
        {
            await viewModel.SeekAsync(ProgressSlider.Value);
            SyncProgressSlider();
        }
    }
}
