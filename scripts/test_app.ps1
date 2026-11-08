param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $SkipBuild) {
    dotnet build (Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\WordSuggestorWindows.Smoke.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'Smoke test build failed.' }
}
$runner = Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe'
& $runner
if ($LASTEXITCODE -ne 0) { throw "Application smoke failed ($LASTEXITCODE)." }
