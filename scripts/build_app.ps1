$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appDataRoot = Join-Path $repoRoot ".appdata"
$nuGetDir = Join-Path $appDataRoot "NuGet"
$dotnetHome = Join-Path $repoRoot ".dotnet"
$solutionPath = Join-Path $repoRoot "WordSuggestorWindows.sln"
$nugetConfigPath = Join-Path $repoRoot "NuGet.Config"
$localNuGetConfigPath = Join-Path $nuGetDir "NuGet.Config"

New-Item -ItemType Directory -Force $nuGetDir | Out-Null
Copy-Item $nugetConfigPath $localNuGetConfigPath -Force

$env:APPDATA = $appDataRoot
$env:DOTNET_CLI_HOME = $dotnetHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

dotnet build $solutionPath

