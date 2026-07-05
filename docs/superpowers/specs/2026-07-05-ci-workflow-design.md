# GitHub Actions CI Design

## Goal

Add deterministic GitHub Actions validation for pull requests and pushes to `main`, covering restore, Release build, all .NET tests, the repository structure test, and test-result retention.

## Chosen approach

Use one Windows job in `.github/workflows/ci.yml`. `actions/setup-dotnet` reads `global.json`, keeping the SDK version in one source of truth. Explicit restore, build, and test steps make failures attributable and ensure no test is silently skipped. NuGet caching is intentionally omitted because the repository has no tracked `packages.lock.json`; enabling the built-in cache would be unsafe without adding dependency-locking changes outside this issue.

Alternatives considered were a matrix split by project and a reusable workflow. Both add concurrency and maintenance overhead without improving coverage for this single-solution Windows repository, so they are outside this issue.

## Workflow behavior

- Trigger for every pull request and for pushes to `main`.
- Run on `windows-latest` with the .NET SDK selected by `global.json`.
- Restore `SonicRelay.Windows.slnx`, then build it in Release with `--no-restore`.
- Test the solution in Release with `--no-build --no-restore`, writing TRX files below `TestResults`.
- Run `tests/Repository.Structure.Tests.ps1` so non-admin and repository guardrails remain enforced.
- Upload TRX files with `actions/upload-artifact` under `if: always()` and ignore the absence of files, while the test step itself still fails normally.
- Use no secrets, release publishing, deployment, warning suppression, or runtime changes.

## Documentation and validation

README documents the same CI triggers and stages. The existing repository structure test gains workflow assertions and is run red/green before the workflow is added. Local verification uses only the focused structure test plus solution restore, Release build, and solution tests.
