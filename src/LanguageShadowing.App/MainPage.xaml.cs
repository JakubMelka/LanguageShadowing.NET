using LanguageShadowing.Application.ViewModels;

namespace LanguageShadowing.App;

public partial class MainPage : ContentPage
{
    private bool _initialized;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
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
        }
    }

    private async void OnProgressDragCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel viewModel)
        {
            await viewModel.SeekAsync(ProgressSlider.Value);
        }
    }
}
