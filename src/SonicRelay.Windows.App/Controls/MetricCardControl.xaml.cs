using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SonicRelay.Windows.App.Controls;

/// <summary>A small labelled metric tile (label + value) used by the quality card.</summary>
public sealed partial class MetricCardControl : UserControl
{
    public MetricCardControl()
    {
        InitializeComponent();
        Apply();
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(MetricCardControl), new PropertyMetadata("Label", OnChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(MetricCardControl), new PropertyMetadata("—", OnChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MetricCardControl)d).Apply();

    private void Apply()
    {
        LabelText.Text = Label ?? string.Empty;
        ValueText.Text = string.IsNullOrEmpty(Value) ? "—" : Value;
    }
}
