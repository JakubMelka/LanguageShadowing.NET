using System.ComponentModel;
using LanguageShadowing.Application.ViewModels;

namespace LanguageShadowing.App;

public partial class MainPage : ContentPage
{
    private bool _initialized;
    private bool _isProgressDragActive;
    private MainViewModel? _subscribedViewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HookViewModel(viewModel);
        SyncProgressSlider();
    }

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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.PositionSeconds) or nameof(MainViewModel.DurationSeconds))
        {
            MainThread.BeginInvokeOnMainThread(SyncProgressSlider);
        }
    }

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
