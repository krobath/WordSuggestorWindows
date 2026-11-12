param([string]$CoreRevision, [switch]$IncludeWorkingTree)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $CoreRevision) {
    $CoreRevision = (Get-Content -LiteralPath (Join-Path $windowsRepo 'dependencies.lock.json') -Raw | ConvertFrom-Json).coreRevision
}
$coreRepo = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore'
$root = Join-Path $windowsRepo ('.artifacts\clean-' + [guid]::NewGuid().ToString('N').Substring(0,12))
New-Item -ItemType Directory -Path $root -Force | Out-Null
foreach ($entry in @(@($windowsRepo,'WordSuggestorWindows','HEAD'), @($coreRepo,'WordSuggestorCore',$CoreRevision))) {
    if ($IncludeWorkingTree -and $entry[1] -eq 'WordSuggestorWindows') {
        $destination = Join-Path $root 'WordSuggestorWindows'
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Push-Location $windowsRepo
        try {
            $tracked = (& git -c core.quotepath=false ls-files -z --cached --others --exclude-standard) -join "`n"
            if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate clean-source snapshot.' }
            foreach ($relative in ($tracked.Split([char]0) | Where-Object { $_ })) {
                $source = [IO.Path]::GetFullPath((Join-Path $windowsRepo $relative))
                if (-not $source.StartsWith(($windowsRepo + '\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Snapshot path escapes repository.' }
                if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue } # tracked deletion
                $target = Join-Path $destination $relative
                New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
                Copy-Item -LiteralPath $source -Destination $target
            }
        } finally { Pop-Location }
        continue
    }
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
if ($IncludeWorkingTree) { Write-Output "PASS clean source snapshot of working tree (no commit/index change): $root" }
else { Write-Output "PASS clean committed exports: $root" }
