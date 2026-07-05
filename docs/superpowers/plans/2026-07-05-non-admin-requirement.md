# Non-admin Requirement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce SonicRelay Windows Publisher's non-admin installation and runtime constraint in documentation and repository guardrails.

**Architecture:** Keep one canonical checklist and summarize its consequences in the README, publisher specification, and architecture. Extend the focused PowerShell structure test to prevent accidental removal of the checklist, README link, or critical constraints.

**Tech Stack:** Markdown, PowerShell, GitHub CLI

---

### Task 1: Add the failing documentation guardrail

**Files:**
- Modify: `tests/Repository.Structure.Tests.ps1`

- [x] Add `docs/non-admin-checklist.md` to required paths and assertions for the README link and critical checklist phrases.
- [x] Run `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Repository.Structure.Tests.ps1` and expect failure because the checklist does not exist.

### Task 2: Document the non-admin constraint

**Files:**
- Modify: `README.md`
- Modify: `docs/windows-publisher.md`
- Modify: `docs/architecture.md`
- Create: `docs/non-admin-checklist.md`

- [x] Add a prominent README statement and checklist link.
- [x] Add the exact `Non-admin requirement` section to the publisher specification.
- [x] Add architecture deployment constraints covering packaging, storage, networking, and dependencies.
- [x] Add the canonical checklist with every item required by Issue #3 and honest WinUI 3, Windows App SDK, and audio-capture risks.
- [x] Run the focused PowerShell test and expect success.

### Task 3: Verify and publish

**Files:**
- Review all files changed by Tasks 1 and 2.

- [x] Scan spec and plan for placeholders or contradictions.
- [x] Run the focused PowerShell test again from a clean command invocation.
- [x] Review `git diff --check`, `git diff --stat`, and the complete scoped diff.
- [x] Commit the scoped files on `main`, push `main`, and close Issue #3 with the commit reference and validation result.
