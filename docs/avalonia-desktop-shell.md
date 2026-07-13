# Avalonia desktop shell (issue #32, phase 2)

The shared desktop shell that will run the publisher on **both Windows and Linux** is being
built in Avalonia UI, staged behind the current WinUI app. This document covers the first
slice: the new shell project, its design system, and the reusable components — bound to the
same shared presentation projection the WinUI dashboard already uses.

Design spec: [`docs/superpowers/specs/2026-07-11-avalonia-desktop-shell-design.md`](superpowers/specs/2026-07-11-avalonia-desktop-shell-design.md).
Reference prototype: <https://delight-fusion-app.lovable.app>.

## What ships in this slice

- **`src/SonicRelay.Windows.Desktop`** — an Avalonia (`net10.0`) desktop app that references
  the existing shared layers (`Presentation`, `Core`, `ApiClient`, `Signaling`, `WebRtc`,
  `Audio`). Views and view models depend only on the `Presentation` abstractions — no WASAPI,
  WinUI, or Win32 knowledge — so the same shell drives the future Linux/PipeWire adapter.
- **Centralized design tokens** (`Styles/Tokens.axaml`) — palette, spacing, radii, typography
  and semantic status brushes, ported from the locked `Sonic.*` WinUI palette so the two UIs
  stay visually aligned. Dark theme is the default; a `Light` variant is defined (not the
  shipping theme this phase) so a future light theme needs no component changes.
  `Styles/Controls.axaml` exposes the recurring style classes (`card`, `card-title`,
  `metric-label`, `metric-value`, `mono`, …). Components never hard-code visual values.
- **The nine reusable components** from issue #32, each self-contained
  (`src/SonicRelay.Windows.Desktop/Controls`): `ConnectionStatusBadge`, `MetricProgressBar`,
  `AudioLevelMonitor`, `SessionCodeCard`, `BandwidthGauge`, `InfrastructureStatusCard`,
  `TechnicalConsole`, `SidebarNavigation`, `AccountStatusHeader`.
- **The dashboard shell** (`Views/MainWindow.axaml`) — sidebar rail, top bar with the global
  transmission status and account area, broadcast-session code, real-time audio monitor,
  signal-infrastructure and stream-quality cards, bandwidth gauge, and the technical console.
  The window is resizable with a minimum size and reflows its cards (they wrap, rather than
  shrink) on narrower widths; the sidebar and actions are keyboard-focusable.

## Real data, no permanent mocks

`DashboardShellViewModel.Update(snapshot, diagnostics, forceRelay)` is the single binding path
every component reads from. It runs the shared, unit-tested projection
(`DashboardViewModel.From` + `PublisherUiStateResolver.Resolve`) and republishes the results
as change-notifying properties — session code, viewer count, Session/Signaling/WebRTC-ICE
status, Direct/Relay mode, audio peak/dB, and RTT all come from real session state.

Metrics the presentation layer does not yet supply — **jitter, packet loss, bitrate** — render
as `—` (never fabricated). Wiring them from WebRTC `getStats` is the next slice, exactly as in
the WinUI dashboard (see [`windows-publisher.md`](windows-publisher.md)).

`MainWindowViewModel.Attach(PublisherRuntime)` is the seam that binds a live runtime:
it subscribes to `Workflow.StateChanged` and `IWebRtcPublisher.DiagnosticsChanged` and rebuilds
the shell on the UI thread, and its contextual commands (create/start/stop/end/retry) map to
`PublisherWorkflow`, gated by `PublisherUiCapabilities` for the current state. With no runtime
attached the app launches in a **preview** state (a representative snapshot) so the layout and
design system can be validated without a backend — this is a bootstrap placeholder, overwritten
by `Update` the moment a runtime is attached.

## Scope boundaries

- WinUI stays the shipped UI until the Avalonia shell reaches minimum functional parity; this
  slice runs **side by side** and does not replace it.
- The sign-in flow, tray/minimize-to-tray, reconnection UX, the non-dashboard pages, and the
  live-runtime startup wiring are later phase-2 slices; the disabled sidebar entries are their
  placeholders.
- Linux/PipeWire capture and packaging are phases 3–5.

## Running and testing

The shell builds and runs cross-platform. Verified on this repo's `net10.0` toolchain:

```bash
dotnet build src/SonicRelay.Windows.Desktop/SonicRelay.Windows.Desktop.csproj
dotnet test  tests/SonicRelay.Windows.Desktop.Tests/SonicRelay.Windows.Desktop.Tests.csproj
```

`tests/SonicRelay.Windows.Desktop.Tests` holds UI-free projection tests
(`DashboardShellViewModelTests`) and headless Avalonia smoke tests (`ShellRenderTests`) that
rasterize the full shell. Set `SHELL_SHOT_DIR` when running the tests to write a
`shell-preview.png` of the rendered shell for visual review against the prototype.
