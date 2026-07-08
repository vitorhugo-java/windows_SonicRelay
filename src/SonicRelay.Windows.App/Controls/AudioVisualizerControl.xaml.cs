using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace SonicRelay.Windows.App.Controls;

/// <summary>
/// A lightweight audio-level visualizer: a row of bars filled with the locked
/// teal/blue vertical gradient, eased toward a per-bar target derived from the
/// current level. No charting dependency — just bound rectangles animated by a
/// ~30fps timer, settling to a flat idle line when capture is inactive.
/// </summary>
public sealed partial class AudioVisualizerControl : UserControl
{
    private const int BarCount = 32;
    private const double MaxBarHeight = 96;
    private const double BaselineHeight = 3;

    private readonly Rectangle[] bars = new Rectangle[BarCount];
    private readonly double[] current = new double[BarCount];
    private readonly double[] weights = new double[BarCount];
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Random random = new();

    public AudioVisualizerControl()
    {
        InitializeComponent();
        BuildBars();
        timer.Tick += OnTick;
        Loaded += (_, _) => timer.Start();
        Unloaded += (_, _) => timer.Stop();
    }

    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level), typeof(double), typeof(AudioVisualizerControl), new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(AudioVisualizerControl), new PropertyMetadata(false));

    /// <summary>Current audio level in [0, 1] (peak).</summary>
    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <summary>When false, the bars settle to the flat idle baseline.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private void BuildBars()
    {
        var gradient = (Brush)Application.Current.Resources["Sonic.VisualizerGradientBrush"];
        for (var i = 0; i < BarCount; i++)
        {
            // A gentle centre-weighted hump so the bars read as a spectrum, not a block.
            weights[i] = 0.35 + 0.65 * Math.Sin(Math.PI * (i + 0.5) / BarCount);
            current[i] = BaselineHeight;
            var bar = new Rectangle
            {
                Width = 6,
                Height = BaselineHeight,
                RadiusX = 3,
                RadiusY = 3,
                Fill = gradient,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            bars[i] = bar;
            BarsPanel.Children.Add(bar);
        }
    }

    private void OnTick(object? sender, object e)
    {
        var level = IsActive ? Math.Clamp(Level, 0, 1) : 0;
        for (var i = 0; i < BarCount; i++)
        {
            var jitter = IsActive ? 0.75 + random.NextDouble() * 0.25 : 1.0;
            var target = BaselineHeight + (MaxBarHeight - BaselineHeight) * level * weights[i] * jitter;
            // Ease toward the target so the motion is smooth rather than jumpy.
            current[i] += (target - current[i]) * 0.35;
            bars[i].Height = Math.Max(BaselineHeight, current[i]);
        }
    }
}
