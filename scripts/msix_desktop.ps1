# Qualification-only orchestration. Inspect native package identities, never
# window captions/content. No clicks, input, process termination or trust changes.
function Read-MsixDesktopResult([string]$Json) {
    if($Json.Length -gt 4096){throw 'Desktop probe output too large.'}
    $value=$Json | ConvertFrom-Json -ErrorAction Stop
    $names=@($value.PSObject.Properties.Name | Sort-Object)
    if(($names -join ',') -cne 'foreground_process_id,installer_foreground,schema_version,visible_installer_windows' -or
       $value.schema_version -isnot [int] -or $value.schema_version -ne 1 -or
       $value.visible_installer_windows -isnot [int] -or $value.visible_installer_windows -lt 0 -or $value.visible_installer_windows -gt 256 -or
       $value.installer_foreground -isnot [bool] -or $value.foreground_process_id -isnot [int] -or $value.foreground_process_id -lt 0 -or
       ($value.installer_foreground -and $value.visible_installer_windows -eq 0)){throw 'Desktop probe schema rejected.'}
    return $value
}
function Get-MsixDesktopState([string]$Runner) {
    $result=Invoke-ReleaseProcess $Runner @('--installer-desktop-state') 10
    if($result.ExitCode -ne 0){throw 'Read-only App Installer desktop probe failed.'}
    return Read-MsixDesktopResult $result.Output
}
function Get-MsixAutomaticDecision([string]$InstalledVersion,[string]$ExpectedVersion,$Desktop) {
    if($Desktop.visible_installer_windows -gt 0){return 'interactive_dialog'}
    if($InstalledVersion -ceq $ExpectedVersion){return 'updated'}
    return 'pending'
}
function Wait-MsixInstallerIdle([scriptblock]$ReadDesktop,$Record,[string]$Phase,
    [ValidateRange(1,120)][int]$TimeoutSeconds=120) {
    if(-not $Record.Contains('installer_desktop')){$Record.installer_desktop=[ordered]@{}}
    $evidence=[ordered]@{dialog_observed=$false;idle=$false;foreground_process_id=0}
    $Record.installer_desktop[$Phase]=$evidence
    $clock=[Diagnostics.Stopwatch]::StartNew();$clearSince=$null
    while($clock.Elapsed.TotalSeconds -lt $TimeoutSeconds){
        $desktop=& $ReadDesktop
        $evidence.foreground_process_id=$desktop.foreground_process_id
        if($desktop.visible_installer_windows -gt 0){
            if($Record.Contains('checks') -and $Record.checks.Contains('automatic_update') -and $Record.checks.automatic_update){
                $Record.checks.automatic_update=$false
                $Record.automatic_update_not_completed_reason='interactive_app_installer'
                Write-Host 'PENDING MSIX previous automatic observation invalidated by a later App Installer dialog.'
            }
            if(-not $evidence.dialog_observed){
                Write-Host ('ACTION MSIX '+$Phase+': close the WordSuggestor test App Installer dialog with Cancel/Annuller. Do not choose Finish Updating or Open Now. Close any other App Installer window yourself, then leave mouse/keyboard idle. Waiting up to '+$TimeoutSeconds+' seconds.')
            }
            $evidence.dialog_observed=$true;$clearSince=$null
        }else{
            if($null -eq $clearSince){$clearSince=$clock.Elapsed.TotalSeconds}
            if($clock.Elapsed.TotalSeconds-$clearSince -ge 0.5){$evidence.idle=$true;return}
        }
        Start-Sleep -Milliseconds 250
    }
    $Record.desktop_blocked_phase=$Phase
    throw ('MSIX.EnvironmentBlocked.AppInstaller phase='+$Phase+'. Dialog did not close; next phase was not started. Close it with Cancel and prepare a fresh test session.')
}
