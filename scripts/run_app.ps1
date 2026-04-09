param(
    [string]$SampleText = "Jeg vil gerne skri",
    [switch]$SkipBootstrap,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$bootstrapScript = Join-Path $repoRoot "scripts\bootstrap_core_cli.ps1"
$buildScript = Join-Path $repoRoot "scripts\build_app.ps1"
$appExe = Join-Path $repoRoot "src\WordSuggestorWindows.App\bin\Debug\net9.0-windows\WordSuggestorWindows.App.exe"

if (-not $SkipBootstrap) {
    & $bootstrapScript
}

if (-not $SkipBuild) {
    & $buildScript
}

if (-not (Test-Path $appExe)) {
    throw "App executable not found at '$appExe'."
}

$env:WORDSUGGESTOR_WINDOWS_STARTUP_TEXT = $SampleText

Write-Host "Launching WordSuggestorWindows.App with startup text:"
Write-Host $SampleText

Start-Process -FilePath $appExe
