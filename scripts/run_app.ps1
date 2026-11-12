param(
    [string]$SampleText = "Jeg vil gerne skri",
    [switch]$SkipBootstrap,
    [switch]$SkipBuild,
    [string]$AppPath,
    [switch]$Restart
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "dotnet_env.ps1")
. (Join-Path $PSScriptRoot "app_process.ps1")

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$bootstrapScript = Join-Path $repoRoot "scripts\bootstrap_core_cli.ps1"
$buildScript = Join-Path $repoRoot "scripts\build_app.ps1"
$appExe = Join-Path $repoRoot "src\WordSuggestorWindows.App\bin\Debug\net10.0-windows\WordSuggestorWindows.App.exe"
$buildExe = $appExe
if ($AppPath) { $appExe = Resolve-WordSuggestorDevPath $AppPath }

# Check before bootstrap/build rather than hitting MSBuild's file-copy retries.
$paths = @($appExe)
if (-not $SkipBuild) { $paths += $buildExe }
foreach ($path in ($paths | Select-Object -Unique)) {
    foreach ($running in @(Get-WordSuggestorAppProcess $path)) {
        if ($Restart) { Close-WordSuggestorAppGracefully $running $path }
        elseif (-not $SkipBuild) {
            throw "WordSuggestor is still running (PID $($running.Id)). Use Afslut WordSuggestor, or rerun this command with -Restart to request a normal shutdown with save prompts. No build was started."
        }
    }
}

if (-not $SkipBootstrap) {
    & $bootstrapScript
}

if (-not $SkipBuild) {
    & $buildScript
}

if (-not (Test-Path $appExe)) {
    throw "App executable not found at '$appExe'."
}

Write-Host "Launching WordSuggestorWindows.App."
$previousStartupText = [Environment]::GetEnvironmentVariable('WORDSUGGESTOR_WINDOWS_STARTUP_TEXT','Process')
try {
    $env:WORDSUGGESTOR_WINDOWS_STARTUP_TEXT = $SampleText
    Start-Process -FilePath $appExe -WindowStyle Hidden
} finally { [Environment]::SetEnvironmentVariable('WORDSUGGESTOR_WINDOWS_STARTUP_TEXT',$previousStartupText,'Process') }
