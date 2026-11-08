param([switch]$SkipBuild, [string]$AppPath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $SkipBuild) {
    dotnet build (Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\WordSuggestorWindows.Smoke.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'GUI smoke build failed.' }
}
if ($AppPath) { $env:WORDSUGGESTOR_TEST_APP_PATH = [System.IO.Path]::GetFullPath($AppPath) }
& (Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe') --gui
if ($LASTEXITCODE -ne 0) { throw "GUI smoke failed ($LASTEXITCODE)." }
