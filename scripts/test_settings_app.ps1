param(
    [ValidateSet('State', 'Ui', 'Core', 'All')][string]$Mode = 'All',
    [switch]$SkipBuild,
    [string]$RunnerPath
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $SkipBuild) {
    dotnet build (Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\WordSuggestorWindows.Smoke.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'Settings app smoke build failed.' }
}
$runner = Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe'
if ($RunnerPath) { $runner = [IO.Path]::GetFullPath($RunnerPath) }
if ($Mode -in @('Core', 'All')) {
    & $runner --core-settings
    if ($LASTEXITCODE -ne 0) { throw "Settings Core adapter smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('State', 'All')) {
    & $runner --settings-state
    if ($LASTEXITCODE -ne 0) { throw "Settings state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('Ui', 'All')) {
    & $runner --settings-ui
    if ($LASTEXITCODE -ne 0) { throw "Settings desktop smoke failed ($LASTEXITCODE). An unlocked interactive Windows desktop is required." }
}
