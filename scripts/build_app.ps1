param([ValidateSet("Debug", "Release")][string]$Configuration = "Debug", [string]$ArtifactsPath)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "dotnet_env.ps1")
. (Join-Path $PSScriptRoot "app_process.ps1")
$outputExe = Join-Path $windowsRepo "src\WordSuggestorWindows.App\bin\$Configuration\net10.0-windows\WordSuggestorWindows.App.exe"
if ($ArtifactsPath) { $ArtifactsPath = Resolve-WordSuggestorDevPath $ArtifactsPath; $outputExe = Join-Path $ArtifactsPath "bin\WordSuggestorWindows.App\$($Configuration.ToLowerInvariant())\WordSuggestorWindows.App.exe" }
$running = @(Get-WordSuggestorAppProcess $outputExe)
if ($running.Count -gt 0) {
    throw "WordSuggestor is using this build output (PID $($running[0].Id)). Exit via Afslut WordSuggestor, use run_app.ps1 -Restart, or build separately with -ArtifactsPath .artifacts\editor-tabs."
}
$solutionPath = Join-Path $windowsRepo "WordSuggestorWindows.sln"
$arguments = @('build', $solutionPath, '--configuration', $Configuration)
if ($ArtifactsPath) { $arguments += @('--artifacts-path', $ArtifactsPath) }
dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Windows build failed ($LASTEXITCODE)." }
