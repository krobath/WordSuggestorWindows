param([switch]$SkipBuild, [string]$AppPath, [string]$RunnerPath, [switch]$FrameworkDependentApp, [switch]$EditorOnly)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $SkipBuild) {
    dotnet build (Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\WordSuggestorWindows.Smoke.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'GUI smoke build failed.' }
}
if ($AppPath) { $env:WORDSUGGESTOR_TEST_APP_PATH = [System.IO.Path]::GetFullPath($AppPath) }
if ($FrameworkDependentApp) { $env:WORDSUGGESTOR_TEST_KEEP_SDK = '1' }
$runner = Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe'
if ($RunnerPath) { $runner = [IO.Path]::GetFullPath($RunnerPath) }
$mode = if ($EditorOnly) { '--tabs-gui' } else { '--gui' }
& $runner $mode
if ($LASTEXITCODE -ne 0) { throw "GUI smoke failed ($LASTEXITCODE)." }
