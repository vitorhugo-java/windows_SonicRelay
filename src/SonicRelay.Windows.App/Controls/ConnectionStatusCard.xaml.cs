using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SonicRelay.Windows.Presentation;

namespace SonicRelay.Windows.App.Controls;

/// <summary>Session / signaling / WebRTC status badges plus mode, code, and viewers.</summary>
public sealed partial class ConnectionStatusCard : UserControl
{
    public ConnectionStatusCard()
    {
        InitializeComponent();
        Apply(Model);
    }

    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(DashboardViewModel), typeof(ConnectionStatusCard),
        new PropertyMetadata(null, OnModelChanged));

    public DashboardViewModel? Model
    {
        get => (DashboardViewModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ConnectionStatusCard)d).Apply((DashboardViewModel?)e.NewValue);

    private void Apply(DashboardViewModel? model)
    {
        model ??= new DashboardViewModel();

        SessionBadge.Text = model.SessionStatusText;
        SessionBadge.Kind = model.SessionStatusBadge;
        SignalingBadge.Text = model.SignalingStatusText;
        SignalingBadge.Kind = model.SignalingStatusBadge;
        WebRtcBadge.Text = model.WebRtcStatusText;
        WebRtcBadge.Kind = model.WebRtcStatusBadge;

        ModeText.Text = model.ConnectionModeText;
        CodeText.Text = model.SessionCodeText;
        ViewerText.Text = model.ViewerCount.ToString(CultureInfo.CurrentCulture);
    }
}
