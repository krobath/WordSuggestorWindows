param([string]$OutputDirectory, [string]$TestPackageFamilyName)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'settings_qualification.ps1')
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo ('.artifacts\settings-qualification\' + [guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$report = Get-SettingsHostCapabilities
if ($TestPackageFamilyName) { $report.updater.package_roundtrip = Get-SettingsUpdateQualification $TestPackageFamilyName }
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'platform.json') -Encoding UTF8
Get-SettingsSourceSnapshot $repo | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'source.json') -Encoding UTF8
Write-Output ('Hello availability: ' + $report.hello.availability + '; key credential support: ' + $report.hello.key_credential_supported)
Write-Output ('App Installer API present: ' + $report.updater.app_installer_manager_api_present)
Write-Output ('Ordinary user: ' + (-not $report.elevated))
Write-Output ('Qualification results: ' + [IO.Path]::GetFullPath($OutputDirectory))
if ($report.hello.probe_error_category -or $report.hello.key_probe_error_category) { throw 'Platform capability probe failed; no positive qualification was recorded.' }
