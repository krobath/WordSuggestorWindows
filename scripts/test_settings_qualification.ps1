$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'settings_qualification.ps1')
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$registry = Get-Content -LiteralPath (Join-Path $repo 'docs\qualification\settings-controls.v1.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$script:passed = 0
function Check([string]$Name,[bool]$Result) { if (-not $Result) { throw "FAIL SETTINGS $Name" }; $script:passed++; Write-Output "PASS SETTINGS $Name" }
function Reject([string]$Name,[scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Check $Name $rejected
}
Check 'register schema/platform' ($registry.schema_version -eq 1 -and $registry.platform -ceq 'windows')
Check 'canonical macOS reference' ($registry.reference_commit -ceq 'ba18b451372dd2e3f125e557c5db45894f4e1ae3')
Check 'eight categories in approved order' (($registry.categories -join ',') -ceq 'profile,general,speech,suggestions,textAnalysis,learning,errorTracking,advanced')
Check '72 unique controls' ($registry.controls.Count -eq 72 -and @($registry.controls.id | Select-Object -Unique).Count -eq 72)
$analysis = [IO.File]::ReadAllText((Join-Path $repo 'docs\analysis\2026-11-09_settings_parity_analysis.md'))
$referenceIds = @([regex]::Matches($analysis,'(?m)^\| ([PGOFTLEA]\d\d) \|') | ForEach-Object { $_.Groups[1].Value })
Check 'register covers every analyzed control exactly' (-not (Compare-Object ($registry.controls.id | Sort-Object) ($referenceIds | Sort-Object)))
Check 'controls have category, title and primary slice' (@($registry.controls | Where-Object { $_.category -notin $registry.categories -or -not $_.title_da -or $_.primary_slice -lt 0 -or $_.primary_slice -gt 9 }).Count -eq 0)
Check 'no untested control marked qualified' (@($registry.controls | Where-Object runtime_qualification -CNE 'not_run_in_sprint').Count -eq 0)
Check 'Hello explicitly permits PIN and biometrics' ($registry.approved_windows_policy.hello_pin_allowed -eq $true -and $registry.approved_windows_policy.hello_biometrics_allowed -eq $true)
Check 'approved independent shortcut defaults' (($registry.approved_windows_policy.lane_defaults -join ',') -ceq 'ctrl_digits,ctrl_shift_digits,alt_digits')
Check 'legacy remains Windows-only' ($registry.approved_windows_policy.legacy -ceq 'existing_windows_settings')

# Current implementation status is separate from the immutable slice-0 baseline.
$current = Get-Content -LiteralPath (Join-Path $repo 'docs\qualification\settings-parity-status.v1.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check 'current matrix schema platform and canonical reference' ($current.schema_version -eq 1 -and $current.platform -ceq 'windows' -and $current.reference_commit -ceq $registry.reference_commit)
Check 'current matrix covers all 72 controls once' ($current.controls.Count -eq 72 -and @($current.controls.id | Select-Object -Unique).Count -eq 72 -and -not (Compare-Object ($current.controls.id | Sort-Object) ($registry.controls.id | Sort-Object)))
Check 'implementation states never imply human acceptance' (@($current.controls | Where-Object { $_.implementation -notin @('implemented','partial','unavailable') -or $_.manual_qualification -notin @('not_run','blocked','unavailable','deferred_by_user') -or -not $_.automation_suite }).Count -eq 0 -and $current.sprint_complete -eq $false -and $current.release_qualified -eq $false)
$pins = Get-Content -LiteralPath (Join-Path $repo 'dependencies.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check 'status matrix matches pinned Core revision' ($current.core_revision -ceq $pins.coreRevision)
Check 'unknown-word updater and Hello gaps explicit' ((@($current.controls | Where-Object implementation -eq 'unavailable').id -join ',') -ceq 'A06,A07' -and ($current.controls | Where-Object id -eq 'G05').implementation -ceq 'partial' -and ($current.controls | Where-Object id -eq 'P07').manual_qualification -ceq 'deferred_by_user')
Check 'each current control has a local evidence document' (@($current.controls | Where-Object { $_.evidence_document -cnotmatch '^WSA-UX-029_slice[0-9]\.md$' -or -not (Test-Path -LiteralPath (Join-Path $repo ('docs\qualification\'+$_.evidence_document))) }).Count -eq 0)

$family = 'WordSuggestor.Windows.LocalTest.0123456789ab_0123456789abc'
$package = [pscustomobject]@{Name='WordSuggestor.Windows.LocalTest.0123456789ab'; Publisher='CN=WordSuggestor Local Test 0123456789ab'; PackageFamilyName=$family}
Assert-SettingsQualificationPackage $family $package
Check 'isolated test identity accepted' $true
Reject 'production identity cannot be mutated' { Assert-SettingsQualificationPackage 'WordSuggestor.Windows.Beta_0123456789abc' $package }
Reject 'case/identity confusion rejected' { Assert-SettingsQualificationPackage $family ([pscustomobject]@{Name=$package.Name; Publisher='CN=WordSuggestor'; PackageFamilyName=$family}) }
Reject 'wrong family rejected' { Assert-SettingsQualificationPackage $family ([pscustomobject]@{Name=$package.Name; Publisher=$package.Publisher; PackageFamilyName='other'}) }
Reject 'missing package rejected' { Assert-SettingsQualificationPackage $family $null }
Reject 'unknown OS flag shape rejected' { Get-SettingsUpdateFlag ([pscustomobject]@{SomethingNew=$true}) @('CheckOnLaunch','OnLaunch') }
Reject 'string flags cannot attest boolean state' { Get-SettingsUpdateFlag ([pscustomobject]@{OnLaunch='false'}) @('OnLaunch') }
$nativeShape=[pscustomobject]@{AutomaticBackgroundTaskUpdatesEnabled=$true; CheckForUpdatesOnLaunch=$false}
Check 'actual OS property names supported' ((Get-SettingsUpdateFlag $nativeShape @('AutomaticBackgroundTaskUpdatesEnabled')) -and -not (Get-SettingsUpdateFlag $nativeShape @('CheckForUpdatesOnLaunch')))
Reject 'paused package not modified' { Get-SettingsUpdateArguments $family ([pscustomobject]@{PausedUntil=[DateTimeOffset]::UtcNow}) $true $true }

# Synthetic OS boundary for mutation/failure paths; never touches a real package.
$script:state = [pscustomobject]@{EnableAutomaticBackgroundTask=$true; CheckOnLaunch=$true; AppInstallerUri='https://fixture.invalid/feed.appinstaller'; Version='0.2.0.10'; HoursBetweenUpdateChecks=6}
$script:setCount=0; $script:failOnce=$false
function Get-AppxPackage { $package }
function Test-SettingsProcessElevated { $false }
function Get-AppxPackageAutoUpdateSettings { param($PackageFamilyName,$ErrorAction) $script:state.PSObject.Copy() }
function Set-AppxPackageAutoUpdateSettings {
    param($PackageFamilyName,[bool]$EnableAutomaticBackgroundTask,[bool]$CheckOnLaunch,$ErrorAction)
    $script:setCount++
    $script:state.EnableAutomaticBackgroundTask=$EnableAutomaticBackgroundTask
    $script:state.CheckOnLaunch=$CheckOnLaunch
    if ($script:failOnce) { $script:failOnce=$false; throw 'Synthetic partial OS mutation failure.' }
}
$result=Invoke-SettingsUpdateRoundtrip $family
Check 'toggle and re-read actual boundary response' ($result.status -ceq 'passed' -and $result.ordinary_user -eq $true)
Check 'OS settings restored after success' ($result.restored -eq $true -and $script:state.CheckOnLaunch -and $script:state.EnableAutomaticBackgroundTask -and $script:setCount -eq 2)
$script:failOnce=$true
Reject 'partial OS failure propagates' { Invoke-SettingsUpdateRoundtrip $family }
Check 'OS settings restored after partial failure' ($script:state.CheckOnLaunch -and $script:state.EnableAutomaticBackgroundTask -and $script:setCount -eq 4)
function Test-SettingsProcessElevated { $true }
Reject 'elevated run cannot attest ordinary-user access' { Invoke-SettingsUpdateRoundtrip $family }
Check 'admin rejection has no mutation' ($script:setCount -eq 4)
function Test-SettingsProcessElevated { $false }
function Set-AppxPackageAutoUpdateSettings { throw [Runtime.InteropServices.COMException]::new('Synthetic unsupported OS boundary.',-2147023113) }
function Set-SettingsNativeUpdateFlags { throw [Runtime.InteropServices.COMException]::new('Synthetic unsupported OS boundary.',-2147023113) }
$blocked=Get-SettingsUpdateQualification $family
Check 'unsupported OS paths recorded as blocked' ($blocked.cmdlet.status -ceq 'blocked' -and $blocked.native.status -ceq 'blocked')
Check 'rejected OS mutation verifies unchanged settings' ($blocked.cmdlet.unchanged -and $blocked.native.unchanged -and $script:state.CheckOnLaunch -and $script:state.EnableAutomaticBackgroundTask)
Check 'failure records sanitized HRESULT only' ($blocked.cmdlet.hresult -ceq '0x800706F7' -and -not ($blocked.cmdlet.Contains('message')))
$fixture=Join-Path $repo ('.artifacts\tests\settings-gate-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
$scriptPath=Join-Path $fixture 'fixture.ps1'; $log=Join-Path $fixture 'fixture.log'
[IO.File]::WriteAllText($scriptPath,"[Console]::Error.WriteLine('synthetic warning'); Write-Output 'synthetic stdout'; exit 0")
$exit=Invoke-SettingsGateProcess $scriptPath @() $log
Check 'stderr does not falsely fail a successful child' ($exit -eq 0 -and [IO.File]::ReadAllText($log).Contains('synthetic warning') -and [IO.File]::ReadAllText($log).Contains('synthetic stdout'))
Check 'gate preserves caller error policy' ($ErrorActionPreference -ceq 'Stop')
[IO.File]::WriteAllText($scriptPath,'exit 9')
Check 'nonzero child result never becomes success' ((Invoke-SettingsGateProcess $scriptPath @() $log) -eq 9)
foreach ($file in @('settings_qualification.ps1','test_settings_platform.ps1','test_settings_qualification.ps1','test_settings_slice0.ps1')) {
    $tokens=$null; $parseErrors=$null
    $null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $file),[ref]$tokens,[ref]$parseErrors)
    Check "$file syntax" ($parseErrors.Count -eq 0)
}
Write-Output "PASS SETTINGS $script:passed contract/safety checks. Mock tests do not attest native OS or Hello access."
