param([string]$CoreRevision)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $CoreRevision) {
    $CoreRevision = (Get-Content -LiteralPath (Join-Path $windowsRepo 'dependencies.lock.json') -Raw | ConvertFrom-Json).coreRevision
}
$coreRepo = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore'
$root = Join-Path $windowsRepo ('.artifacts\clean-checkout\' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force | Out-Null
foreach ($entry in @(@($windowsRepo,'WordSuggestorWindows','HEAD'), @($coreRepo,'WordSuggestorCore',$CoreRevision))) {
    $archive = Join-Path $root ($entry[1] + '.zip')
    & git -C $entry[0] archive --format=zip -o $archive $entry[2]
    if ($LASTEXITCODE -ne 0) { throw "Cannot export $($entry[1]) at $($entry[2])." }
    Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $root $entry[1])
}
$cleanWindows = Join-Path $root 'WordSuggestorWindows'
# Existing installed toolchains remain prerequisites. No source build output or
# local repository caches are copied into either exported checkout.
& (Join-Path $cleanWindows 'scripts\build_app.ps1')
& (Join-Path $cleanWindows 'scripts\test_core_cli.ps1')
& (Join-Path $cleanWindows 'scripts\test_app.ps1')
Write-Output "PASS clean committed exports: $root"
