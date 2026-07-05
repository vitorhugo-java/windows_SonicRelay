# Non-admin Release Smoke Test Design

## Goal

Define a repeatable release gate that proves the portable Windows Publisher ZIP can be downloaded, extracted, configured, exercised, and cleaned up by a standard Windows user without elevation.

## Approaches considered

1. **Manual checklist plus repository contract test (selected):** document realistic clean-user steps and validate that the checklist and README link remain present. This covers the issue acceptance criteria without pretending that CI can reproduce managed Windows policies or UAC behavior.
2. **Manual checklist only:** smallest documentation change, but future edits could silently remove the release gate or required scenarios.
3. **Automated ZIP inspection:** useful for archive composition, but it cannot prove UAC, settings, login, audio-device, or user-profile behavior and is optional for this issue.

## Design

`docs/release-smoke-test.md` is the canonical operator checklist. It records the release URL, artifact name, Windows environment, account type, and results. Preconditions require a standard non-admin account and a clean user-owned extraction folder. The steps cover download, extraction, launch, settings, backend configuration, login attempt, user-scoped config, clearing local state, missing backend, missing audio device, and cleanup.

Each required behavior is an explicit pass/fail gate. Any UAC prompt, Program Files requirement, service or driver installation, firewall modification request, protected-location write, crash, or unhandled failure blocks the release. The document distinguishes expected graceful error states from successful connectivity or audio capture.

`README.md` links the smoke test from both the portable-release instructions and the documentation index. `tests/Repository.Structure.Tests.ps1` verifies the document exists, the README links it, and the checklist contains the core non-admin release gates.

## Error handling and evidence

Testers record failures with the step, Windows version, account type, artifact version, visible error, and relevant user-scoped logs. They must not work around a failure with elevation, machine-wide installation, firewall exceptions, services, or drivers. A failed mandatory item keeps the release blocked until corrected and rerun from a clean standard-user environment.

## Verification

Use TDD for the repository contract: first add assertions and observe failure because the checklist is absent, then add the document and README links and rerun the focused PowerShell test. Finish with Markdown/link checks and `git diff --check`; no full .NET suite is necessary because runtime code is unchanged.
