# Persistent Session Restore — Implementation Plan

Spec: `docs/superpowers/specs/2026-07-07-session-restore-design.md`
Issue: #21

## Step 1 — Workflow restore (TDD)

- Extract `PrepareAuthenticatedStateAsync` from `SignInAndPrepareDeviceAsync`.
- Add `PublisherWorkflow.RestoreSessionAsync` (unauthorized ⇒ clear + login;
  network errors ⇒ silent unauthenticated).
- Tests in `PublisherWorkflowTests` (+ `FakeAuth.GetCurrentUserException`).

## Step 2 — Startup wiring

- `App.ConfigureBackendAsync` fires `RestoreSessionAsync()` after runtime creation.

## Step 3 — Verify + docs

- `dotnet build`, `dotnet test`; note startup restore in `docs/windows-publisher.md`.
