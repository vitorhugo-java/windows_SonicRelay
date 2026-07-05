$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$requiredPaths = @(
    'SonicRelay.Windows.slnx'
    'global.json'
    'Directory.Build.props'
    '.editorconfig'
    '.gitignore'
    'src/SonicRelay.Windows.App/SonicRelay.Windows.App.csproj'
    'src/SonicRelay.Windows.Core/SonicRelay.Windows.Core.csproj'
    'src/SonicRelay.Windows.ApiClient/SonicRelay.Windows.ApiClient.csproj'
    'src/SonicRelay.Windows.Signaling/SonicRelay.Windows.Signaling.csproj'
    'src/SonicRelay.Windows.Audio/SonicRelay.Windows.Audio.csproj'
    'src/SonicRelay.Windows.WebRtc/SonicRelay.Windows.WebRtc.csproj'
    'tests/SonicRelay.Windows.Core.Tests/SonicRelay.Windows.Core.Tests.csproj'
    'tests/SonicRelay.Windows.ApiClient.Tests/SonicRelay.Windows.ApiClient.Tests.csproj'
    'docs/windows-publisher.md'
    'docs/architecture.md'
    'docs/non-admin-checklist.md'
)

$missingPaths = $requiredPaths | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $root $_))
}

if ($missingPaths.Count -gt 0) {
    Write-Error "Missing required repository paths:`n$($missingPaths -join "`n")"
}

$readme = Get-Content -Raw -LiteralPath (Join-Path $root 'README.md')
$checklistPath = Join-Path $root 'docs/non-admin-checklist.md'

if ($readme -notmatch '\(docs/non-admin-checklist\.md\)') {
    Write-Error 'README.md must link to docs/non-admin-checklist.md.'
}

$checklist = Get-Content -Raw -LiteralPath $checklistPath
$requiredNonAdminGuardrails = @(
    'no mandatory admin-required installer'
    'no mandatory Windows service'
    'no custom audio driver'
    'no kernel-mode component'
    'no mandatory inbound local firewall port'
    'no write access to Program Files for runtime data'
    'no write access to HKLM registry for runtime configuration'
    'no machine-wide dependency required for normal usage'
    'app data must go to user-scoped folders'
    'network communication must be outbound-only for API/signaling/WebRTC/TURN/STUN'
    'any dependency requiring elevation must be rejected or documented as incompatible'
)

$missingGuardrails = $requiredNonAdminGuardrails | Where-Object {
    $checklist.IndexOf($_, [StringComparison]::OrdinalIgnoreCase) -lt 0
}

if ($missingGuardrails.Count -gt 0) {
    Write-Error "Missing non-admin guardrails:`n$($missingGuardrails -join "`n")"
}

Write-Host "Repository structure verified: $($requiredPaths.Count) required paths found."
