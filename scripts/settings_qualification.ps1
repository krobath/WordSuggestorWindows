# Slice-0 probes. These functions never enable product features or open an
# enrollment dialog. Mutating update tests are restricted to isolated test MSIXes.
$ErrorActionPreference = 'Stop'

function Test-SettingsProcessElevated {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-SettingsGateProcess([string]$ScriptFile,[string[]]$Arguments,[string]$LogPath) {
    # Windows PowerShell turns redirected native stderr into ErrorRecords.
    # Capture it as evidence, then use the child exit status as the gate result.
    $previousPreference=$ErrorActionPreference
    try {
        $ErrorActionPreference='Continue'
        & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass `
            -File $ScriptFile @Arguments 2>&1 | Tee-Object -FilePath $LogPath | Write-Host
        $code=$LASTEXITCODE
    } finally { $ErrorActionPreference=$previousPreference }
    if ($null -eq $code) { throw 'Test process did not produce an exit code.' }
    return [int]$code
}

function Wait-SettingsWinRt($Operation, [type]$ResultType, [int]$TimeoutSeconds = 10) {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $method = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
        $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetGenericArguments().Count -eq 1 -and
        $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
    } | Select-Object -First 1
    $task = $method.MakeGenericMethod($ResultType).Invoke($null,@($Operation))
    if (-not $task.Wait($TimeoutSeconds * 1000)) {
        try { $Operation.Cancel() } catch { }
        throw [TimeoutException]::new('Platform capability probe timed out.')
    }
    return $task.GetAwaiter().GetResult()
}

function Get-SettingsHostCapabilities {
    $isAdmin = Test-SettingsProcessElevated
    $hello = 'ProbeFailed'; $helloError = $null
    try {
        $null = [Windows.Security.Credentials.UI.UserConsentVerifier,Windows.Security.Credentials.UI,ContentType=WindowsRuntime]
        $hello = (Wait-SettingsWinRt ([Windows.Security.Credentials.UI.UserConsentVerifier]::CheckAvailabilityAsync()) `
            ([Windows.Security.Credentials.UI.UserConsentVerifierAvailability])).ToString()
    } catch { $helloError = $_.Exception.GetType().Name }
    $keySupported = $false; $keyProbeError = $null
    try {
        $null = [Windows.Security.Credentials.KeyCredentialManager,Windows.Security.Credentials,ContentType=WindowsRuntime]
        $keySupported = [bool](Wait-SettingsWinRt ([Windows.Security.Credentials.KeyCredentialManager]::IsSupportedAsync()) ([bool]))
    } catch { $keyProbeError = $_.Exception.GetType().Name }
    $managerAvailable = $false
    try {
        $null = [Windows.Foundation.Metadata.ApiInformation,Windows.Foundation,ContentType=WindowsRuntime]
        $managerAvailable = [Windows.Foundation.Metadata.ApiInformation]::IsTypePresent('Windows.Management.Deployment.AppInstallerManager')
    } catch { }
    return [pscustomobject][ordered]@{
        schema_version = 1
        os_version = [Environment]::OSVersion.Version.ToString()
        process_bits = [IntPtr]::Size * 8
        elevated = $isAdmin
        probe_context = 'unpackaged_windows_powershell'
        hello = [ordered]@{
            availability = $hello; probe_error_category = $helloError
            key_credential_supported = $keySupported; key_probe_error_category = $keyProbeError
            protected_secret_roundtrip = 'not_run'; user_verification_roundtrip = 'not_run'
            product_enabled = $false
        }
        updater = [ordered]@{
            app_installer_manager_api_present = $managerAvailable
            get_cmdlet_present = [bool](Get-Command Get-AppxPackageAutoUpdateSettings -ErrorAction SilentlyContinue)
            set_cmdlet_present = [bool](Get-Command Set-AppxPackageAutoUpdateSettings -ErrorAction SilentlyContinue)
            package_roundtrip = 'not_run'; packaged_app_authority = 'not_qualified'
            product_toggle_enabled = $false
        }
    }
}

function Assert-SettingsQualificationPackage([string]$FamilyName, $Package) {
    if ($FamilyName -cnotmatch '^WordSuggestor\.Windows\.LocalTest\.([a-f0-9]{8,32})_[a-z0-9]{13}$') {
        throw 'Only an isolated WordSuggestor LocalTest package can be changed.'
    }
    $nonce = $Matches[1]
    if (-not $Package -or $Package.Name -cne ('WordSuggestor.Windows.LocalTest.' + $nonce) -or
        $Package.PackageFamilyName -cne $FamilyName -or $Package.Publisher -cne ('CN=WordSuggestor Local Test ' + $nonce)) {
        throw 'Installed test package identity/publisher mismatch.'
    }
}

function Test-SettingsPolicyScheduleEvidence($Result) {
    $steps=@($Result.steps)
    if($steps.Count -ne 2){return $false}
    foreach($step in $steps){
        if($step.enabled -isnot [bool] -or $step.verified -isnot [bool] -or -not $step.verified -or $step.code -cne 'saved' -or $step.stage -cne 'verify_after' -or $step.hresult){return $false}
    }
    return $steps[0].enabled -ne $steps[1].enabled
}

function Get-SettingsUpdateSnapshot([string]$FamilyName) {
    $value = Get-AppxPackageAutoUpdateSettings -PackageFamilyName $FamilyName -ErrorAction Stop
    if (-not $value) { throw 'The test package has no App Installer association.' }
    return $value
}

