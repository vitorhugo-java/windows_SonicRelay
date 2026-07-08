using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SonicRelay.Windows.Presentation;

namespace SonicRelay.Windows.App.Controls;

/// <summary>
/// A status pill whose foreground/background/border come from the locked SonicRelay
/// palette per <see cref="DashboardBadge"/>. Keeps the badge colour rules in one place.
/// </summary>
public sealed partial class StatusBadgeControl : UserControl
{
    public StatusBadgeControl()
    {
        InitializeComponent();
        Apply();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StatusBadgeControl), new PropertyMetadata("Unknown", OnChanged));

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(DashboardBadge), typeof(StatusBadgeControl),
        new PropertyMetadata(DashboardBadge.Neutral, OnChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public DashboardBadge Kind
    {
        get => (DashboardBadge)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StatusBadgeControl)d).Apply();

    private void Apply()
    {
        BadgeText.Text = Text ?? string.Empty;
        var (foreground, background, border) = Kind switch
        {
            DashboardBadge.Success => ("Sonic.SuccessBrush", "Sonic.SuccessBackgroundBrush", "Sonic.SuccessBorderBrush"),
            DashboardBadge.Warning => ("Sonic.WarningBrush", "Sonic.WarningBackgroundBrush", "Sonic.WarningBorderBrush"),
            DashboardBadge.Danger => ("Sonic.DangerBrush", "Sonic.DangerBackgroundBrush", "Sonic.DangerBorderBrush"),
            _ => ("Sonic.NeutralBrush", "Sonic.NeutralBackgroundBrush", "Sonic.NeutralBorderBrush"),
        };
        BadgeText.Foreground = Brush(foreground);
        BadgeBorder.Background = Brush(background);
        BadgeBorder.BorderBrush = Brush(border);
    }

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
}
