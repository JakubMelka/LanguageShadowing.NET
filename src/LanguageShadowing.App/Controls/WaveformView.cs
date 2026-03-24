using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LanguageShadowing.App.Controls;

public sealed class WaveformView : GraphicsView, IDrawable
{
    public static readonly BindableProperty SamplesProperty = BindableProperty.Create(
        nameof(Samples),
        typeof(IReadOnlyList<float>),
        typeof(WaveformView),
        Array.Empty<float>(),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress),
        typeof(double),
        typeof(WaveformView),
        0d,
        propertyChanged: OnVisualPropertyChanged);

    public WaveformView()
    {
        Drawable = this;
        HeightRequest = 72;
    }

    public IReadOnlyList<float> Samples
    {
        get => (IReadOnlyList<float>)GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;
        canvas.FillColor = Color.FromArgb("#EEF2F8");
        canvas.FillRoundedRectangle(dirtyRect, 18);

        var samples = Samples.Count == 0 ? BuildFallbackSamples() : Samples;
        var usableWidth = dirtyRect.Width - 16;
        var originX = dirtyRect.X + 8;
        var barWidth = Math.Max(2f, usableWidth / (samples.Count * 1.6f));
        var gap = Math.Max(1f, barWidth * 0.6f);
        var progress = (float)Math.Clamp(Progress, 0d, 1d);
        var progressX = originX + usableWidth * progress;

        for (var i = 0; i < samples.Count; i++)
        {
            var amplitude = Math.Clamp(samples[i], 0.08f, 1f);
            var height = Math.Max(8f, amplitude * (dirtyRect.Height - 20));
            var x = originX + i * (barWidth + gap);
            var y = dirtyRect.Center.Y - (height / 2f);
            var color = x <= progressX ? Color.FromArgb("#2D7FF9") : Color.FromArgb("#BFC9DA");
            canvas.FillColor = color;
            canvas.FillRoundedRectangle(x, y, barWidth, height, barWidth / 2f);
        }

        canvas.RestoreState();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is WaveformView view)
        {
            view.Invalidate();
        }
    }

    private static IReadOnlyList<float> BuildFallbackSamples()
    {
        return Enumerable.Range(0, 48)
            .Select(index => 0.2f + (MathF.Abs(MathF.Sin(index * 0.42f)) * 0.75f))
            .ToArray();
    }
}
