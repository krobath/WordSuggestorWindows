param([Parameter(Mandatory=$true)][string]$AppDirectory, [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
$source = [System.IO.Path]::GetFullPath($AppDirectory)
$info = Get-Content -LiteralPath (Join-Path $source 'build-info.json') -Raw | ConvertFrom-Json
if ($info.frameworkDependent) { throw 'Standalone package validation requires the self-contained build.' }
if ((Get-FileHash -LiteralPath (Join-Path $source 'Core\WordSuggestorSuggestCLI.exe') -Algorithm SHA256).Hash -ne $info.coreSha256) { throw 'Core artifact digest mismatch.' }
if ((Get-FileHash -LiteralPath (Join-Path $source 'Packs\da_lexicon.sqlite') -Algorithm SHA256).Hash -ne $info.danishPackSha256) { throw 'Danish pack digest mismatch.' }
$isolated = Join-Path ([System.IO.Path]::GetTempPath()) ('wordsuggestor-package-' + [guid]::NewGuid().ToString('N'))
Copy-Item -LiteralPath $source -Destination $isolated -Recurse
Write-Output 'Testing outside source workspace with Swift/.NET SDK paths removed from the app environment.'
& (Join-Path $PSScriptRoot 'test_gui.ps1') -SkipBuild:$SkipBuild -AppPath (Join-Path $isolated 'WordSuggestorWindows.App.exe')
Write-Output "Isolated package location: $isolated"
