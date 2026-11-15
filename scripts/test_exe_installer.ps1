[CmdletBinding(SupportsShouldProcess=$true)]
param([Parameter(Mandatory=$true)][string]$PreviousBundleDirectory,
      [Parameter(Mandatory=$true)][string]$CandidateBundleDirectory,
      [switch]$AllowLocalPreviewInstallation)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'release_transport.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Read-Preview([string]$Directory){
    $path=[IO.Path]::GetFullPath($Directory)
    if(-not $path.StartsWith(($repo+'\.artifacts\release\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Preview must be built under release artifacts.'}
    $draft=Get-Content (Join-Path $path 'release-manifest.draft.json') -Raw|ConvertFrom-Json
    if(-not $draft.local_preview -or $draft.product_id -cnotmatch '^WordSuggestor\.Windows\.Preview\.[a-f0-9]{12}$' -or $draft.release.package_type -cne 'exe' -or $draft.release.artifact.filename -cnotmatch '^WordSuggestor\.Preview_[0-9.]+_[a-f0-9]{12}_x64\.exe$'){throw 'Only isolated preview installers can be tested.'}
    $installer=Join-Path $path $draft.release.artifact.filename
    if((Get-Item $installer).Length -ne $draft.release.artifact.size_bytes -or (Get-FileHash $installer -Algorithm SHA256).Hash -ine $draft.release.artifact.sha256){throw 'Preview installer changed.'}
    $pe=[Diagnostics.FileVersionInfo]::GetVersionInfo($installer)
    $numeric=[version]::new($pe.FileMajorPart,$pe.FileMinorPart,$pe.FileBuildPart,$pe.FilePrivatePart)
    if($numeric -ne [version]$draft.release.package_version){throw 'Installer numeric version mismatch.'}
    return [pscustomobject]@{Installer=$installer;Draft=$draft}
}
$previous=Read-Preview $PreviousBundleDirectory;$candidate=Read-Preview $CandidateBundleDirectory
if($previous.Draft.product_id -cne $candidate.Draft.product_id -or [version]$candidate.Draft.release.package_version -le [version]$previous.Draft.release.package_version){throw 'Expected the same preview identity and a newer candidate.'}
if(-not $AllowLocalPreviewInstallation){Write-Warning 'BLOCKED: explicitly allow an isolated per-user preview installation.';exit 3}
$principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Installer qualification must run without administrator rights.'}
$nonce=$previous.Draft.product_id.Split('.')[-1]
$install=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA ('Programs\WordSuggestorPreview-'+$nonce)))
$allowed=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs'))+'\'
if(-not $install.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $install)){throw 'Existing installation is preserved; build a fresh preview identity.'}
$data=Join-Path $env:LOCALAPPDATA ('WordSuggestor\LocalTests\'+$nonce)
if(Test-Path -LiteralPath $data){throw 'Existing preview data preserved; build a fresh identity.'}
if(-not $PSCmdlet.ShouldProcess($install,'Install, probe bundled Core, upgrade, reject downgrade, uninstall; retain isolated test data')){return}
$root=Join-Path $repo ('.artifacts\exe-tests\'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $root -Force|Out-Null
$record=[ordered]@{schema_version=1;product_id=$previous.Draft.product_id;checks=[ordered]@{};completed=$false;release_qualified=$false}
$installed=$false
function Pass([string]$Name){$record.checks[$Name]=$true;Write-Output ('PASS EXE INSTALLER '+$Name)}
function Run-Installer($Artifact,[string]$Phase){
    $result=Invoke-ReleaseProcess $Artifact.Installer @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/LOG='+ (Join-Path $root ($Phase+'.log')))) 120
    if($result.ExitCode -ne 0){throw 'Preview installer failed.'}
}
function Probe([string]$Mode,[string]$Version){
    $resultPath=Join-Path $data ('diagnostics\qualification-'+[guid]::NewGuid().ToString('N')+'.json')
    $run=Invoke-ReleaseProcess (Join-Path $install 'WordSuggestorWindows.App.exe') @('--qualification-probe',$Mode,'--qualification-root',$data,'--qualification-result',$resultPath) 40
    if($run.ExitCode -ne 0 -or -not (Test-Path $resultPath)){throw 'Installed EXE Core/pack probe failed.'}
    $value=Get-Content $resultPath -Raw|ConvertFrom-Json
    if(-not $value.ok -or $value.package_name -cne $previous.Draft.product_id -or $value.package_version -cne $Version){throw 'Installed EXE identity/Core result mismatch.'}
}
try{
    Run-Installer $previous 'install';$installed=$true
    $marker=Get-Content (Join-Path $install 'direct-install.json') -Raw|ConvertFrom-Json
    if($marker.product_id -cne $previous.Draft.product_id -or $marker.updates_enabled){throw 'Preview installation identity/updates mismatch.'}
    Pass 'clean_install_without_admin'
    Probe 'add' $previous.Draft.release.package_version;Pass 'bundled_core_da_user_word'
    New-Item -ItemType Directory -Path (Join-Path $data 'settings') -Force|Out-Null
    $settings=Join-Path $data 'settings\settings-v1.json'
    [IO.File]::WriteAllText($settings,'{"schemaVersion":9,"interfaceLocalePreference":"da","checkExeUpdatesAutomatically":false,"selectedLanguageCode":"da-DK"}')
    $hash=(Get-FileHash $settings -Algorithm SHA256).Hash
    Run-Installer $candidate 'upgrade'
    Probe 'check' $candidate.Draft.release.package_version;Pass 'n_to_n1_core_and_user_word_retention'
    if((Get-FileHash $settings -Algorithm SHA256).Hash -cne $hash){throw 'Installer modified user settings.'};Pass 'settings_retention'
    $downgrade=Invoke-ReleaseProcess $previous.Installer @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-') 30
    $now=Get-Content (Join-Path $install 'direct-install.json') -Raw|ConvertFrom-Json
    if($downgrade.ExitCode -eq 0 -or $now.package_version -cne $candidate.Draft.release.package_version){throw 'Older installer was not rejected.'};Pass 'older_installer_rejected'
    $uninstaller=Join-Path $install 'uninstall\unins000.exe'
    $result=Invoke-ReleaseProcess $uninstaller @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') 60
    if($result.ExitCode -ne 0 -or (Test-Path (Join-Path $install 'WordSuggestorWindows.App.exe'))){throw 'Preview uninstall failed.'}
    $installed=$false
    if((Get-FileHash $settings -Algorithm SHA256).Hash -cne $hash){throw 'Uninstall modified retained data.'};Pass 'uninstall_preserves_user_data'
    $record.completed=$true
}finally{
    if($installed -and (Test-Path -LiteralPath (Join-Path $install 'direct-install.json'))){
        $owned=Get-Content (Join-Path $install 'direct-install.json') -Raw|ConvertFrom-Json
        $uninstaller=Join-Path $install 'uninstall\unins000.exe'
        if($owned.product_id -ceq $previous.Draft.product_id -and (Test-Path $uninstaller)){$null=Invoke-ReleaseProcess $uninstaller @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') 60}
    }
    [IO.File]::WriteAllText((Join-Path $root 'qualification.json'),($record|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
    Write-Output ('EXE qualification record: '+(Join-Path $root 'qualification.json'))
}
