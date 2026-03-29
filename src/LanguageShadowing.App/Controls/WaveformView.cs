using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LanguageShadowing.App.Controls;

/// <summary>
/// Draws the custom waveform strip shown behind the playback slider.
/// </summary>
/// <remarks>
/// <para>
/// This control exists because the application does not rely on a native audio waveform widget. Instead, the app
/// prepares a small list of normalized amplitude samples and gives them to this view, which then paints a lightweight
/// visual representation on demand.
/// </para>
/// <para>
/// The control is intentionally simple:
/// </para>
/// <list type="bullet">
/// <item><description>it never performs audio analysis itself,</description></item>
/// <item><description>it does not own playback state,</description></item>
/// <item><description>it only converts already prepared sample values into bars, and</description></item>
/// <item><description>it highlights the portion that belongs to the current normalized playback progress.</description></item>
/// </list>
/// <para>
/// When no real waveform samples are available yet, the control generates a deterministic fallback pattern. That keeps
/// the layout stable and avoids an empty rectangle before synthesis finishes.
/// </para>
/// </remarks>
public sealed class WaveformView : GraphicsView, IDrawable
{
    /// <summary>
    /// Defines the samples rendered by the control.
    /// </summary>
    /// <remarks>
    /// The expected value is a list of normalized amplitudes where each item is typically in the range 0-1.
    /// Values outside that range are tolerated and clamped during drawing.
    /// </remarks>
    public static readonly BindableProperty SamplesProperty = BindableProperty.Create(
        nameof(Samples),
        typeof(IReadOnlyList<float>),
        typeof(WaveformView),
        Array.Empty<float>(),
        propertyChanged: OnVisualPropertyChanged);

    /// <summary>
    /// Defines the current playback progress as a normalized value in the range 0-1.
    /// </summary>
    /// <remarks>
    /// The control does not know anything about seconds or media duration. The view model converts time into a ratio,
    /// and the control simply uses that ratio to decide which bars should be painted as "already played".
    /// </remarks>
    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress),
        typeof(double),
        typeof(WaveformView),
        0d,
        propertyChanged: OnVisualPropertyChanged);

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveformView"/> class.
    /// </summary>
    /// <remarks>
    /// The control makes itself its own drawable so all painting logic stays in one type. The fixed height is a visual
    /// design decision: the waveform should remain compact enough to sit behind the transport slider.
    /// </remarks>
    public WaveformView()
    {
        Drawable = this;
        HeightRequest = 72;
    }

    /// <summary>
    /// Gets or sets the waveform samples to render.
    /// </summary>
    public IReadOnlyList<float> Samples
    {
        get => (IReadOnlyList<float>)GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    /// <summary>
    /// Gets or sets the normalized playback progress.
    /// </summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// Draws the waveform background and sample bars.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The drawing algorithm is intentionally geometric rather than pixel-perfect. The available width is divided into
    /// one slot per sample, and each slot becomes one rounded bar. Gap size and bar width are derived from the slot
    /// width so that the same code scales from narrow to wide layouts without truncating the sample count.
    /// </para>
    /// <para>
    /// A sample does not represent an exact PCM frame. It is only a display bucket, so the purpose of the rendering is
    /// to communicate "shape" and playback progress, not signal accuracy.
    /// </para>
    /// </remarks>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;
        canvas.FillColor = Color.FromArgb("#EEF2F8");
        canvas.FillRoundedRectangle(dirtyRect, 18);

        var samples = Samples.Count == 0 ? BuildFallbackSamples() : Samples;
        var usableWidth = Math.Max(0f, dirtyRect.Width - 16);
        var originX = dirtyRect.X + 8;
        var progress = (float)Math.Clamp(Progress, 0d, 1d);
        var progressX = originX + usableWidth * progress;

        if (samples.Count > 0 && usableWidth > 0)
        {
            var stepWidth = usableWidth / samples.Count;
            var gap = Math.Min(1.5f, stepWidth * 0.25f);
            var barWidth = Math.Max(0.5f, stepWidth - gap);

            for (var i = 0; i < samples.Count; i++)
            {
                var amplitude = Math.Clamp(samples[i], 0.08f, 1f);
                var height = Math.Max(8f, amplitude * (dirtyRect.Height - 20));
                var x = originX + i * stepWidth;
                var y = dirtyRect.Center.Y - (height / 2f);
                var color = x <= progressX ? Color.FromArgb("#2D7FF9") : Color.FromArgb("#BFC9DA");
                canvas.FillColor = color;
                canvas.FillRoundedRectangle(x, y, barWidth, height, Math.Min(barWidth / 2f, 2f));
            }
        }

        canvas.RestoreState();
    }

    /// <summary>
    /// Invalidates the control whenever a visual input changes.
    /// </summary>
    /// <remarks>
    /// MAUI bindable properties do not redraw custom graphics automatically. Calling <see cref="VisualElement.InvalidateMeasure"/>
    /// would be the wrong tool here because the layout size does not change; only the pixels do. <see cref="GraphicsView.Invalidate"/>
    /// requests a repaint while keeping layout unchanged.
    /// </remarks>
    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is WaveformView view)
        {
            view.Invalidate();
        }
    }

    /// <summary>
    /// Builds a deterministic placeholder waveform when no real samples are available.
    /// </summary>
    /// <remarks>
    /// The values are intentionally synthetic but stable. A random placeholder would make the UI flicker between runs,
    /// which would be distracting and harder to debug.
    /// </remarks>
    private static IReadOnlyList<float> BuildFallbackSamples()
    {
        return Enumerable.Range(0, 144)
            .Select(index => 0.2f + (MathF.Abs(MathF.Sin(index * 0.42f)) * 0.75f))
            .ToArray();
    }
}
