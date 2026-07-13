using Avalonia.Threading;
using SonicRelay.Windows.Presentation;
using SonicRelay.Windows.WebRtc;

namespace SonicRelay.Windows.Desktop.ViewModels;

/// <summary>
/// Root view model for the desktop shell. It composes the sidebar, the account header and
/// the <see cref="DashboardShellViewModel"/>, and translates the shell's contextual actions
/// into <see cref="PublisherWorkflow"/> calls once a runtime is attached. With no runtime
/// (the standalone preview launch) the actions are disabled and the shell renders the
/// representative snapshot, so the layout and design system stay verifiable without a
/// backend. Attaching a live runtime — tray, reconnection and sign-in flow — is the next
/// phase-2 slice (issue #32); this type already exposes the seam for it.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private PublisherRuntime? runtime;
    private PublisherWorkflow? workflow;
    private IWebRtcPublisher? webRtc;
    private PublisherSnapshot? snapshot;

    public MainWindowViewModel()
    {
        Navigation =
        [
            new NavigationItem("◧", "Dashboard") { IsSelected = true },
            new NavigationItem("♪", "Audio") { IsEnabled = false },
            new NavigationItem("⧉", "Session") { IsEnabled = false },
            new NavigationItem("⚙", "Diagnostics") { IsEnabled = false },
            new NavigationItem("⚑", "Settings") { IsEnabled = false },
        ];

        CreateSessionCommand = new RelayCommand(() => Run(w => w.CreateSessionAsync()), () => Can(c => c.CanCreateSession));
        StartAudioCommand = new RelayCommand(() => Run(w => w.StartAudioAsync()), () => Can(c => c.CanStartAudio));
        StopAudioCommand = new RelayCommand(() => Run(w => w.StopAudioAsync()), () => Can(c => c.CanStopAudio));
        EndSessionCommand = new RelayCommand(() => Run(w => w.EndSessionAsync()), () => Can(c => c.CanEndSession));
        RetryCommand = new RelayCommand(() => Run(w => w.ReconnectSignalingAsync()), () => Can(c => c.CanRetry));
        LogoutCommand = new RelayCommand(() => Run(w => w.LogoutAsync()), () => Can(c => c.CanLogout));
    }

    public IReadOnlyList<NavigationItem> Navigation { get; }
    public DashboardShellViewModel Shell { get; } = new();

    public RelayCommand CreateSessionCommand { get; }
    public RelayCommand StartAudioCommand { get; }
    public RelayCommand StopAudioCommand { get; }
    public RelayCommand EndSessionCommand { get; }
    public RelayCommand RetryCommand { get; }
    public RelayCommand LogoutCommand { get; }

    /// <summary>
    /// Attaches a live publisher runtime: subscribes to workflow and WebRTC diagnostics and
    /// rebuilds the shell on every change (marshalled to the UI thread). Passing <c>null</c>
    /// detaches. Idempotent for the same runtime.
    /// </summary>
    public void Attach(PublisherRuntime? next)
    {
        if (ReferenceEquals(runtime, next)) return;

        if (workflow is not null) workflow.StateChanged -= OnStateChanged;
        if (webRtc is not null) webRtc.DiagnosticsChanged -= OnDiagnosticsChanged;

        runtime = next;
        workflow = next?.Workflow;
        webRtc = next?.WebRtcPublisher;

        if (workflow is not null) workflow.StateChanged += OnStateChanged;
        if (webRtc is not null) webRtc.DiagnosticsChanged += OnDiagnosticsChanged;

        snapshot = workflow?.State;
        Rebuild();
    }

    private void OnStateChanged(PublisherSnapshot state) => Dispatch(() => { snapshot = state; Rebuild(); });
    private void OnDiagnosticsChanged(WebRtcPublisherDiagnostics _) => Dispatch(Rebuild);

    private void Rebuild()
    {
        Shell.Update(snapshot, webRtc?.Diagnostics, runtime?.RelayPreference.ForceRelay ?? false);
        RaiseCommandStates();
    }

    private Task Run(Func<PublisherWorkflow, Task> action) =>
        workflow is null ? Task.CompletedTask : action(workflow);

    private bool Can(Func<PublisherUiCapabilities, bool> capability) =>
        workflow is not null && capability(Shell.Capabilities) && snapshot?.IsBusy != true;

    private void RaiseCommandStates()
    {
        CreateSessionCommand.RaiseCanExecuteChanged();
        StartAudioCommand.RaiseCanExecuteChanged();
        StopAudioCommand.RaiseCanExecuteChanged();
        EndSessionCommand.RaiseCanExecuteChanged();
        RetryCommand.RaiseCanExecuteChanged();
        LogoutCommand.RaiseCanExecuteChanged();
    }

    private static void Dispatch(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }

    /// <summary>Standalone preview instance (no runtime) used at launch and by the designer.</summary>
    public static MainWindowViewModel CreatePreview()
    {
        var vm = new MainWindowViewModel();
        vm.Shell.Update(PreviewSnapshot(), PreviewDiagnostics(), forceRelay: false);
        return vm;
    }

    private static WebRtcPublisherDiagnostics PreviewDiagnostics() => new(
        ViewerConnectionCount: 1,
        Viewers:
        [
            new PeerConnectionDiagnostics(
                "viewer-1",
                PeerConnectionState.Connected,
                SelectedCandidatePair: "host:host",
                EstimatedRoundTripTime: TimeSpan.FromMilliseconds(38),
                AudioSend: new AudioSendDiagnostics(
                    EncodedPacketsSent: 12_000,
                    PacedPacketsDropped: 0,
                    SendFailures: 0,
                    PacingBacklogPackets: 0,
                    PacingBacklogDuration: TimeSpan.Zero,
                    FrameDurationMs: 20,
                    OpusBitrateKbps: 96,
                    Channels: 2,
                    ProfileId: "music-96",
                    InbandFecEnabled: true,
                    ExpectedPacketLossPercent: 1),
                AudioReceive: new AudioReceptionDiagnostics(
                    Jitter: TimeSpan.FromMilliseconds(4),
                    PacketLossPercent: 0.2,
                    CumulativePacketsLost: 0)),
        ]);

    private static PublisherSnapshot PreviewSnapshot() => new()
    {
        IsAuthenticated = true,
        UserEmail = "publisher@sonicrelay.app",
        DeviceName = Environment.MachineName,
        SessionId = Guid.NewGuid(),
        SessionCode = "K7DRRP",
        ViewerCount = 2,
        SignalingState = SonicRelay.Windows.Signaling.SignalingConnectionState.Connected,
        AudioState = SonicRelay.Windows.Audio.AudioCaptureState.Capturing,
        AudioDiagnostics = new SonicRelay.Windows.Audio.AudioCaptureDiagnostics(
            SonicRelay.Windows.Audio.AudioCaptureState.Capturing,
            Device: null,
            LastError: null,
            Level: new SonicRelay.Windows.Audio.AudioLevelSnapshot(0.72f, 0.41f),
            BytesCaptured: 0,
            FramesCaptured: 0),
        ActivityLog =
        [
            $"{DateTimeOffset.Now:HH:mm:ss} Signed in and publisher device is ready.",
            $"{DateTimeOffset.Now:HH:mm:ss} Session created.",
            $"{DateTimeOffset.Now:HH:mm:ss} Signaling: Connected.",
            $"{DateTimeOffset.Now:HH:mm:ss} Audio capture started.",
        ],
    };
}
