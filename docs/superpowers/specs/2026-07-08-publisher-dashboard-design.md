# Publisher Dashboard Design

Issue: #25

## Goal

A Windows publisher dashboard matching the Flutter Audio Monitor dark style: rounded
cards, subtle borders, teal/blue audio visualizer, and status badges — driven by the
existing publisher/signaling/WebRTC/audio state. Business logic stays out of XAML; a
ViewModel projects state into UI-friendly, always-non-null display values.

## Locked palette (App-level ResourceDictionary)

`Styles/SonicPalette.xaml`, merged in `App.xaml`, defines the exact tokens from the
issue as named brushes. Required semantic brushes:
`Sonic.AppBackgroundBrush`, `Sonic.CardBackgroundBrush`,
`Sonic.CardBackgroundElevatedBrush`, `Sonic.CardBorderBrush`,
`Sonic.TextPrimaryBrush`, `Sonic.TextSecondaryBrush`, `Sonic.TextMutedBrush`,
`Sonic.AccentTealBrush`, `Sonic.AccentBlueBrush`, `Sonic.SuccessBrush`,
`Sonic.WarningBrush`, `Sonic.DangerBrush`, `Sonic.NeutralBrush`. Badge
background/border variants and the visualizer gradient stops
(`#2F5FA8` → `#2DD4BF`) are defined alongside. Card radius 16, 1px `CardBorder`,
20px padding, 16px gaps. No new colors, no default Windows blue as brand.

## ViewModel (Presentation, unit-tested)

`DashboardViewModel` is a pure projection built by
`DashboardViewModel.From(PublisherSnapshot?, WebRtcPublisherDiagnostics?, bool forceRelay)`:

- **Session status** → `Streaming` (capturing), `Waiting` (session, not capturing),
  `Error` (error message), else `Idle`, each with a `DashboardBadge`.
- **Signaling status** → text + badge from `SignalingConnectionState`.
- **WebRTC/ICE status** → aggregated from per-viewer `PeerConnectionState`
  (`Connected` wins, then `Failed`, `Checking`, `Disconnected`, else `Idle`).
- **Connection mode** → `Relay` when the selected candidate pair is relay or relay is
  forced, `Direct` when a pair is selected without relay, else `Unknown`.
- **Latency** → real `EstimatedRoundTripTime` from diagnostics, else `—`.
- **Jitter / packet loss / bitrate** → `—` until getStats plumbing exists (a
  documented follow-up); shown safely as unknown, never null/empty.
- **Audio level** → peak/rms from `AudioCaptureDiagnostics.Level` for the visualizer.
- `DesignTime` static mock so the page previews without a live session.

`DashboardBadge { Success, Warning, Danger, Neutral }`.

## Controls (App)

Lightweight WinUI `UserControl`s, no charting dependency:

- `StatusBadgeControl` — `Text` + `Kind` → pill with palette foreground/background/border.
- `MetricCardControl` — `Label` + `Value` metric tile.
- `AudioVisualizerControl` — `Level` + `IsActive`; a row of bars with the vertical
  teal/blue gradient, eased toward a per-bar target by a ~30fps timer so it animates
  while capture is active and settles to a flat idle line otherwise.
- `ConnectionStatusCard` — session/signaling/WebRTC/mode badges from the ViewModel.
- `QualityMetricsCard` — latency/jitter/packet-loss/bitrate metric tiles.
- `PublisherDashboardPage` — composes the cards + visualizer; subscribes to
  `Workflow.StateChanged` and `IWebRtcPublisher.DiagnosticsChanged`, rebuilds the
  ViewModel, and pushes it to the cards. Shows `DesignTime` data in the XAML designer.

The page replaces the old placeholder `DashboardPage` as the "Dashboard" nav target.

## Tests

`DashboardViewModelTests` (Presentation): status/badge mapping for idle/waiting/
streaming/error, signaling and WebRTC aggregation, connection-mode derivation
(relay pair / forced / direct / unknown), latency formatting, unknown-safe metrics,
and audio level passthrough. Controls are verified by `dotnet build` + manual QA.

## Acceptance

Builds without admin; dark dashboard matching the Flutter style through the locked
named palette; visualizer animates on capture; connection/quality cards update from
real state; unknown values shown as `—`; no random colors; no charting dependency;
`dotnet build` + `dotnet test` pass.
