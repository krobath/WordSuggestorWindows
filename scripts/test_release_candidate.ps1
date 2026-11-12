param([Parameter(Mandatory=$true)][string]$BundleDirectory)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release_common.ps1')
$candidate = Test-ReleaseBundle $BundleDirectory -VerifySignature
$windowsRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scratch = Join-Path $windowsRepo ('.artifacts\tests\candidate-' + [guid]::NewGuid().ToString('N'))
$makeAppx = Get-ReleaseSdkTool 'makeappx'
$null = & $makeAppx unpack /p (Join-Path $candidate.Directory $candidate.Manifest.release.artifact.filename) /d $scratch /o 2>&1
if ($LASTEXITCODE -ne 0) { throw 'Candidate payload extraction failed.' }
& (Join-Path $PSScriptRoot 'test_package.ps1') -AppDirectory $scratch
Write-Output 'PASS actual release candidate payload desktop gate.'
