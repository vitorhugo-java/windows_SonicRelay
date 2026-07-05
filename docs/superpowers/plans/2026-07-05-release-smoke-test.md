# Non-admin Release Smoke Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a realistic, enforced manual release gate for running the portable Windows Publisher ZIP as a standard user.

**Architecture:** Keep environment-dependent validation in a manual checklist and add a focused repository contract test for document presence, discoverability, and mandatory scenarios. Link the checklist from existing portable-release documentation.

**Tech Stack:** Markdown, PowerShell

---

### Task 1: Add the failing documentation contract

**Files:**
- Modify: `tests/Repository.Structure.Tests.ps1`
- Test: `tests/Repository.Structure.Tests.ps1`

- [x] Require `docs/release-smoke-test.md` in the repository path list.
- [x] Require a README link and the mandatory non-admin smoke-test gates.
- [x] Run `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Repository.Structure.Tests.ps1` and confirm failure because the checklist does not exist.

### Task 2: Add the release smoke-test checklist

**Files:**
- Create: `docs/release-smoke-test.md`

- [x] Document clean standard-user prerequisites and result metadata.
- [x] Add pass/fail steps for download, extraction, non-elevated launch, settings, backend URL, login, user-scoped config, clearing state, unavailable backend, and unavailable audio device.
- [x] Make UAC, Program Files, service, driver, and firewall changes explicit release blockers.
- [x] Add failure evidence and cleanup instructions.

### Task 3: Link and verify

**Files:**
- Modify: `README.md`
- Test: `tests/Repository.Structure.Tests.ps1`

- [x] Link the checklist from portable-release instructions and the documentation index.
- [x] Rerun the focused PowerShell test and confirm it passes.
- [x] Run `git diff --check` and inspect the scoped diff.

### Task 4: Publish and close issue 13

**Files:**
- Commit only the files listed by this plan.

- [ ] Commit on `main` with a message that closes issue 13.
- [ ] Push `main` to `origin`.
- [ ] Confirm issue 13 is closed and report the commit and verification.
