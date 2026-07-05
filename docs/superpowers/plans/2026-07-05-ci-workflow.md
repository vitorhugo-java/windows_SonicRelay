# GitHub Actions CI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Windows GitHub Actions workflow that restores, builds, tests, and preserves test results for pull requests and `main` pushes.

**Architecture:** A single Windows job follows the repository's `global.json` and solution file. The existing PowerShell structure test validates the workflow contract, while README documents the developer-facing behavior.

**Tech Stack:** GitHub Actions YAML, .NET 10, PowerShell, xUnit/TRX

---

### Task 1: Add a failing workflow contract test

**Files:**
- Modify: `tests/Repository.Structure.Tests.ps1`
- Test: `tests/Repository.Structure.Tests.ps1`

- [ ] Add `.github/workflows/ci.yml` to required paths and assert the workflow contains `pull_request`, a `main` push filter, `windows-latest`, `actions/setup-dotnet`, `dotnet restore`, Release build, `dotnet test`, the repository structure test, TRX logging, and `actions/upload-artifact`.
- [ ] Run `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Repository.Structure.Tests.ps1` and confirm it fails because `.github/workflows/ci.yml` is missing.

### Task 2: Implement CI and documentation

**Files:**
- Create: `.github/workflows/ci.yml`
- Modify: `README.md`

- [ ] Create the workflow with `pull_request` and `push` to `main`, a `windows-latest` job, `actions/checkout@v4`, `actions/setup-dotnet@v4` using `global-json-file`, restore, Release build, solution tests with TRX output, the structure test, and `actions/upload-artifact@v4` guarded by `always()`.
- [ ] Add a README CI section listing triggers, stages, test result artifacts, and the absence of secrets/admin runtime dependencies.
- [ ] Re-run the focused PowerShell test and confirm it passes.

### Task 3: Verify and publish

**Files:**
- Verify: `.github/workflows/ci.yml`
- Verify: `README.md`
- Verify: `tests/Repository.Structure.Tests.ps1`

- [ ] Run `dotnet restore SonicRelay.Windows.slnx` and require exit code 0.
- [ ] Run `dotnet build SonicRelay.Windows.slnx --configuration Release --no-restore` and require exit code 0.
- [ ] Run `dotnet test SonicRelay.Windows.slnx --configuration Release --no-build --no-restore` and require exit code 0 with no failed tests.
- [ ] Review `git diff --check`, the scoped diff, and repository status.
- [ ] Commit all scoped files on `main` with `feat: add GitHub Actions CI (closes #11)`, push `main`, verify the remote commit, and verify issue 11 is closed.
