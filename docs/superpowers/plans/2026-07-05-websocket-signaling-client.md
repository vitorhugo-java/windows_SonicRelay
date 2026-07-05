# Authenticated WebSocket Signaling Client Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an authenticated, reconnecting outbound WebSocket signaling client with validated envelopes, safe diagnostics, and focused tests.

**Architecture:** Public protocol/lifecycle contracts live in the Signaling project. `SignalingClient` uses an internal socket adapter so production uses `ClientWebSocket` while tests drive exact frames and failures without network access.

**Tech Stack:** .NET 10, `System.Net.WebSockets`, `System.Text.Json`, xUnit 2.9.

---

### Task 1: Protocol envelope and state contracts

**Files:**
- Create: `src/SonicRelay.Windows.Signaling/SignalingConnectionState.cs`
- Create: `src/SonicRelay.Windows.Signaling/SignalingMessageTypes.cs`
- Create: `src/SonicRelay.Windows.Signaling/SignalingMessageEnvelope.cs`
- Create: `src/SonicRelay.Windows.Signaling/ISignalingMessageHandler.cs`
- Create: `src/SonicRelay.Windows.Signaling/ISignalingClient.cs`
- Test: `tests/SonicRelay.Windows.Signaling.Tests/SignalingMessageEnvelopeTests.cs`

- [ ] Write tests asserting camel-case round trips for every supported type, rejection of malformed/unknown envelopes, and `[REDACTED]` diagnostic payloads for offer/answer/ICE.
- [ ] Run `dotnet test tests/SonicRelay.Windows.Signaling.Tests/SonicRelay.Windows.Signaling.Tests.csproj --filter FullyQualifiedName~SignalingMessageEnvelopeTests` and confirm compilation/test failure because the contracts do not exist.
- [ ] Implement the enum, type set, handler/client interfaces, and envelope parse/serialize/redaction methods with `JsonSerializerDefaults.Web`.
- [ ] Re-run the same focused command and expect all envelope tests to pass.

### Task 2: Socket transport and lifecycle

**Files:**
- Modify: `src/SonicRelay.Windows.Signaling/SonicRelay.Windows.Signaling.csproj`
- Create: `src/SonicRelay.Windows.Signaling/WebSockets/IWebSocketConnection.cs`
- Create: `src/SonicRelay.Windows.Signaling/WebSockets/ClientWebSocketConnection.cs`
- Create: `src/SonicRelay.Windows.Signaling/SignalingClient.cs`
- Create: `src/SonicRelay.Windows.Signaling/Properties/AssemblyInfo.cs`
- Create: `tests/SonicRelay.Windows.Signaling.Tests/TestDoubles.cs`
- Test: `tests/SonicRelay.Windows.Signaling.Tests/SignalingClientTests.cs`

- [ ] Write fake-socket tests asserting WS(S) URL/query construction, Bearer authentication, `publisher.ready`, viewer dispatch, ping/pong, same-session idempotence, different-session rejection, clean `session.ended`, and observable state changes.
- [ ] Run `dotnet test tests/SonicRelay.Windows.Signaling.Tests/SonicRelay.Windows.Signaling.Tests.csproj --filter FullyQualifiedName~SignalingClientTests` and confirm failure because `SignalingClient` does not exist.
- [ ] Implement the internal adapter and minimal client lifecycle, including a single lifecycle lock and normal-close cancellation.
- [ ] Re-run the focused client tests and expect them to pass.

### Task 3: Conservative reconnect

**Files:**
- Modify: `src/SonicRelay.Windows.Signaling/SignalingClient.cs`
- Modify: `tests/SonicRelay.Windows.Signaling.Tests/TestDoubles.cs`
- Modify: `tests/SonicRelay.Windows.Signaling.Tests/SignalingClientTests.cs`

- [ ] Add tests that inject an immediate delay and scripted transient close, then assert `Reconnecting`, a replacement socket, a second `publisher.ready`, and `Faulted` after three failed retries.
- [ ] Run the focused client test command and confirm the reconnect tests fail for missing behavior.
- [ ] Add transient classification and bounded 1/2/4-second backoff through an internal delay abstraction.
- [ ] Re-run the focused tests and expect them to pass.

### Task 4: Project wiring and docs

**Files:**
- Create: `tests/SonicRelay.Windows.Signaling.Tests/SonicRelay.Windows.Signaling.Tests.csproj`
- Modify: `SonicRelay.Windows.slnx`
- Modify: `README.md`

- [ ] Reference Core from Signaling and reference Core/Signaling from the test project without adding runtime packages.
- [ ] Add the focused test project to the solution.
- [ ] Document authentication, signaling-only transport, reconnection, and non-admin outbound behavior in README.
- [ ] Run `dotnet test tests/SonicRelay.Windows.Signaling.Tests/SonicRelay.Windows.Signaling.Tests.csproj` and expect zero failures.
- [ ] Run `dotnet build src/SonicRelay.Windows.Signaling/SonicRelay.Windows.Signaling.csproj --no-restore` and expect exit code 0.
- [ ] Inspect `git diff --check`, `git diff --stat`, and the final scoped diff before committing.

## Plan self-review

Every design requirement maps to a task, file paths and commands are exact, names are consistent, and there are no deferred implementation placeholders. The work remains one testable subsystem and does not include application UI composition or media behavior.