function ConvertTo-SettingsUpdateFingerprint($Snapshot) {
    # Compare all OS-returned properties except the two deliberately exercised
    # booleans. No URI/identity values are written to diagnostic output.
    $properties = [ordered]@{}
    foreach ($property in ($Snapshot.PSObject.Properties | Sort-Object Name)) {
        if ($property.Name -notin @('EnableAutomaticBackgroundTask','CheckOnLaunch','AutomaticBackgroundTask','OnLaunch','AutomaticBackgroundTaskUpdatesEnabled','CheckForUpdatesOnLaunch','LastCheckedForUpdates')) {
            $properties[$property.Name] = $property.Value
        }
    }
    return ($properties | ConvertTo-Json -Depth 12 -Compress)
}

function Get-SettingsUpdateFlag($Snapshot, [string[]]$Names) {
    foreach ($name in $Names) {
        if ($null -ne $Snapshot.PSObject.Properties[$name]) {
            if ($Snapshot.$name -isnot [bool]) { throw 'OS update flag must be a boolean.' }
            return $Snapshot.$name
        }
    }
    throw 'Unknown OS auto-update settings shape; no change is allowed.'
}

function Get-SettingsUpdateArguments([string]$FamilyName, $Snapshot, [bool]$Background, [bool]$OnLaunch) {
    if ($Snapshot.PSObject.Properties['PausedUntil'] -and $null -ne $Snapshot.PausedUntil) {
        throw 'A paused test package cannot be exercised without changing its pause policy.'
    }
    $arguments=@{PackageFamilyName=$FamilyName; EnableAutomaticBackgroundTask=$Background; CheckOnLaunch=$OnLaunch}
    $mapping=@{
        AppInstallerUri='AppInstallerUri'; Version='Version'; UpdateUris='UpdateUris'; RepairUris='RepairUris'
        OptionalPackageUris='OptionalPackages'; DependencyPackageUris='DependencyPackages'
        ShowPromptOnLaunchWhenUpdateIsAvailable='ShowPrompt'; UpdateBlocksActivation='UpdateBlocksActivation'
        ForceUpdateFromAnyVersion='ForceUpdateFromAnyVersion'; HoursBetweenUpdateChecks='HoursBetweenUpdateChecks'
    }
    foreach ($name in $mapping.Keys) {
        if ($Snapshot.PSObject.Properties[$name] -and $null -ne $Snapshot.$name) {
            if ($Snapshot.$name -is [Array] -and $Snapshot.$name.Count -eq 0) { continue }
            $arguments[$mapping[$name]]=$Snapshot.$name
        }
    }
    if ($Snapshot.PSObject.Properties['IsAutoRepairEnabled']) { $arguments.DisableAutoRepairs=-not $Snapshot.IsAutoRepairEnabled }
    return $arguments
}

function Set-SettingsNativeUpdateFlags([string]$FamilyName, $Snapshot, [bool]$Background, [bool]$OnLaunch) {
    $null=[Windows.Management.Deployment.AppInstallerManager,Windows.Management.Deployment,ContentType=WindowsRuntime]
    $null=[Windows.Management.Deployment.AutoUpdateSettingsOptions,Windows.Management.Deployment,ContentType=WindowsRuntime]
    $options=[Windows.Management.Deployment.AutoUpdateSettingsOptions]::new()
    $options.AppInstallerUri=[uri]$Snapshot.AppInstallerUri
    $options.AutomaticBackgroundTask=$Background; $options.OnLaunch=$OnLaunch
    $options.ShowPrompt=$Snapshot.ShowPromptOnLaunchWhenUpdateIsAvailable
    $options.UpdateBlocksActivation=$Snapshot.UpdateBlocksActivation
    $options.ForceUpdateFromAnyVersion=$Snapshot.ForceUpdateFromAnyVersion
    $options.IsAutoRepairEnabled=$Snapshot.IsAutoRepairEnabled
    $options.HoursBetweenUpdateChecks=$Snapshot.HoursBetweenUpdateChecks
    $version=[version]$Snapshot.Version; $nativeVersion=$options.Version
    $nativeVersion.Major=$version.Major; $nativeVersion.Minor=$version.Minor
    $nativeVersion.Build=$version.Build; $nativeVersion.Revision=$version.Revision
    $options.Version=$nativeVersion
    foreach ($name in @('UpdateUris','RepairUris','OptionalPackageUris','DependencyPackageUris')) {
        foreach ($uri in $Snapshot.$name) { $options.$name.Add([uri]$uri) }
    }
    [Windows.Management.Deployment.AppInstallerManager]::GetDefault().SetAutoUpdateSettings($FamilyName,$options)
}

