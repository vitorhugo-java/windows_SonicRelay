# Authenticated WebSocket Signaling Client Design

## Scope

Implement the Windows Publisher signaling boundary only. The component opens one outbound authenticated WebSocket per active session, exchanges JSON control messages, exposes connection state, dispatches supported envelopes, and reconnects conservatively after transient transport failures. It does not transport audio or create SDP, ICE, WebRTC, local listeners, ports, services, or firewall rules.

## Protocol

The configured signaling base URL is normalized from HTTP(S) to WS(S) when needed. Existing path and query values are preserved, and escaped `sessionId` and `deviceId` query parameters are appended. Authentication uses the current access token in `Authorization: Bearer <token>` during the WebSocket handshake.

Messages use a JSON envelope with camel-case fields: `type`, optional `sessionId`, optional `viewerId`, and optional `payload`. Supported types are `publisher.ready`, `viewer.ready`, `webrtc.offer`, `webrtc.answer`, `webrtc.ice_candidate`, `session.joined`, `session.left`, `session.ended`, `ping`, `pong`, and `error`. The publisher sends `publisher.ready` after every successful connection and answers `ping` with `pong`. Viewer identity remains in the envelope so later WebRTC work can route multiple viewers independently.

Malformed JSON, missing/unknown types, and structurally invalid envelopes are rejected before dispatch. Diagnostic redaction preserves routing metadata but replaces payloads for SDP/ICE message types with a fixed redaction marker.

## Components

- `ISignalingClient` defines connect, send, clean close, current state, and state-change notification.
- `SignalingClient` owns lifecycle serialization, token loading, URL construction, the receive loop, message dispatch, ping/pong, and reconnect policy.
- `ISignalingMessageHandler` receives validated envelopes without transport concerns.
- `SignalingMessageEnvelope` owns validated protocol serialization/deserialization and safe diagnostic formatting.
- `SignalingConnectionState` represents `Disconnected`, `Connecting`, `Connected`, `Reconnecting`, `Closing`, `Closed`, and `Faulted`.
- An internal WebSocket adapter/factory isolates `ClientWebSocket` so protocol and lifecycle behavior are deterministic in tests.

## Lifecycle and failures

Only one active lifecycle is allowed. Repeating `ConnectAsync` for the same session/device is idempotent; attempting another session while active fails explicitly. The initial connection failure moves to `Faulted` and is returned to the caller. After a successful connection, transient WebSocket, I/O, or premature-close failures enter `Reconnecting` and retry at 1, 2, and 4 seconds. Authentication, cancellation, normal closure, explicit close, and `session.ended` do not reconnect. Exhausted retries end in `Faulted`.

`CloseAsync` cancels reconnect/receive work, sends a normal close when possible, and finishes in `Closed`. State notifications occur only when the value changes.

## Tests and documentation

Focused tests cover envelope round trips, invalid envelopes, sensitive payload redaction, URL/authentication/publisher readiness, dispatch, ping/pong, clean session ending, duplicate prevention, and state transitions. Documentation states that WebSocket carries signaling control messages only; audio remains a future WebRTC concern.

## Design self-review

The design contains no placeholders. It covers every issue message type and acceptance criterion, keeps multi-viewer routing explicit, and excludes the prohibited media work. The URL path is intentionally configuration-owned because the backend repository currently exposes no signaling contract to discover.
