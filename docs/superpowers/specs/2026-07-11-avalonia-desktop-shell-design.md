# Avalonia Desktop Shell — Design Spec (Issue #32, Phase 2)

## Provenance / Context

- **Date:** 2026-07-11
- **Author session:** Claude Code (Opus 4.8) — `session_01AmtwTNXAmSKoWU9MeiNzm2`
  (https://claude.ai/code/session_01AmtwTNXAmSKoWU9MeiNzm2)
- **Driven by:** GitHub issue
  [#32 — "Adicionar suporte ao Linux com arquitetura multiplataforma"](https://github.com/vitorhugo-dotnet/windows_SonicRelay/issues/32)
- **Specific mandate:** the follow-up comment
  [#issuecomment-4939155969](https://github.com/vitorhugo-dotnet/windows_SonicRelay/issues/32#issuecomment-4939155969),
  which states PR #34 delivered only the architectural foundation (Phase 1) and that the
  **next expected PR is Phase 2 — the new Avalonia shell on Windows**.

This document was produced during a chat where the maintainer asked to "implement issue #32,
do like explained in that comment." The scope of Phase 2 is large, so this spec captures the
agreed context and the recommended incremental approach before implementation begins.

## Background

The SonicRelay desktop publisher is currently Windows-only: **WinUI 3 / Windows App SDK** for
the UI and **WASAPI Loopback** for system-audio capture. Issue #32's long-term goal is to run
the publisher on Linux too, without duplicating app logic or destabilising the Windows version.

The chosen architecture (issue #32) is a shared **Avalonia UI** desktop app with platform-specific
adapters isolated behind interfaces. The rollout is staged deliberately:

1. **Phase 1 — Architecture extraction.** ✅ Delivered by PR #34: runtime, platform contracts,
   and the state projection moved into `SonicRelay.Windows.Presentation` (platform-agnostic view
   models). It did **not** ship any new visual shell.
2. **Phase 2 — New Avalonia shell on Windows.** ← This spec. Reproduce the Lovable prototype as a
   real Avalonia desktop app on Windows, bound to real session data, keeping WASAPI capture and
   the existing auth/signaling/WebRTC flow. WinUI stays until minimum functional parity is proven.
3. Phase 3+ — Linux (PipeWire) adapter, distribution. Out of scope here.

## Current codebase state (relevant facts)

- Solution: `SonicRelay.Windows.slnx`, .NET 10, projects under `src/`:
  `App` (WinUI 3), `Presentation`, `Core`, `ApiClient`, `Signaling`, `WebRtc`, `Audio`.
- `SonicRelay.Windows.Presentation` (from PR #34) holds the platform-agnostic layer:
  - `DashboardViewModel` — pure projection of publisher/signaling/WebRTC/audio state into
    always-non-null, UI-friendly display values (`SessionStatusText`/`Badge`,
    `SignalingStatusText`/`Badge`, `WebRtcStatusText`/`Badge`, `ConnectionModeText`,
    `SessionCodeText`, `ViewerCount`, `IsCapturing`, `AudioPeak`/`AudioRms`, `LatencyText`,
    `JitterText`, `PacketLossText`, `BitrateText`). Includes a `DesignTime` sample and a
    `DashboardBadge` semantic enum (`Success/Warning/Danger/Neutral`).
  - `PublisherSnapshot` — the immutable session state record (auth, device, session, viewer
    count, signaling state, audio state + diagnostics, busy flag, error, activity log) plus
    `Can…` command guards.
  - `PublisherWorkflow`, `TrayApplicationController`.
- There is **no** type literally named `PublisherUiState`; the comment's "estados definidos em
  `PublisherUiState`" refers to the state projection currently expressed by `DashboardViewModel`
  + `PublisherSnapshot`. The issue's canonical UI-state list
  (`LoggedOut, Authenticating, Idle, CreatingSession, WaitingViewer, ConnectingSignaling,
  ConnectingWebRtc, StreamingDirect, StreamingRelay, Reconnecting, Faulted, Ended`) should be
  introduced as an explicit enum derived from the snapshot as part of Phase 2.
- `JitterText`, `PacketLossText`, `BitrateText` are currently `Unknown` ("—"): WebRTC `getStats`
  jitter/packet-loss/bitrate are **not yet plumbed**. Wiring these is part of the "real metrics,
  no permanent mocks" requirement.
- Existing WinUI app already has an ad-hoc design system to mine for tokens:
  `App/Styles/DesignTokens.xaml`, `App/Styles/SonicPalette.xaml`, and controls
  (`ConnectionStatusCard`, `MetricCardControl`, `QualityMetricsCard`, `StatusBadgeControl`,
  `AudioVisualizerControl`) and pages.

## Design references

- **Live prototype (authoritative):** https://delight-fusion-app.lovable.app — confirmed
  accessible. Dark theme, card-based layout, monospace for technical data.
- **Issue screenshots:** six images attached to issue #32, matching the live prototype.

Observed prototype layout (to reproduce, adapted to desktop conventions):

- **Top bar:** "VH" brand mark, "SonicRelay Publisher" title, account email + sign-out, a global
  transmission-status indicator ("STREAMING ACTIVE").
- **Sidebar navigation** (left).
- **Broadcast Session card:** highlighted session code (e.g. `K7DRRP`) with copy + share.
- **Audio Monitor:** real-time peak level (e.g. `-3.2 dB`) with visualisation.
- **Signal Infrastructure card:** Session / Signaling / WebRTC-ICE status, Direct-vs-Relay mode,
  viewer count.
- **Stream Quality metrics:** Latency/RTT, Jitter, Packet loss.
- **Bandwidth:** bitrate (e.g. `96 kbps`) + Opus profile label.
- **Technical console (bottom):** terminal-style event log.
- **System resources:** CPU / RAM footer indicators.

## Objective of Phase 2

Ship a real, running **Avalonia desktop app on Windows** that reproduces the prototype's shell
and design system, binds its indicators to **real session data** (no permanent mocks), and keeps
WASAPI capture plus the existing auth / signaling / WebRTC / Opus flow — without regressing the
current WinUI app (which remains the shipped UI until parity is reached).

## Recommended approach — incremental (vertical slice first)

Building the entire Phase 2 (new app + full design system + every component + tray + reconnect +
full parity) as one blind PR is high-risk and hard to review. Recommended breakdown:

- **PR 1 (this slice):** new Avalonia app project + centralized design tokens (palette, spacing,
  radius, typography, states; dark theme default) + the full dashboard shell layout (sidebar,
  top bar, infrastructure/quality/bandwidth/session cards, audio monitor, technical console),
  bound to the existing `DashboardViewModel` / `PublisherSnapshot` with real data where the
  Presentation layer already provides it. Runs side-by-side with WinUI; does not replace it.
- **PR 2:** plumb real WebRTC `getStats` jitter / packet-loss / bitrate into the Presentation
  layer so those tiles stop reading "—"; introduce the explicit UI-state enum.
- **PR 3:** tray, minimize-to-tray, reconnection, remaining pages; validate functional parity.
- **PR 4:** flip default to Avalonia, retire WinUI once parity is confirmed.

Rationale: each PR is independently reviewable and leaves the app runnable; the rewrite of the UI
is not entangled with new metrics plumbing or the later Linux audio work.

## Architecture (target)

- New project `src/SonicRelay.Windows.Desktop` (Avalonia, .NET 10), referencing the existing
  shared layers (`Presentation`, `Core`, `ApiClient`, `Signaling`, `WebRtc`, `Audio`). The
  view/view-model layer depends only on `Presentation` abstractions — no direct WASAPI / WinUI /
  Win32 knowledge in views or view models (per issue #32's platform-abstraction rule).
- **Design tokens** as Avalonia resources (`Styles/`): colors, spacing, radii, typography,
  semantic state brushes — no hard-coded visual values scattered in components.
- **Reusable components** (issue #32 list), each a self-contained Avalonia control:
  `ConnectionStatusBadge`, `MetricProgressBar`, `AudioLevelMonitor`, `SessionCodeCard`,
  `BandwidthGauge`, `InfrastructureStatusCard`, `TechnicalConsole`, `SidebarNavigation`,
  `AccountStatusHeader`.
- **UI states:** introduce the canonical enum listed above, projected from `PublisherSnapshot`;
  each state defines allowed actions, indicators, error text, retry availability, and which
  metrics are shown.
- **Real metrics binding:** components bind to `DashboardViewModel` fields (RTT, jitter, packet
  loss, bitrate, codec, ICE candidate pair / Direct-Relay, viewer count, signaling state, audio
  peak/level). Fields not yet plumbed render as "—" until PR 2 wires them — no permanent fake data.

## Desktop UX requirements (from issue #32)

Resizable window without breaking cards; minimum width/height; reflow (reorganize cards) on
smaller screens rather than shrinking everything; keyboard navigation with visible focus;
accessible text for icons/indicators; avoid heavy animations that could interfere with capture;
reduce metric refresh rate when the window is minimized.

## Testing

- Unit tests for any new state-projection logic (UI-state enum derivation, formatting) live in
  `Presentation.Tests` and stay UI-free.
- Avalonia view rendering validated manually against the live prototype (visual parity) and, where
  practical, headless Avalonia UI smoke tests for the shell.

## Out of scope (this spec / Phase 2)

- Linux / PipeWire capture and any Linux packaging (Phases 3–5).
- Removing the WinUI app before parity is proven.
- Full light theme (dark theme is the default; light is prepared, not required).
- Pixel-perfect reproduction of the web prototype without desktop adaptation.
- Backend changes.

## Open questions (to confirm before/along implementation)

1. Confirm the incremental slicing above (vs. one large Phase-2 PR).
2. New project name: `SonicRelay.Windows.Desktop` acceptable? (A cross-platform-neutral name such
   as `SonicRelay.Desktop` may be preferable given the eventual Linux target.)
3. Avalonia MVVM toolkit preference (CommunityToolkit.Mvvm vs. ReactiveUI vs. plain).
