param([string]$OutputDirectory, [string]$TestPackageFamilyName, [switch]$PackagedUpdateProbe, [string]$RunnerPath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'settings_qualification.ps1')
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo ('.artifacts\settings-qualification\' + [guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
if ($PackagedUpdateProbe) {
    if (-not $TestPackageFamilyName) { throw 'Packaged update qualification requires an installed isolated LocalTest family.' }
    $package = Get-AppxPackage | Where-Object PackageFamilyName -CEQ $TestPackageFamilyName | Select-Object -First 1
    Assert-SettingsQualificationPackage $TestPackageFamilyName $package
    if (Test-SettingsProcessElevated) { throw 'Use an ordinary-user session.' }
    $before = Get-SettingsUpdateSnapshot $TestPackageFamilyName
    $null = Get-SettingsUpdateArguments $TestPackageFamilyName $before $true $true # rejects paused fixtures
    $fingerprint = ConvertTo-SettingsUpdateFingerprint $before
    $onLaunch = Get-SettingsUpdateFlag $before @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')
    $background = Get-SettingsUpdateFlag $before @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')
    . (Join-Path $PSScriptRoot 'dotnet_env.ps1')
    if (-not $RunnerPath) { $RunnerPath = Join-Path $repo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe' }
    $runId = [guid]::NewGuid().ToString('N')
    & $RunnerPath --activate-update-fixture ($TestPackageFamilyName+'!App') $runId
    if ($LASTEXITCODE -ne 0) { throw 'Isolated packaged activation failed.' }
    $nonce = $package.Name.Substring('WordSuggestor.Windows.LocalTest.'.Length)
    $resultPath = Join-Path $env:LOCALAPPDATA ('WordSuggestor\LocalTests\'+$nonce+'\diagnostics\update-policy-'+$runId+'.json')
    $limit = [DateTime]::UtcNow.AddSeconds(45)
    while (-not (Test-Path -LiteralPath $resultPath) -and [DateTime]::UtcNow -lt $limit) { Start-Sleep -Milliseconds 200 }
    if (-not (Test-Path -LiteralPath $resultPath)) { throw 'Packaged update qualification produced no result.' }
    $result = [IO.File]::ReadAllText($resultPath) | ConvertFrom-Json
    $after = Get-SettingsUpdateSnapshot $TestPackageFamilyName
    $restored = (ConvertTo-SettingsUpdateFingerprint $after) -ceq $fingerprint -and
        (Get-SettingsUpdateFlag $after @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')) -eq $onLaunch -and
        (Get-SettingsUpdateFlag $after @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -eq $background
    if (-not $restored -or $result.schema_version -ne 1 -or $result.run_id -cne $runId -or $result.package_name -cne $package.Name -or
        $result.packaged_app_context -isnot [bool] -or -not $result.packaged_app_context -or $result.restored -isnot [bool] -or -not $result.restored) { throw 'Native result/restoration validation failed.' }
    $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'update-native.json') -Encoding UTF8
    if($result.mutation_stage){Write-Output ('UPDATE NATIVE stage='+$result.mutation_stage+' code='+$result.mutation_code+' hresult='+$result.hresult)}
    if ($result.status -ceq 'passed' -and $result.changed -is [bool] -and $result.changed -and (Test-SettingsPolicyScheduleEvidence $result)) { Write-Output 'PASS UPDATE NATIVE packaged off/on/read/restore; ordinary user; fixture identity/source/pause preserved.'; exit 0 }
    if ($result.status -ceq 'blocked') { Write-Warning 'Native update mutation blocked; production G05 remains read-only.'; exit 3 }
    throw 'Native packaged update qualification failed.'
}
$report = Get-SettingsHostCapabilities
if ($TestPackageFamilyName) { $report.updater.package_roundtrip = Get-SettingsUpdateQualification $TestPackageFamilyName }
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'platform.json') -Encoding UTF8
Get-SettingsSourceSnapshot $repo | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'source.json') -Encoding UTF8
Write-Output ('Hello availability: ' + $report.hello.availability + '; key credential support: ' + $report.hello.key_credential_supported)
Write-Output ('App Installer API present: ' + $report.updater.app_installer_manager_api_present)
Write-Output ('Ordinary user: ' + (-not $report.elevated))
Write-Output ('Qualification results: ' + [IO.Path]::GetFullPath($OutputDirectory))
if ($report.hello.probe_error_category -or $report.hello.key_probe_error_category) { throw 'Platform capability probe failed; no positive qualification was recorded.' }
