using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SonicRelay.Windows.Presentation;
using SonicRelay.Windows.WebRtc;

namespace SonicRelay.Windows.App.Pages;

/// <summary>
/// The publisher dashboard (issue #25). Business logic stays in
/// <see cref="DashboardViewModel"/>; this page just subscribes to the workflow and
/// WebRTC diagnostics, rebuilds the ViewModel, and pushes it to the cards.
/// </summary>
public sealed partial class PublisherDashboardPage : Page
{
    private PublisherRuntime? runtime;
    private PublisherWorkflow? workflow;
    private IWebRtcPublisher? webRtcPublisher;

    public PublisherDashboardPage()
    {
        InitializeComponent();

        if (global::Windows.ApplicationModel.DesignMode.DesignModeEnabled)
        {
            ApplyModel(DashboardViewModel.DesignTime);
            return;
        }

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.RuntimeChanged += OnRuntimeChanged;
        Attach(App.CurrentApp.Runtime);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        App.CurrentApp.RuntimeChanged -= OnRuntimeChanged;
        Attach(null);
    }

    private void OnRuntimeChanged(PublisherRuntime? next) => DispatcherQueue.TryEnqueue(() => Attach(next));

    private void Attach(PublisherRuntime? next)
    {
        if (workflow is not null) workflow.StateChanged -= OnStateChanged;
        if (webRtcPublisher is not null) webRtcPublisher.DiagnosticsChanged -= OnDiagnosticsChanged;

        runtime = next;
        workflow = next?.Workflow;
        webRtcPublisher = next?.WebRtcPublisher;

        if (workflow is not null) workflow.StateChanged += OnStateChanged;
        if (webRtcPublisher is not null) webRtcPublisher.DiagnosticsChanged += OnDiagnosticsChanged;

        Rebuild();
    }

    private void OnStateChanged(PublisherSnapshot state) => DispatcherQueue.TryEnqueue(Rebuild);
    private void OnDiagnosticsChanged(WebRtcPublisherDiagnostics diagnostics) => DispatcherQueue.TryEnqueue(Rebuild);

    private void Rebuild()
    {
        var model = DashboardViewModel.From(
            workflow?.State,
            webRtcPublisher?.Diagnostics,
            runtime?.RelayPreference.ForceRelay ?? false);
        ApplyModel(model);
    }

    private void ApplyModel(DashboardViewModel model)
    {
        HeadlineBadge.Text = model.SessionStatusText;
        HeadlineBadge.Kind = model.SessionStatusBadge;
        ConnectionCard.Model = model;
        QualityCard.Model = model;
        Visualizer.Level = model.AudioPeak;
        Visualizer.IsActive = model.IsCapturing;
        LevelText.Text = model.IsCapturing
            ? string.Create(CultureInfo.CurrentCulture, $"peak {model.AudioPeak * 100:F0}%")
            : DashboardViewModel.Unknown;
    }
}
