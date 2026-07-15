# Connection-Loss Diagnostics: Export, Clear, Retention, and Richer Events

## Goal

The publisher still loses connection when the window is minimized, the app is closed, or
even while it stays open in the foreground, and today's diagnostics can't say why. Extend
the existing `DiagnosticLog`/`DiagnosticReportExporter` pair so a user can capture a
support-ready log covering exactly what happened around a drop, export it, clear it, and
never accumulate an unbounded pile of `.jsonl` files on disk.

## Scope

In scope: retention/cleanup of on-disk logs, a "Clear logs" action, and new diagnostic
events at the transitions most likely to correlate with a loss (minimize/restore to tray,
window close, reconnect attempts, ICE restarts, and the concrete reason a signaling
WebSocket closed). Out of scope: fixing the underlying reconnection behavior itself (that
already exists — jitmath backoff, ICE restart on `participant.reconnected`) and any
server-side change (tracked separately in `dotnet_SonicRelay`).

## Architecture

`DiagnosticLog` gains two additions, both in `SonicRelay.Windows.Core.Diagnostics`:

- **Retention**: on construction, `DiagnosticLog` deletes `publisher-*.jsonl` files in its
  log directory older than a configurable threshold (default 3 days). This runs once per
  process start, best-effort (I/O errors are swallowed the same way `WriteAsync` already
  swallows them — diagnostics must never interrupt publisher operation).
- **Clear**: a new `ClearAsync()` method deletes every `publisher-*.jsonl` file under the
  log directory and empties `RecentEvents`. Guarded by the same `writeLock` used for
  writes, so a clear can't race an in-flight append.

New call sites write additional `DiagnosticEvent` categories through the existing
`WriteDiagnosticAsync` wrapper in `PublisherRuntime`:

- `window-state`: minimized to tray / restored from tray / close-to-tray vs. real exit
  (wired from the tray/shell code added in the close-to-tray work).
- `signaling-closed`: fired when the signaling client's connection ends, carrying a
  bounded `reason` property (`normal`, `timeout`, `transport-error`, `server-closed`,
  `cancelled`) rather than a raw exception message. `SignalingClient` needs a small change
  to surface *why* the socket closed instead of just that it closed, mirroring the
  category split the API side will add for the same purpose.
- `reconnect-attempt` / `ice-restart`: existing reconnect-with-jitter and ICE-restart code
  paths gain a diagnostic write at the point they already fire, using data already
  available there (no new state needed).

## UI

The Diagnostics page gets a **Clear logs** button next to the existing **Export** button.
Clearing shows the same sanitized-feedback pattern the export path uses (success or a
sanitized failure message, never a raw exception). A confirmation prompt is required
before clearing, since it is destructive and not undoable.

## Data and safety

Retention and clear reuse the existing redaction and user-scoped storage guarantees — no
new surface for credentials or PII. The `reason` properties added to new events are drawn
from a fixed enum, never from free-text exception messages, so they can't leak connection
strings or stack traces into the log file.

## Verification

Core tests cover: cleanup deletes files older than the threshold and keeps newer ones,
`ClearAsync` empties both the file(s) and `RecentEvents`, and the new event categories
serialize through the existing redactor without exposing raw properties. A manual pass
minimizes/restores the app and force-closes the signaling socket to confirm the new events
appear with a correct `reason`.
