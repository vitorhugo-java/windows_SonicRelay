# Non-admin Requirement Design

## Goal

Make non-admin installation, configuration, and runtime a documented product constraint that future implementation and dependency decisions can reference and verify.

## Design

The README will state the constraint prominently and link to a canonical checklist. The publisher specification will define the user-visible requirement and reject elevation-dependent approaches. The architecture document will translate that requirement into deployment, storage, networking, packaging, and dependency boundaries.

`docs/non-admin-checklist.md` will be the canonical review gate. It will contain every constraint from Issue #3 and explicit risk notes for WinUI 3 packaging, Windows App SDK deployment, and WASAPI loopback capture. The existing repository structure test will verify that the checklist exists, is linked from the README, and retains the critical guardrail language.

## Decisions

- Prefer unpackaged, self-contained, per-user, or portable distribution.
- Store configuration, tokens, logs, and mutable runtime data in user-scoped folders.
- Permit only outbound application-initiated API, signaling, WebRTC, TURN, and STUN communication.
- Reject mandatory services, drivers, kernel components, firewall changes, machine-wide dependencies, protected-folder writes, and HKLM runtime configuration.
- Treat any dependency that needs elevation as incompatible unless it is optional and explicitly documented as outside normal usage.

## Validation

Run `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Repository.Structure.Tests.ps1`. The test must fail before the documentation is added and pass after all required guardrails and links are present.