function Invoke-SettingsUpdateRoundtrip([string]$FamilyName, [ValidateSet('cmdlet','native')][string]$Method='cmdlet') {
    $package = Get-AppxPackage | Where-Object PackageFamilyName -CEQ $FamilyName | Select-Object -First 1
    Assert-SettingsQualificationPackage $FamilyName $package
    if (Test-SettingsProcessElevated) {
        throw 'Run the OS preference test as an ordinary user.'
    }
    $before = Get-SettingsUpdateSnapshot $FamilyName
    $background = Get-SettingsUpdateFlag $before @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')
    $onLaunch = Get-SettingsUpdateFlag $before @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')
    $fingerprint = ConvertTo-SettingsUpdateFingerprint $before
    $restoreArguments = Get-SettingsUpdateArguments $FamilyName $before $background $onLaunch
    $changeArguments = Get-SettingsUpdateArguments $FamilyName $before (-not $background) (-not $onLaunch)
    $changed = $false; $restored = $false
    try {
        # The finally restoration also runs after a partially failing Set call.
        $changed = $true
        if ($Method -eq 'native') { Set-SettingsNativeUpdateFlags $FamilyName $before (-not $background) (-not $onLaunch) }
        else { Set-AppxPackageAutoUpdateSettings @changeArguments -ErrorAction Stop | Out-Null }
        $after = Get-SettingsUpdateSnapshot $FamilyName
        if ((Get-SettingsUpdateFlag $after @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -eq $background -or
            (Get-SettingsUpdateFlag $after @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')) -eq $onLaunch -or
            (ConvertTo-SettingsUpdateFingerprint $after) -cne $fingerprint) {
            throw 'OS update settings did not change exactly as requested.'
        }
    } finally {
        if ($changed) {
            # A rejected Set often leaves the fixture unchanged. Avoid masking
            # the original error with an unnecessary failing restoration call.
            $now=Get-SettingsUpdateSnapshot $FamilyName
            $needsRestore=(Get-SettingsUpdateFlag $now @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -ne $background -or
                (Get-SettingsUpdateFlag $now @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')) -ne $onLaunch -or
                (ConvertTo-SettingsUpdateFingerprint $now) -cne $fingerprint
            if ($needsRestore) {
                if ($Method -eq 'native') { Set-SettingsNativeUpdateFlags $FamilyName $before $background $onLaunch }
                else { Set-AppxPackageAutoUpdateSettings @restoreArguments -ErrorAction Stop | Out-Null }
            }
            $final = Get-SettingsUpdateSnapshot $FamilyName
            $restored = (Get-SettingsUpdateFlag $final @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -eq $background -and
                (Get-SettingsUpdateFlag $final @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')) -eq $onLaunch -and
                (ConvertTo-SettingsUpdateFingerprint $final) -ceq $fingerprint
            if (-not $restored) { throw 'Test package OS settings restoration failed; inspect the isolated fixture.' }
        }
    }
    return [pscustomobject]@{ status='passed'; restored=$restored; ordinary_user=$true; source_identity_and_other_settings_preserved=$true }
}

function Get-SettingsUpdateQualification([string]$FamilyName) {
    $package=Get-AppxPackage | Where-Object PackageFamilyName -CEQ $FamilyName | Select-Object -First 1
    Assert-SettingsQualificationPackage $FamilyName $package
    $results=[ordered]@{}
    foreach ($method in @('cmdlet','native')) {
        $before=Get-SettingsUpdateSnapshot $FamilyName
        try { $results[$method]=Invoke-SettingsUpdateRoundtrip $FamilyName $method }
        catch {
            $failure=$_.Exception
            while ($failure.InnerException) { $failure=$failure.InnerException }
            $after=Get-SettingsUpdateSnapshot $FamilyName
            $unchanged=(ConvertTo-SettingsUpdateFingerprint $after) -ceq (ConvertTo-SettingsUpdateFingerprint $before) -and
                (Get-SettingsUpdateFlag $after @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -eq
                    (Get-SettingsUpdateFlag $before @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -and
                (Get-SettingsUpdateFlag $after @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')) -eq
                    (Get-SettingsUpdateFlag $before @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch'))
            if (-not $unchanged) { throw 'Isolated OS settings were not restored; qualification failed.' }
            $results[$method]=[ordered]@{status='blocked'; error_category=$failure.GetType().Name; hresult=('0x{0:X8}' -f ($failure.HResult -band 0xffffffffL)); unchanged=$true}
        }
    }
    return $results
}

function Get-SettingsZeroIntervalControl([string]$FamilyName) {
    $package=Get-AppxPackage | Where-Object PackageFamilyName -CEQ $FamilyName | Select-Object -First 1
    Assert-SettingsQualificationPackage $FamilyName $package
    if(Test-SettingsProcessElevated){throw 'Zero-interval control requires an ordinary-user LocalTest.'}
    $before=Get-SettingsUpdateSnapshot $FamilyName
    if($before.HoursBetweenUpdateChecks -lt 1){throw 'Positive original interval required.'}
    $originalFingerprint=ConvertTo-SettingsUpdateFingerprint $before
    $background=Get-SettingsUpdateFlag $before @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')
    $launch=Get-SettingsUpdateFlag $before @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')
    $restore=Get-SettingsUpdateArguments $FamilyName $before $background $launch
    $zero=Get-SettingsUpdateArguments $FamilyName $before $background $launch
    $zero.HoursBetweenUpdateChecks=0
    $matchesOriginal={param($snapshot)
        (ConvertTo-SettingsUpdateFingerprint $snapshot) -ceq $originalFingerprint -and
            (Get-SettingsUpdateFlag $snapshot @('AutomaticBackgroundTaskUpdatesEnabled','EnableAutomaticBackgroundTask','AutomaticBackgroundTask')) -eq $background -and
            (Get-SettingsUpdateFlag $snapshot @('CheckForUpdatesOnLaunch','CheckOnLaunch','OnLaunch')) -eq $launch
    }
    $accepted=$false;$errorCode=$null
    try{Set-AppxPackageAutoUpdateSettings @zero -ErrorAction Stop | Out-Null;$accepted=$true}
    catch{$failure=$_.Exception;while($failure.InnerException){$failure=$failure.InnerException};$errorCode=('0x{0:X8}' -f ($failure.HResult -band 0xffffffffL))}
    finally{
        $now=Get-SettingsUpdateSnapshot $FamilyName
        if(-not (& $matchesOriginal $now)){
            Set-AppxPackageAutoUpdateSettings @restore -ErrorAction Stop | Out-Null
        }
        $after=Get-SettingsUpdateSnapshot $FamilyName
        if(-not (& $matchesOriginal $after)){throw 'Zero-interval control did not restore policy.'}
    }
    return [ordered]@{requested_hours=0;original_hours=$before.HoursBetweenUpdateChecks;accepted=$accepted;hresult=$errorCode;restored=$true}
}

function Get-SettingsUiFailure([ValidateSet('window','read_nodes','read_node')][string]$Stage,[Exception]$Failure) {
    while($Failure.InnerException){$Failure=$Failure.InnerException}
    return [ordered]@{error_stage=$Stage;error_category=$Failure.GetType().Name;hresult=('0x{0:X8}' -f ($Failure.HResult -band 0xffffffffL))}
}

function Test-SettingsUiWindowOwner([int]$OwnerProcessId,[IntPtr]$Handle,[int[]]$SettingsProcessIds,[int[]]$FrameProcessIds,[bool]$HasSettingsChild) {
    # IntPtr.Zero converts to $true in Windows PowerShell; never use -not $Handle.
    if($Handle -eq [IntPtr]::Zero){return $false}
    return $OwnerProcessId -in $SettingsProcessIds -or ($OwnerProcessId -in $FrameProcessIds -and $HasSettingsChild)
}

function Get-SettingsUiWindows {
    $settingsIds=@(Get-Process -Name SystemSettings -ErrorAction SilentlyContinue | Where-Object {$_.Path -ieq (Join-Path $env:SystemRoot 'ImmersiveControlPanel\SystemSettings.exe')} | Select-Object -ExpandProperty Id)
    if(-not $settingsIds.Count){return}
    $frameIds=@(Get-Process -Name ApplicationFrameHost -ErrorAction SilentlyContinue | Where-Object {$_.Path -ieq (Join-Path $env:SystemRoot 'System32\ApplicationFrameHost.exe')} | Select-Object -ExpandProperty Id)
    $conditions=@(foreach($ownerId in @($settingsIds+$frameIds)){
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,[int]$ownerId)
    })
    $filter=if($conditions.Count -eq 1){$conditions[0]}else{[System.Windows.Automation.OrCondition]::new([System.Windows.Automation.Condition[]]$conditions)}
    $windows=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$filter)
    foreach($window in $windows){
        try{
            $owner=$window.Current.ProcessId;$handle=[IntPtr]$window.Current.NativeWindowHandle
            if($handle -eq [IntPtr]::Zero){continue}
            $hasSettingsChild=$false
            if($owner -in $frameIds){
                foreach($settingsId in $settingsIds){
                    $condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,[int]$settingsId)
                    if($null -ne $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)){$hasSettingsChild=$true;break}
                }
            }
            if(Test-SettingsUiWindowOwner $owner $handle $settingsIds $frameIds $hasSettingsChild){
                [pscustomobject]@{Element=$window;Route=$(if($owner -in $settingsIds){'direct'}else{'hosted'})}
            }
        }catch [System.Windows.Automation.ElementNotAvailableException]{continue}
    }
}

function Get-SettingsUiPageEvidence([bool]$TargetSeen,[bool]$HeaderSeen,[bool]$SpecsSeen,[bool]$VersionSeen,[bool]$ListPageSeen) {
    $verified=$TargetSeen -and $HeaderSeen -and $SpecsSeen -and $VersionSeen -and -not $ListPageSeen
    return [ordered]@{page_target_verified=$verified;target_name_seen=$TargetSeen;details_header_seen=$HeaderSeen;specifications_seen=$SpecsSeen;version_seen=$VersionSeen;page_kind=$(if($ListPageSeen){'installed_apps_list'}elseif($verified){'app_details'}else{'unverified'});status=$(if($ListPageSeen){'wrong_settings_page'}elseif($TargetSeen -and -not $verified){'app_details_unverified'}else{'page_not_verified'})}
}

function Test-SettingsUiNodeInList($Node) {
    $current=$Node
    for($depth=0;$depth -lt 32 -and $null -ne $current;$depth++){
        $kind=$current.Current.ControlType
        if($kind -in @([System.Windows.Automation.ControlType]::List,[System.Windows.Automation.ControlType]::ListItem,[System.Windows.Automation.ControlType]::DataItem)){return $true}
        if($kind -eq [System.Windows.Automation.ControlType]::Window){return $false}
        $current=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($current)
    }
    return $null -ne $current # unknown/deep ancestry is not proof of a header
}

function Get-SettingsUiNodeEvidence($Nodes,[ValidatePattern('^[a-f0-9]{12}$')][string]$Nonce,[string]$PackageVersion='') {
    $result=[ordered]@{page_target_verified=$false;update_toggle_found=$false;toggle_count=0;toggle_ids=@();status='page_not_verified';node_read_errors=0;error_stage=$null;error_category=$null;hresult=$null}
    $ids=@();$count=0;$updateFound=$false
    $targetSeen=$false;$headerSeen=$false;$specsSeen=$false;$versionSeen=$false;$listPageSeen=$false
    foreach($node in $Nodes){
        try{
            $current=$node.Current;$name=[string]$current.Name;$controlId=[string]$current.AutomationId
            if($controlId -ceq 'SystemSettings_InstalledApps_SystemComponentsSettingLink_ButtonEntityItem'){$listPageSeen=$true}
            if($name.Contains($Nonce)){$targetSeen=$true}
            if(-not $current.IsOffscreen -and $name -ceq ('WordSuggestor LocalTest '+$Nonce) -and $current.ControlType -eq [System.Windows.Automation.ControlType]::Text -and -not (Test-SettingsUiNodeInList $node)){$headerSeen=$true}
            if(-not $current.IsOffscreen -and $current.ControlType -eq [System.Windows.Automation.ControlType]::Text){
                if($name -in @('Specifications','Specifikationer')){$specsSeen=$true}
                if($PackageVersion -and $name -ceq $PackageVersion){$versionSeen=$true}
            }
            if(-not $node.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::IsTogglePatternAvailableProperty)){continue}
            $count++
            if($controlId -cmatch '^[A-Za-z][A-Za-z0-9._-]{0,100}$'){$ids+=$controlId}
            if($name -match '(auto.*updat|updat.*auto|automat.*opdater|opdater.*automat)'){$updateFound=$true}
        }catch{
            $result.node_read_errors++
            $failure=Get-SettingsUiFailure 'read_node' $_.Exception
            foreach($key in $failure.Keys){$result[$key]=$failure[$key]}
        }
    }
    $page=Get-SettingsUiPageEvidence $targetSeen $headerSeen $specsSeen $versionSeen $listPageSeen
    foreach($key in $page.Keys){$result[$key]=$page[$key]}
    if($result.page_target_verified){
        $result.toggle_count=$count;$result.toggle_ids=@($ids | Select-Object -Unique -First 32)
        $result.update_toggle_found=$updateFound
        $result.status=if($updateFound){'read_only_control_found'}else{'no_update_toggle_observed'}
    }
    return $result
}

function Invoke-SettingsPackagedPageLaunch($Package,[string]$Runner) {
    Assert-SettingsQualificationPackage $Package.PackageFamilyName $Package
    if(Test-SettingsProcessElevated){throw 'Packaged Settings launch requires an ordinary user.'}
    $runId=[guid]::NewGuid().ToString('N')
    & $Runner --activate-settings-fixture ($Package.PackageFamilyName+'!App') $runId | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Packaged Settings activation failed.'}
    $path=Join-Path $env:LOCALAPPDATA ('WordSuggestor\LocalTests\'+$Package.Name.Split('.')[-1]+'\diagnostics\settings-page-'+$runId+'.json')
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    while(-not (Test-Path -LiteralPath $path) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 200}
    if(-not (Test-Path -LiteralPath $path)){throw 'No packaged Settings launch receipt; rebuild the fixture.'}
    if(([IO.FileInfo]$path).Length -gt 16384){throw 'Settings launch receipt too large.'}
    return Read-SettingsPageLaunchEvidence ([IO.File]::ReadAllText($path)) $Package $runId
}

function Read-SettingsPageLaunchEvidence([string]$Raw,$Package,[string]$RunId) {
    if($Raw.Length -gt 8192){throw 'Settings launch receipt too large.'}
    try{$value=$Raw|ConvertFrom-Json -ErrorAction Stop}catch{throw 'Invalid Settings launch receipt JSON.'}
    $fields=@('schema_version','run_id','package_name','package_family','package_version','packaged_context','opened','status','hresult','g05_qualified')
    if(@($value.PSObject.Properties).Count -ne $fields.Count -or @($fields|Where-Object {$null -eq $value.PSObject.Properties[$_]}).Count -gt 0 -or
       ($value.schema_version -isnot [int]) -or $value.schema_version -ne 1 -or $value.run_id -cne $runId -or $value.package_name -cne $Package.Name -or $value.package_family -cne $Package.PackageFamilyName -or $value.package_version -cne ([string]$Package.Version) -or
       $value.packaged_context -isnot [bool] -or -not $value.packaged_context -or $value.opened -isnot [bool] -or $value.g05_qualified -isnot [bool] -or $value.g05_qualified -or
       $value.status -notin @('launch_completed','launch_declined','launch_failed','launch_timeout') -or ($value.hresult -and $value.hresult -cnotmatch '^0x[A-F0-9]{8}$') -or
       ($value.opened -and $value.hresult) -or ($value.opened -ne ($value.status -ceq 'launch_completed'))){throw 'Settings launch receipt rejected.'}
    return $value
}

function Get-SettingsUpdateUiEvidence([string]$FamilyName,[string]$Runner) {
    $package=Get-AppxPackage | Where-Object PackageFamilyName -CEQ $FamilyName | Select-Object -First 1
    Assert-SettingsQualificationPackage $FamilyName $package
    if(Test-SettingsProcessElevated){throw 'Settings UI diagnosis requires an ordinary-user LocalTest.'}
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $result=Get-SettingsUiNodeEvidence @() $package.Name.Split('.')[-1]
    # Interactive, read-only: never click, toggle, reset, uninstall or stop Settings.
    if(-not $Runner){throw 'Packaged Settings launcher runner required; no external URI fallback.'}
    $launch=Invoke-SettingsPackagedPageLaunch $package $Runner
    if(-not $launch.opened){$result.status='packaged_launch_unavailable';$result.error_stage='packaged_launch';$result.hresult=$launch.hresult;$result.launch=$launch;return $result}
    $deadline=[DateTime]::UtcNow.AddSeconds(12)
    do {
        $stage='window'
        try{
            foreach($candidate in @(Get-SettingsUiWindows)){
                $stage='read_nodes'
                $window=$candidate.Element
                $nodes=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
                $result=Get-SettingsUiNodeEvidence $nodes $package.Name.Split('.')[-1] ([string]$package.Version)
                $result.window_route=$candidate.Route;$result.launch=$launch
                if($result.page_target_verified){return $result}
            }
        }catch{
            $failure=Get-SettingsUiFailure $stage $_.Exception
            foreach($key in $failure.Keys){$result[$key]=$failure[$key]}
            $result.status='ui_probe_unavailable'
        }
        Start-Sleep -Milliseconds 300
    }while([DateTime]::UtcNow -lt $deadline)
    $result.launch=$launch
    return $result
}

function Get-SettingsPolicyChangedFields($Before,$After) {
    $names=@(@($Before.PSObject.Properties.Name)+@($After.PSObject.Properties.Name) | Sort-Object -Unique)
    foreach($name in $names){
        if($name -eq 'LastCheckedForUpdates'){continue}
        if($name -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,80}$'){throw 'Unexpected native property name; no diagnostic content emitted.'}
        $left=@{value=$Before.$name}|ConvertTo-Json -Depth 12 -Compress
        $right=@{value=$After.$name}|ConvertTo-Json -Depth 12 -Compress
        if($left -cne $right){Write-Output $name}
    }
}

function Get-SettingsPolicySafeSummary($Snapshot) {
    $summary=[ordered]@{}
    foreach($property in $Snapshot.PSObject.Properties){
        if($property.Name -cnotmatch '^[A-Za-z][A-Za-z0-9_]{0,80}$'){continue}
        if($property.Value -is [bool]){$summary[$property.Name]=$property.Value}
        elseif($property.Name -ceq 'HoursBetweenUpdateChecks' -and ($property.Value -is [int] -or $property.Value -is [uint32] -or $property.Value -is [byte]) -and $property.Value -ge 0 -and $property.Value -le 255){$summary[$property.Name]=[int]$property.Value}
        elseif($property.Name -cmatch 'PolicySource$' -and [string]$property.Value -cin @('Default','System')){$summary[$property.Name]=[string]$property.Value}
    }
    return $summary
}

function Get-SettingsUpdateToggleState($Package) {
    Assert-SettingsQualificationPackage $Package.PackageFamilyName $Package
    $wanted=$Package.PackageFamilyName+'_ToggleSwitch'
    foreach($candidate in @(Get-SettingsUiWindows)){
        $nodes=$candidate.Element.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
        $page=Get-SettingsUiNodeEvidence $nodes $Package.Name.Split('.')[-1] ([string]$Package.Version)
        if(-not $page.page_target_verified){continue}
        $condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$wanted)
        $controls=$candidate.Element.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)
        if($controls.Count -ne 1 -or -not $controls[0].Current.IsEnabled){throw 'The unique test-app update control is not available.'}
        $pattern=$controls[0].GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
        $state=[string]$pattern.Current.ToggleState
        if($state -notin @('On','Off')){throw 'The update control has an indeterminate state.'}
        return $state
    }
    throw 'The test-app details page cannot be verified; no manual step started.'
}

function Get-SettingsManualPhaseEvidence($Package,$Baseline,[ValidateSet('before_reopen','after_reopen','passive','after_manual_navigation','restoration')][string]$Phase) {
    Assert-SettingsQualificationPackage $Package.PackageFamilyName $Package
    $sample=[ordered]@{phase=$Phase;observed=$null;stable_state=$null;native_changed_fields=$null}
    try{
        $sample.observed=Get-SettingsUpdateToggleState $Package
        Start-Sleep -Milliseconds 500
        $sample.stable_state=Get-SettingsUpdateToggleState $Package
    }catch{
        $failure=$_.Exception;while($failure.InnerException){$failure=$failure.InnerException}
        $sample.ui_read_failure=[ordered]@{error_category=$failure.GetType().Name;hresult=('0x{0:X8}' -f ($failure.HResult -band 0xffffffffL))}
    }
    # Read native state even when the visible control differs from the request.
    try{
        $native=Get-SettingsUpdateSnapshot $Package.PackageFamilyName
        $sample.native_changed_fields=@(Get-SettingsPolicyChangedFields $Baseline $native)
        $sample.native=Get-SettingsPolicySafeSummary $native
    }catch{
        $failure=$_.Exception;while($failure.InnerException){$failure=$failure.InnerException}
        $sample.native_read_failure=[ordered]@{error_category=$failure.GetType().Name;hresult=('0x{0:X8}' -f ($failure.HResult -band 0xffffffffL))}
    }
    Write-Host ('G05 manual phase='+$Phase+' observed='+$sample.observed+' stable='+$sample.stable_state+' native_changed_fields='+($sample.native_changed_fields -join ',')+' native_read_hresult='+$sample.native_read_failure.hresult)
    return $sample
}

function Invoke-SettingsManualUpdateRoundtrip($Package,[string]$Runner,$InitialEvidence,[Collections.IDictionary]$Result) {
    Assert-SettingsQualificationPackage $Package.PackageFamilyName $Package
    if(Test-SettingsProcessElevated){throw 'Manual Settings roundtrip requires an ordinary user.'}
    if(-not $InitialEvidence.page_target_verified -or -not $InitialEvidence.update_toggle_found -or $InitialEvidence.page_kind -cne 'app_details'){throw 'Manual roundtrip requires verified test-app details and control.'}
    $Result.schema_version=1;$Result.status='not_started';$Result.steps=@();$Result.g05_qualified=$false
    $initial=Get-SettingsUpdateToggleState $Package
    $before=Get-SettingsUpdateSnapshot $Package.PackageFamilyName
    $Result.initial_native=Get-SettingsPolicySafeSummary $before
    $Result.initial_state=$initial;$Result.ui_state_restored=$false;$Result.native_snapshot_restored=$false
    $opposite=if($initial -ceq 'On'){'Off'}else{'On'}
    Write-Host ('MANUAL G05: TWO SEPARATE changes, one per Enter: '+$initial+' -> '+$opposite+' -> '+$initial+'. Do not perform both changes before the first Enter.')
    try{
        foreach($requested in @($opposite,$initial)){
            $step=[ordered]@{requested=$requested;observed=$null;ui_verified=$false;native_changed_fields=$null}
            $Result.steps+=@($step);$Result.status='awaiting_manual_change'
            $stepNumber=$Result.steps.Count
            Write-Host ('MANUAL G05 step '+$stepNumber+'/2: Only WordSuggestor LocalTest '+$Package.Name.Split('.')[-1]+': change its Windows update control to '+$requested+'. LEAVE IT '+$requested+' while pressing Enter. Do not change any other app.')
            if($stepNumber -eq 1){Write-Host 'Do NOT switch back yet. Wait for the separate step 2/2 instruction after verification.'}
            $null=Read-Host ('With the control still '+$requested+', return here and press Enter (Ctrl+C aborts; cleanup is attempted)')
            # Capture the current page BEFORE any packaged activation/navigation.
            # Reacquire UIA elements on every read; never click, set or cache state.
            $step.before_reopen=Get-SettingsManualPhaseEvidence $Package $before 'before_reopen'
            $Result.status='reopening_settings'
            $page=Get-SettingsUpdateUiEvidence $Package.PackageFamilyName $Runner
            $step.reopen=$page
            if(-not $page.page_target_verified){$Result.status='reopened_page_unverified';throw 'Reopened test-app details were not verified; the pre-navigation sample is retained.'}
            $step.after_reopen=Get-SettingsManualPhaseEvidence $Package $before 'after_reopen'
            $step.observed=$step.after_reopen.observed
            $step.native_changed_fields=$step.after_reopen.native_changed_fields
            if($step.after_reopen.Contains('native')){$step.native=$step.after_reopen.native}
            if($step.after_reopen.Contains('native_read_failure')){$step.native_read_failure=$step.after_reopen.native_read_failure}
            foreach($phase in @('before_reopen','after_reopen')){
                $sample=$step[$phase]
                if($sample.Contains('ui_read_failure')){$Result.status='ui_read_failed_'+$phase;throw 'The control state could not be read; phase evidence is retained.'}
                if($sample.observed -cne $requested){$Result.status='unexpected_state_'+$phase;throw 'Observed UI state differs from the request. Both phase samples are retained; no cause is inferred.'}
                if($sample.stable_state -cne $requested){$Result.status='unstable_state_'+$phase;throw 'UI state changed between reads. Both phase samples are retained; no cause is inferred.'}
            }
            $step.ui_verified=$true
            Write-Host ('G05 manual observed='+$step.observed+' native_changed_fields='+($step.native_changed_fields -join ',')+' native_read_hresult='+$step.native_read_failure.hresult)
        }
        $Result.status='completed'
    }finally{
        try{
            $Result.final_state=Get-SettingsUpdateToggleState $Package
            $Result.ui_state_restored=$Result.final_state -ceq $initial
            $after=Get-SettingsUpdateSnapshot $Package.PackageFamilyName
            $Result.final_native=Get-SettingsPolicySafeSummary $after
            $Result.final_native_changed_fields=@(Get-SettingsPolicyChangedFields $before $after)
            $Result.native_snapshot_restored=$Result.final_native_changed_fields.Count -eq 0
            if($Result.status -ceq 'completed'){
                if(-not $Result.ui_state_restored){$Result.status='ui_not_restored'}
                elseif(-not $Result.native_snapshot_restored){$Result.status='ui_restored_native_changed'}
            }
        }catch{$Result.restoration_read_failed=$true;$Result.status='restoration_read_failed'}
    }
}

function Wait-SettingsObservationOffset([Diagnostics.Stopwatch]$Watch,[ValidateRange(0,15000)][int]$TargetMilliseconds) {
    while($Watch.ElapsedMilliseconds -lt $TargetMilliseconds){
        $remaining=$TargetMilliseconds-$Watch.ElapsedMilliseconds
        if($remaining -le 0){break}
        Start-Sleep -Milliseconds ([int][Math]::Min(200,$remaining))
    }
}

function Invoke-SettingsPassiveUpdateObservation($Package,$InitialEvidence,[Collections.IDictionary]$Result) {
    Assert-SettingsQualificationPackage $Package.PackageFamilyName $Package
    if(Test-SettingsProcessElevated){throw 'Passive Settings observation requires an ordinary user.'}
    if(-not $InitialEvidence.page_target_verified -or -not $InitialEvidence.update_toggle_found -or $InitialEvidence.page_kind -cne 'app_details'){throw 'Passive observation requires verified test-app details and control.'}
    $Result.schema_version=1;$Result.status='not_started';$Result.samples=@();$Result.g05_qualified=$false
    $initial=Get-SettingsUpdateToggleState $Package
    $baseline=Get-SettingsUpdateSnapshot $Package.PackageFamilyName
    $Result.initial_state=$initial;$Result.initial_native=Get-SettingsPolicySafeSummary $baseline
    $requested=if($initial -ceq 'On'){'Off'}else{'On'}
    $Result.requested_state=$requested
    $label='WordSuggestor LocalTest '+$Package.Name.Split('.')[-1]
    $Result.ui_state_restored=$false;$Result.native_snapshot_restored=$false
    try{
        $Result.status='awaiting_manual_change'
        Write-Host ('PASSIVE G05 step 1/3: Only '+$label+': switch to '+$requested+' and LEAVE IT '+$requested+'. Then press Enter here. Do not switch back.')
        $null=Read-Host 'After Enter, do not click or navigate in Settings until step 2/3; four reads are scheduled over 15 seconds'
        $Result.status='observing_without_activation'
        $watch=[Diagnostics.Stopwatch]::StartNew()
        foreach($offset in @(0,5000,10000,15000)){
            Wait-SettingsObservationOffset $watch $offset
            $sample=Get-SettingsManualPhaseEvidence $Package $baseline 'passive'
            $sample.requested_offset_ms=$offset;$sample.elapsed_ms=$watch.ElapsedMilliseconds
            $Result.samples+=@($sample)
        }
        $states=@(@(foreach($sample in $Result.samples){$sample.observed;$sample.stable_state})|Where-Object {$_ -in @('On','Off')})
        $Result.passive_ui_transition_observed=@($states|Select-Object -Unique).Count -gt 1
        $Result.passive_ui_reads_complete=@($Result.samples|Where-Object {$_.Contains('ui_read_failure')}).Count -eq 0
        $Result.first_requested_ui_observed=$Result.samples[0].observed -ceq $requested -and $Result.samples[0].stable_state -ceq $requested
        $Result.passive_native_reads_succeeded=@($Result.samples|Where-Object {-not $_.Contains('native_read_failure')}).Count
        $Result.passive_native_change_observed=if($Result.passive_native_reads_succeeded -eq 0){$null}else{@($Result.samples|Where-Object {$_.native_changed_fields.Count -gt 0}).Count -gt 0}
        Write-Host ('G05 passive first_requested='+$Result.first_requested_ui_observed+' ui_transition='+$Result.passive_ui_transition_observed+' native_reads='+$Result.passive_native_reads_succeeded+' native_change='+$Result.passive_native_change_observed)
        $Result.status='awaiting_manual_navigation'
        Write-Host ('PASSIVE G05 step 2/3: In Windows Settings only, go Back to Installed apps. Find '+$label+' and open its Advanced options. Do NOT launch WordSuggestor or toggle any setting.')
        $null=Read-Host 'Once the SAME test app details are visible again, return here and press Enter'
        # No runner argument, activation, Launcher, URI, navigation or UI writes here.
        $Result.after_manual_navigation=Get-SettingsManualPhaseEvidence $Package $baseline 'after_manual_navigation'
        if($Result.after_manual_navigation.Contains('ui_read_failure')){$Result.status='manual_navigation_unverified';throw 'The manually reopened test-app control cannot be verified; samples retained.'}
        $Result.status='awaiting_manual_restoration'
        Write-Host ('PASSIVE G05 step 3/3: Only '+$label+': verify the control is '+$initial+' (the starting state). If already '+$initial+', leave it unchanged. Otherwise restore it manually.')
        $null=Read-Host ('With the control '+$initial+', press Enter for final readback and cleanup')
        $Result.status='observation_completed'
    }finally{
        $Result.final=Get-SettingsManualPhaseEvidence $Package $baseline 'restoration'
        $Result.ui_state_restored=-not $Result.final.Contains('ui_read_failure') -and $Result.final.observed -ceq $initial -and $Result.final.stable_state -ceq $initial
        $Result.native_snapshot_restored=-not $Result.final.Contains('native_read_failure') -and $Result.final.native_changed_fields.Count -eq 0
        if($Result.status -ceq 'observation_completed'){
            if(-not $Result.ui_state_restored){$Result.status='observation_completed_ui_not_restored'}
            elseif(-not $Result.native_snapshot_restored){$Result.status='observation_completed_native_changed'}
        }
    }
}

function Get-SettingsSourceSnapshot([string]$Repository) {
    $head = & git -C $Repository rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read source revision.' }
    $status = @(& git -C $Repository status --porcelain=v1)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read source status.' }
    $names = ((& git -C $Repository -c core.quotepath=false ls-files -z --cached --others --exclude-standard) -join "`n").Split([char]0)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate source snapshot.' }
    $files = @(foreach ($name in ($names | Where-Object { $_ } | Sort-Object -Unique)) {
        $path = Join-Path $Repository $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            [ordered]@{ path=$name.Replace('\','/'); sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
        } else { [ordered]@{path=$name.Replace('\','/'); deleted=$true} }
    })
    return [ordered]@{ head=([string]$head).Trim(); working_tree_dirty=($status.Count -gt 0); status=$status; files=$files }
}
