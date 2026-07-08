# Publisher Dashboard — Implementation Plan

Spec: `docs/superpowers/specs/2026-07-08-publisher-dashboard-design.md`
Issue: #25

## Step 1 — Locked palette

- `Styles/SonicPalette.xaml` with all `Sonic.*` named brushes + badge variants +
  visualizer gradient stops; merge in `App.xaml`.

## Step 2 — ViewModel (TDD, Presentation)

- Add a WebRtc project reference to Presentation (for stats types).
- `DashboardViewModel` + `DashboardBadge` + `DashboardViewModel.From(...)` +
  `DesignTime`; `DashboardViewModelTests`.

## Step 3 — Controls (App)

- `StatusBadgeControl`, `MetricCardControl`, `AudioVisualizerControl`,
  `ConnectionStatusCard`, `QualityMetricsCard`.

## Step 4 — Page + wiring

- `PublisherDashboardPage`: subscribe to workflow + WebRTC diagnostics, build the
  ViewModel, push to cards; design-time mock. Point the "Dashboard" nav item at it
  and remove the old placeholder page.

## Step 5 — Verify + docs

- `dotnet build`, `dotnet test`; document the dashboard in `docs/windows-publisher.md`.
