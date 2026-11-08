param([ValidateSet("Debug", "Release")][string]$Configuration = "Debug")
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "dotnet_env.ps1")
$solutionPath = Join-Path $windowsRepo "WordSuggestorWindows.sln"
dotnet build $solutionPath --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "Windows build failed ($LASTEXITCODE)." }
