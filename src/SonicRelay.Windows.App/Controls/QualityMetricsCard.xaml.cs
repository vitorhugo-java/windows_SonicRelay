using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SonicRelay.Windows.Presentation;

namespace SonicRelay.Windows.App.Controls;

/// <summary>Latency / jitter / packet-loss / bitrate metric tiles from the ViewModel.</summary>
public sealed partial class QualityMetricsCard : UserControl
{
    public QualityMetricsCard()
    {
        InitializeComponent();
        Apply(Model);
    }

    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(DashboardViewModel), typeof(QualityMetricsCard),
        new PropertyMetadata(null, OnModelChanged));

    public DashboardViewModel? Model
    {
        get => (DashboardViewModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((QualityMetricsCard)d).Apply((DashboardViewModel?)e.NewValue);

    private void Apply(DashboardViewModel? model)
    {
        model ??= new DashboardViewModel();
        LatencyMetric.Value = model.LatencyText;
        JitterMetric.Value = model.JitterText;
        PacketLossMetric.Value = model.PacketLossText;
        BitrateMetric.Value = model.BitrateText;
    }
}
