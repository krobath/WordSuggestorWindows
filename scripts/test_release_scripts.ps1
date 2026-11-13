$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release_common.ps1')
. (Join-Path $PSScriptRoot 'release_transport.ps1')
. (Join-Path $PSScriptRoot 'msix_integrity.ps1')
. (Join-Path $PSScriptRoot 'msix_desktop.ps1')
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root = Join-Path $repo ('.artifacts\tests\release-' + [guid]::NewGuid().ToString('N'))
$bundle = Join-Path $root 'bundle'
New-Item -ItemType Directory -Path $bundle -Force | Out-Null
$script:passed = 0
function Check([string]$Name, [bool]$Pass) { if (-not $Pass) { throw "FAIL RELEASE $Name" }; $script:passed++; Write-Output "PASS RELEASE $Name" }
function Reject([string]$Name, [scriptblock]$Action) {
    $failed = $false; try { & $Action | Out-Null } catch { $failed = $true }
    Check $Name $failed
}
$packagePath = Join-Path $bundle 'WordSuggestor.Beta_0.2.0.1_x64.msix'
$identity = [pscustomobject]@{ Name='WordSuggestor.Windows.Beta'; Publisher='CN=Fixture'; Version='0.2.0.1'; Architecture='x64' }
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($packagePath,[IO.Compression.ZipArchiveMode]::Create)
try {
    $entry = $zip.CreateEntry('AppxManifest.xml')
    $writer = New-Object IO.StreamWriter($entry.Open(),[Text.UTF8Encoding]::new($false))
    try { $writer.Write('<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"><Identity Name="WordSuggestor.Windows.Beta" Publisher="CN=Fixture" Version="0.2.0.1" ProcessorArchitecture="x64" /><Properties><uap10:PackageIntegrity><uap10:Content Enforcement="on"/></uap10:PackageIntegrity></Properties></Package>') }
    finally { $writer.Dispose() }
} finally { $zip.Dispose() }
$feedPath = Join-Path $bundle 'WordSuggestor.Beta.appinstaller'
$feed = @'
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018" Version="0.2.0.1" Uri="https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.appinstaller">
<MainPackage Name="WordSuggestor.Windows.Beta" Publisher="CN=Fixture" Version="0.2.0.1" ProcessorArchitecture="x64" Uri="https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta_0.2.0.1_x64.msix" />
<UpdateSettings><OnLaunch HoursBetweenUpdateChecks="6" ShowPrompt="true" UpdateBlocksActivation="false" /><AutomaticBackgroundTask /><ForceUpdateFromAnyVersion>false</ForceUpdateFromAnyVersion></UpdateSettings></AppInstaller>
'@
[IO.File]::WriteAllText($feedPath,$feed,[Text.UTF8Encoding]::new($false))
$manifest = [ordered]@{ schema_version=1; release=[ordered]@{
    platform='windows'; architecture='x64'; channel='beta'; version='0.2.0-beta.1'; build_number='2001';
    package_version='0.2.0.1'; package_type='msix'; status='pending'; minimum_os='Windows 10 2004 or later, x64';
    status_message=''; release_notes=@('Fixture'); known_issues=@(); artifact=(Get-ReleaseFileContract $packagePath);
    update_feed=(Get-ReleaseFileContract $feedPath); signing=[ordered]@{ publisher='CN=Fixture'; identity=''; signature_verified=$false;
        verification_result='not_checked'; verified_at=''; verification_tool='SignTool verify /pa /all /v' } } }
$manifestPath = Join-Path $bundle 'release-manifest.json'
function Write-Manifest { [IO.File]::WriteAllText($manifestPath,($manifest | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false)) }
Write-Manifest
Check 'pending bundle is structurally valid (not trusted signing)' ((Test-ReleaseBundle $bundle).Identity.Name -ceq $identity.Name)
Reject 'unsigned fixture cannot pass actual trust gate' { Test-ReleaseBundle $bundle -VerifySignature }
$manifest.release.artifact.sha256 = 'a' * 64; Write-Manifest
Reject 'checksum mismatch rejected before SSH' { Test-ReleaseBundle $bundle }
$manifest.release.artifact = Get-ReleaseFileContract $packagePath; Write-Manifest
$manifest.release.status = 'active'; Write-Manifest
Reject 'unsigned active rejected' { Test-ReleaseBundle $bundle }
$manifest.release.status = 'pending'; $manifest.release.download_url='https://untrusted.example/file'; Write-Manifest
Reject 'uploader manifest cannot choose URLs' { Test-ReleaseBundle $bundle }
$manifest.release.Remove('download_url'); Write-Manifest
$manifest.release.artifact.filename = '../escape.msix'; Write-Manifest
Reject 'path traversal rejected' { Test-ReleaseBundle $bundle }
$manifest.release.artifact = Get-ReleaseFileContract $packagePath; Write-Manifest
[IO.File]::WriteAllText((Join-Path $bundle 'unexpected.txt'),'fixture')
Reject 'unexpected bundle file rejected' { Test-ReleaseBundle $bundle }
Remove-Item -LiteralPath (Join-Path $bundle 'unexpected.txt')
foreach ($pair in @(@('identity',$feed.Replace('Name="WordSuggestor.Windows.Beta"','Name="Other.App"')),
    @('publisher',$feed.Replace('CN=Fixture','CN=Other')), @('version',$feed.Replace('Version="0.2.0.1"','Version="0.2.0.0"')),
    @('downgrade',$feed.Replace('>false</ForceUpdateFromAnyVersion>','>true</ForceUpdateFromAnyVersion>')),
    @('origin',$feed.Replace('https://wordsuggestor.com','http://evil.example')),
    @('extra policy',$feed.Replace('</UpdateSettings>','<Unknown /></UpdateSettings>')),
    @('duplicate policy',$feed.Replace('<AutomaticBackgroundTask />','<AutomaticBackgroundTask /><AutomaticBackgroundTask />')),
    @('extra dependency',$feed.Replace('</AppInstaller>','<OptionalPackages /></AppInstaller>')),
    @('no prompt',$feed.Replace('ShowPrompt="true"','ShowPrompt="false"')))) {
    [IO.File]::WriteAllText($feedPath,$pair[1])
    Reject ("feed $($pair[0]) rejected") { Test-AppInstallerDocument $feedPath $identity 'WordSuggestor.Beta_0.2.0.1_x64.msix' }
}
[IO.File]::WriteAllText($feedPath,$feed)
$manifest.release.update_feed = Get-ReleaseFileContract $feedPath; Write-Manifest
Reject 'noncanonical version rejected' { Assert-PackageVersion '0.02.0.1' }
Reject 'overflow version rejected' { Assert-PackageVersion '0.2.65536.1' }

# Public host keys are fixtures. No SSH connection is made by these pin checks.
$key = [byte[]](0,0,0,11) + [Text.Encoding]::ASCII.GetBytes('ssh-ed25519') + [byte[]](0,0,0,32) + [byte[]](1..32)
$sha = [Security.Cryptography.SHA256]::Create()
try { $pin = 'SHA256:' + [Convert]::ToBase64String($sha.ComputeHash($key)).TrimEnd('=') } finally { $sha.Dispose() }
$knownHosts = Join-Path $root 'known_hosts'
[IO.File]::WriteAllText($knownHosts,('fixture.example ssh-ed25519 ' + [Convert]::ToBase64String($key)))
$options = @(Get-PinnedSshOptions 'fixture.example' 22 'publisher' $knownHosts $pin '')
Check 'strict ED25519 pin and dedicated known_hosts' (($options -contains 'StrictHostKeyChecking=yes') -and ($options -contains 'HostKeyAlgorithms=ssh-ed25519'))
Reject 'changed host key rejected' { Get-PinnedSshOptions 'fixture.example' 22 'publisher' $knownHosts ('SHA256:' + ('a' * 43)) '' }
Reject 'wrong SSH host rejected' { Get-PinnedSshOptions 'other.example' 22 'publisher' $knownHosts $pin '' }
$response = [pscustomobject]@{ ExitCode=0; Output='{"ok":true,"code":"OK","operation":"validate","validated":true,"target":{"platform":"windows","architecture":"x64","channel":"beta","package_version":"0.2.0.1"}}' }
Check 'structured target success parsed' ((Read-PublisherResult $response 'validate' '0.2.0.1').ok)
foreach ($code in @{2='VALIDATION_ERROR';3='CONFLICT';4='INTEGRITY_ERROR';5='PUBLICATION_ERROR';6='HTTPS_VERIFICATION_ERROR';7='CONFIGURATION_ERROR';8='NOT_FOUND';9='INTERNAL_ERROR'}.GetEnumerator()) {
    $result = [pscustomobject]@{ ExitCode=[int]$code.Key; Output=(@{ok=$false;code=$code.Value;message='PRIVATE_SERVER_ERROR'} | ConvertTo-Json -Compress) }
    Reject ("server exit $($code.Key) handled") { Read-PublisherResult $result 'publish' '0.2.0.1' }
}
Reject 'malformed server result rejected' { Read-PublisherResult ([pscustomobject]@{ ExitCode=0;Output='banner then json' }) 'validate' '0.2.0.1' }
Reject 'transport failure handled' { Read-PublisherResult ([pscustomobject]@{ ExitCode=255;Output='' }) 'validate' '0.2.0.1' }
$plans = @(& (Join-Path $PSScriptRoot 'publish_beta_release.ps1') -BundleDirectory $bundle -BackendDirectory '/reviewed/backend' -HostName fixture.example -UserName publisher -KnownHostsFile $knownHosts -HostKeyFingerprint $pin -WhatIf)
$plan = $plans | Where-Object { $_.PSObject.Properties['what_if'] } | Select-Object -Last 1
Check 'WhatIf makes no connection and previews exactly three files' ($plan -and -not $plan.network_contacted -and $plan.files.Count -eq 3 -and -not $plan.signature_checked)
$second = & (Join-Path $PSScriptRoot 'publish_beta_release.ps1') -BundleDirectory $bundle -BackendDirectory '/reviewed/backend' -HostName fixture.example -UserName publisher -KnownHostsFile $knownHosts -HostKeyFingerprint $pin -WhatIf |
    Where-Object { $_.PSObject.Properties['what_if'] }
Check 'new staging ID for every attempt' ($plan.staging_id -cne $second.staging_id -and $plan.staging_path.EndsWith('.uploading'))

$echoPath = Join-Path $root 'echo.ps1'
[IO.File]::WriteAllText($echoPath,'param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Values) [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false); [Console]::Write(($Values | ConvertTo-Json -Compress))',[Text.UTF8Encoding]::new($true))
$powershell = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$blocked = Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1')) 15
Check 'local install harness requires explicit temporary-trust opt-in' ($blocked.ExitCode -eq 3 -and $blocked.Output.Contains('BLOCKED MSIX'))
$values = @('space value','quote"value','trailing\','Prøve æøå','$(literal)')
$echo = Invoke-ReleaseProcess $powershell (@('-NoProfile','-ExecutionPolicy','Bypass','-File',$echoPath) + $values) 10
$decoded = $echo.Output | ConvertFrom-Json
Check 'native arguments preserve quotes Unicode and literal shell syntax' ($echo.ExitCode -eq 0 -and @(Compare-Object $values $decoded -CaseSensitive).Count -eq 0)
$timeoutScript = Join-Path $root 'timeout.ps1'
$pidFile = Join-Path $root 'transport-child.pid'
[IO.File]::WriteAllText($timeoutScript,'param([string]$PidFile) [IO.File]::WriteAllText($PidFile,[string]$PID); Start-Sleep -Seconds 30')
Reject 'transport deadline enforced' { Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',$timeoutScript,'-PidFile',$pidFile) 2 }
$childPid = [int][IO.File]::ReadAllText($pidFile)
Check 'timed-out transport process actually reaped' (-not (Get-Process -Id $childPid -ErrorAction SilentlyContinue))
$overflowScript = Join-Path $root 'overflow.ps1'
[IO.File]::WriteAllText($overflowScript,'[Console]::Write(("x" * 1100000))')
Reject 'transport stdout is bounded' { Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',$overflowScript) 10 }

# Execute the real staging transaction against a mocked process boundary. This
# checks ordering and failure containment without any SSH access or credentials.
$originalInvoker = (Get-Command Invoke-ReleaseProcess).ScriptBlock
$script:transportCalls = New-Object 'Collections.Generic.List[object]'
$script:failTransfer = $false; $script:failRename = $false
try {
    Set-Item Function:\Invoke-ReleaseProcess -Value {
        param([string]$Executable,[string[]]$Arguments,[int]$TimeoutSeconds)
        $command = $Arguments[-1]
        $call = [pscustomobject]@{ executable=$Executable; arguments=$Arguments; command=$command; batch=@() }
        if ($Executable -eq 'fixture-sftp') {
            $index = [Array]::IndexOf($Arguments,'-b')
            $call.batch = [IO.File]::ReadAllLines($Arguments[$index+1])
        }
        $script:transportCalls.Add($call)
        if (($Executable -eq 'fixture-sftp' -and $script:failTransfer) -or ($command.Contains('mv -T') -and $script:failRename)) {
            return [pscustomobject]@{ ExitCode=1;Output='';Error='PRIVATE_TRANSPORT_DETAIL' }
        }
        $operation = if ($command -match 'publish_beta_release_on_docker01.sh (validate|register|publish) ') { $Matches[1] } else { '' }
        $json = @{ok=$true;code='OK';operation=$operation;validated=$true;status=$(if ($operation -eq 'publish') {'active'} else {'pending'});
            target=@{platform='windows';architecture='x64';channel='beta';package_version='0.2.0.1'}} | ConvertTo-Json -Depth 5 -Compress
        return [pscustomobject]@{ExitCode=0;Output=$json;Error=''}
    }
    $endpoint = [pscustomobject]@{HostName='fixture.example';Port=22;UserName='publisher';KnownHostsFile=$knownHosts;
        HostKeyFingerprint=$pin;IdentityFile='';SshPath='fixture-ssh';SftpPath='fixture-sftp';BackendDirectory='/reviewed/backend';TimeoutSeconds=10}
    $candidate = Test-ReleaseBundle $bundle
    $transaction = Invoke-PrivateReleaseStaging $candidate $endpoint 'register' ('windows-x64-beta-0.2.0.1-' + [guid]::NewGuid().ToString('D'))
    Check 'pending transaction creates uploads renames validates then registers' ($script:transportCalls.Count -eq 5 -and
        $script:transportCalls[0].command.Contains('mkdir -m 700') -and $script:transportCalls[1].executable -eq 'fixture-sftp' -and
        $script:transportCalls[2].command.Contains('mv -T -n') -and $script:transportCalls[3].command.Contains(' validate ') -and
        $script:transportCalls[4].command.Contains(' register ') -and $transaction.result.status -ceq 'pending')
    Check 'exactly three files go only to uploading directory' ($script:transportCalls[1].batch.Count -eq 3 -and
        @($script:transportCalls[1].batch | Where-Object { $_ -notmatch '\.uploading/' }).Count -eq 0 -and
        @($script:transportCalls | Where-Object { $_.command -match '/wordsuggestorbackend_beta/' }).Count -eq 0)
    $script:transportCalls.Clear(); $script:failTransfer = $true
    Reject 'failed transfer never renames validates or activates' { Invoke-PrivateReleaseStaging $candidate $endpoint 'register' ('windows-x64-beta-0.2.0.1-' + [guid]::NewGuid().ToString('D')) }
    Check 'failed transfer stops after private upload' ($script:transportCalls.Count -eq 2)
    $script:failTransfer=$false; $script:failRename=$true; $script:transportCalls.Clear()
    Reject 'failed atomic rename never reaches server validator' { Invoke-PrivateReleaseStaging $candidate $endpoint 'register' ('windows-x64-beta-0.2.0.1-' + [guid]::NewGuid().ToString('D')) }
    Check 'failed rename stops before validation' ($script:transportCalls.Count -eq 3)
    $script:failRename=$false; $script:transportCalls.Clear()
    Reject 'register cannot activate active metadata' {
        $candidate.Manifest.release.status='active'; $candidate.Manifest.release.signing.signature_verified=$true
        Invoke-PrivateReleaseStaging $candidate $endpoint 'register' ('windows-x64-beta-0.2.0.1-' + [guid]::NewGuid().ToString('D'))
    }
    $transaction = Invoke-PrivateReleaseStaging $candidate $endpoint 'publish' ('windows-x64-beta-0.2.0.1-' + [guid]::NewGuid().ToString('D'))
    Check 'active transaction always validates before publish' ($script:transportCalls.Count -eq 5 -and
        $script:transportCalls[3].command.Contains(' validate ') -and $script:transportCalls[4].command.Contains(' publish '))
} finally { Set-Item Function:\Invoke-ReleaseProcess -Value $originalInvoker }
$backend = Join-Path $repo '.artifacts\backend-reference\2f67ddc\scripts\beta_release_publish.py'
if (-not (Test-Path -LiteralPath $backend)) {
    python (Join-Path $repo 'tests\verify_backend_manifest.py') $backend $bundle --export-from (Join-Path $repo '..\WordSuggestorBackend')
    if ($LASTEXITCODE -ne 0) { throw 'Pinned backend source is required; see Release.md for offline export.' }
}
if (Test-Path -LiteralPath $backend) {
    python (Join-Path $repo 'tests\verify_backend_manifest.py') $backend $bundle
    if ($LASTEXITCODE -ne 0) { throw 'Pinned backend contract validation failed.' }
    $script:passed++
} else { throw 'Pinned backend contract reference is required.' }
# New local preparation never weakens the production trust gate or installs by default.
$shell=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$myBefore=@(Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue).Count
$trustBefore=@(Get-ChildItem Cert:\LocalMachine\TrustedPeople -ErrorAction SilentlyContinue).Count
$dry=Invoke-ReleaseProcess $shell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'prepare_msix_test.ps1'),'-WhatIf') 15
Check 'local preparation WhatIf succeeds without provisioning' ($dry.ExitCode -eq 0)
Check 'WhatIf leaves signer and device trust stores unchanged' (@(Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue).Count -eq $myBefore -and @(Get-ChildItem Cert:\LocalMachine\TrustedPeople -ErrorAction SilentlyContinue).Count -eq $trustBefore)
$blocked=Invoke-ReleaseProcess $shell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-SkipBuild') 15
Check 'native installation defaults to BLOCKED without mutation' ($blocked.ExitCode -eq 3)
foreach($extra in @(@('-PrepareUntrustedLocalTest'),@('-PrepareUntrustedLocalTest','-LocalTestIdentity','-ReleaseStatus','active'))) {
 $rejected=Invoke-ReleaseProcess $shell (@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'package_app.ps1'))+$extra) 15
 Check 'untrusted preparation cannot become production or active' ($rejected.ExitCode -ne 0)
}
foreach($script in @('package_app.ps1','prepare_msix_test.ps1','test_msix.ps1')) {
 $tokens=$null;$parseErrors=$null;$null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $script),[ref]$tokens,[ref]$parseErrors)
 Check ($script+' syntax') ($parseErrors.Count -eq 0)
}
$probePath=Join-Path $root ('qualification-'+[guid]::NewGuid().ToString('N')+'.json')
$probe=[ordered]@{schema_version=1;ok=$true;package_name='WordSuggestor.Windows.LocalTest.0123456789ab';package_version='0.2.0.1';error_category=$null}
function Save-Probe { [IO.File]::WriteAllText($probePath,($probe|ConvertTo-Json -Compress)) }
Save-Probe
Check 'installed Core receipt binds exact schema identity version and success' ((Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version).ok)
Reject 'installed Core receipt rejects wrong identity' {Wait-ReleaseProbeResult $probePath 'Other.App' '0.2.0.1'}
Reject 'installed Core receipt rejects wrong version' {Wait-ReleaseProbeResult $probePath $probe.package_name '0.2.0.2'}
$probe.ok=$false;Save-Probe
Reject 'negative Core result cannot qualify activation' {Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version}
$probe.ok=$true;$probe.schema_version='1';Save-Probe
Reject 'probe schema version must be numeric' {Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version}
$probe.schema_version=1;$probe.error_category='SyntheticFailure';Save-Probe
Reject 'success cannot carry an error' {Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version}
$probe.error_category=$null;$probe.content='PRIVATE_DOCUMENT';Save-Probe
Reject 'probe does not admit content-bearing fields' {Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version}
$probe.Remove('content');[IO.File]::WriteAllText($probePath,'{invalid')
Reject 'malformed probe result rejected' {Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version}
[IO.File]::WriteAllText($probePath,('x'*16385))
Reject 'probe output bounded' {Wait-ReleaseProbeResult $probePath $probe.package_name $probe.package_version}
Reject 'unreserved result filename rejected' {Wait-ReleaseProbeResult (Join-Path $root 'settings.json') $probe.package_name $probe.package_version}
$missing=Join-Path $root ('qualification-'+[guid]::NewGuid().ToString('N')+'.json')
Reject 'activation without fresh receipt times out' {Wait-ReleaseProbeResult $missing $probe.package_name $probe.package_version 1}
$delayed=Join-Path $root ('qualification-'+[guid]::NewGuid().ToString('N')+'.json')
$writerPath=Join-Path $root 'delayed-probe.ps1'
$writer='param([string]$Target) Start-Sleep -Milliseconds 300; [IO.File]::WriteAllText($Target,''' + ($probe|ConvertTo-Json -Compress) + ''')'
[IO.File]::WriteAllText($writerPath,$writer)
$writerProcess=Start-Process -FilePath $powershell -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$writerPath+'"'),'-Target',('"'+$delayed+'"')) -WindowStyle Hidden -PassThru
try {
    Check 'activation waits for delayed positive Core receipt' ((Wait-ReleaseProbeResult $delayed $probe.package_name $probe.package_version 5).ok)
    if(-not $writerProcess.WaitForExit(5000)){throw 'Synthetic receipt writer did not exit.'}
} finally {if(-not $writerProcess.HasExited){$writerProcess.Kill()};$writerProcess.Dispose()}
$manualOnly=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-SkipAutomaticUpdateProbe','-PreviousBundleDirectory',$bundle,'-CandidateBundleDirectory',$bundle) 15
Check 'manual-only qualification cannot apply to a production identity' ($manualOnly.ExitCode -ne 0 -and ($manualOnly.Output+$manualOnly.Error).Contains('restricted to isolated LocalTest fixtures'))
$oldNonce=[guid]::NewGuid().ToString('N').Substring(0,12)
$oldSession=Join-Path $repo ('.artifacts\msix-tests\prepared-'+$oldNonce)
New-Item -ItemType Directory -Path $oldSession | Out-Null
[IO.File]::WriteAllText((Join-Path $oldSession 'prepared-session.json'),(@{schema_version=1;package_name=('WordSuggestor.Windows.LocalTest.'+$oldNonce);trust_changed=$false;installed=$false;key_removed=$true}|ConvertTo-Json))
$oldRejected=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-PreparedLocalTestSession',$oldSession,'-SkipBuild') 15
Check 'old three-package session is rejected before trust or installation' ($oldRejected.ExitCode -ne 0 -and ($oldRejected.Output+$oldRejected.Error).Contains('newer negative-test bundle is required'))
$missingNegative=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-PreviousBundleDirectory',$bundle,'-CandidateBundleDirectory',$bundle) 15
Check 'explicit bundle qualification requires newer negative fixture before mutation' ($missingNegative.ExitCode -ne 0 -and ($missingNegative.Output+$missingNegative.Error).Contains('higher-version negative-test bundle is required'))
$rejectingVerifier=Join-Path $root 'rejecting-verifier.ps1'
[IO.File]::WriteAllText($rejectingVerifier,'[Console]::Error.WriteLine("SYNTHETIC_SIGNATURE_REJECTION"); exit 1')
$verificationRejected=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',$rejectingVerifier) 15
Check 'expected verifier stderr preserves nonzero status without a PowerShell stop' ($verificationRejected.ExitCode -eq 1 -and $verificationRejected.Error.Contains('SYNTHETIC_SIGNATURE_REJECTION'))
$guiSkip=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-SkipInstalledGuiProbe','-PreviousBundleDirectory',$bundle,'-CandidateBundleDirectory',$bundle) 15
Check 'GUI-skipping qualification is rejected for production fixtures' ($guiSkip.ExitCode -ne 0 -and ($guiSkip.Output+$guiSkip.Error).Contains('Skipping installed GUI qualification is restricted'))
# Runtime integrity is mandatory for newly built and uploaded packages. The
# legacy fixture intentionally remains unsigned; these are structural checks.
$runtimeManifest=[IO.File]::ReadAllText((Join-Path $repo 'packaging/AppxManifest.xml'))
Assert-ReleaseIntegrityManifest (Read-ReleaseXml $runtimeManifest)
Check 'packaging template enables Windows runtime content integrity' $true
foreach($policy in @('off','default','ON')) {
    Reject ("runtime integrity $policy is rejected") {Assert-ReleaseIntegrityManifest (Read-ReleaseXml $runtimeManifest.Replace('Enforcement="on"',('Enforcement="'+$policy+'"')))}
}
Reject 'missing runtime integrity is rejected' {Assert-ReleaseIntegrityManifest (Read-ReleaseXml ([regex]::Replace($runtimeManifest,'<uap10:PackageIntegrity>.*?</uap10:PackageIntegrity>','',[Text.RegularExpressions.RegexOptions]::Singleline)))}
Reject 'wrong integrity namespace is rejected' {Assert-ReleaseIntegrityManifest (Read-ReleaseXml $runtimeManifest.Replace('/uap/windows10/10','/uap/windows10/99'))}
Reject 'duplicate runtime integrity is rejected' {Assert-ReleaseIntegrityManifest (Read-ReleaseXml $runtimeManifest.Replace('</uap10:PackageIntegrity>','</uap10:PackageIntegrity><uap10:PackageIntegrity><uap10:Content Enforcement="on" /></uap10:PackageIntegrity>'))}

# Deliberately synthetic ZIPs, not deployable MSIXs. Native trust/rejection is
# tested separately on actual SignTool-signed LocalTest packages.
$payloadDirectory=Join-Path $root 'installed-payload'
New-Item -ItemType Directory -Path (Join-Path $payloadDirectory 'data') -Force | Out-Null
$dllBytes=[byte[]](0..255)
[IO.File]::WriteAllBytes((Join-Path $payloadDirectory 'Fixture.dll'),$dllBytes)
[IO.File]::WriteAllBytes((Join-Path $payloadDirectory 'data/sample.bin'),[byte[]](9,8,7,6))
$blockMap='<BlockMap xmlns="http://schemas.microsoft.com/appx/2010/blockmap"><File Name="Fixture.dll"/><File Name="data\sample.bin"/></BlockMap>'
function New-IntegrityFixture([string]$Path,[string]$Map=$blockMap) {
    $archive=[IO.Compression.ZipFile]::Open($Path,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach($name in @('Fixture.dll','data/sample.bin','AppxSignature.p7x','AppxBlockMap.xml')) {
            $bytes=switch($name){'Fixture.dll'{$dllBytes};'data/sample.bin'{[byte[]](9,8,7,6)};'AppxSignature.p7x'{[byte[]](1..16)};default{[Text.Encoding]::UTF8.GetBytes($Map)}}
            $entry=$archive.CreateEntry($name,[IO.Compression.CompressionLevel]::NoCompression)
            $output=$entry.Open();try{$output.Write($bytes,0,$bytes.Length)}finally{$output.Dispose()}
        }
    }finally{$archive.Dispose()}
}
function Read-IntegrityEntry([string]$Path,[string]$Name) {
    $archive=[IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $input=$archive.GetEntry($Name).Open();$memory=New-Object IO.MemoryStream
        try{$input.CopyTo($memory);return [Convert]::ToBase64String($memory.ToArray())}finally{$input.Dispose();$memory.Dispose()}
    }finally{$archive.Dispose()}
}
$originalIntegrity=Join-Path $root 'integrity-original.msix'
New-IntegrityFixture $originalIntegrity
Check 'installed comparison verifies every original payload file' ((Assert-InstalledMsixPayload $originalIntegrity $payloadDirectory) -eq 2)
[IO.File]::WriteAllBytes((Join-Path $payloadDirectory 'Fixture.dll'),[byte[]](255..0))
Reject 'same-sized altered installed DLL is rejected' {Assert-InstalledMsixPayload $originalIntegrity $payloadDirectory}
[IO.File]::WriteAllBytes((Join-Path $payloadDirectory 'Fixture.dll'),$dllBytes)
[IO.File]::WriteAllBytes((Join-Path $payloadDirectory 'data/sample.bin'),[byte[]](9,8,7))
Reject 'changed installed payload size is rejected' {Assert-InstalledMsixPayload $originalIntegrity $payloadDirectory}
Remove-Item -LiteralPath (Join-Path $payloadDirectory 'data/sample.bin')
Reject 'missing installed payload file is rejected' {Assert-InstalledMsixPayload $originalIntegrity $payloadDirectory}
[IO.File]::WriteAllBytes((Join-Path $payloadDirectory 'data/sample.bin'),[byte[]](9,8,7,6))
foreach($badName in @('..\escape.bin','C:\escape.bin','Fixture.dll')) {
    $badPackage=Join-Path $root ([guid]::NewGuid().ToString('N')+'.msix')
    New-IntegrityFixture $badPackage $blockMap.Replace('data\sample.bin',$badName)
    Reject ("unsafe or duplicate block-map path rejected: $badName") {Assert-InstalledMsixPayload $badPackage $payloadDirectory}
}
$signatureCorruption=Join-Path $root 'integrity-bad-signature.msix'
New-MsixSignatureCorruption $originalIntegrity $signatureCorruption
Check 'signature corruption leaves original payload intact' ((Read-IntegrityEntry $originalIntegrity 'Fixture.dll') -ceq (Read-IntegrityEntry $signatureCorruption 'Fixture.dll'))
Check 'signature corruption actually changes signature footprint' ((Read-IntegrityEntry $originalIntegrity 'AppxSignature.p7x') -cne (Read-IntegrityEntry $signatureCorruption 'AppxSignature.p7x'))
Reject 'signature fixture never overwrites an existing artifact' {New-MsixSignatureCorruption $originalIntegrity $signatureCorruption}
$payloadCorruption=Join-Path $root 'integrity-bad-payload.msix'
$payloadMutation=New-MsixPayloadCorruption $originalIntegrity $payloadCorruption
Check 'payload corruption targets compressed DLL data' ($payloadMutation.entry -ceq 'Fixture.dll' -and $payloadMutation.offset -eq (30+11+53))
Check 'payload corruption retains the signed footprint and block map' ((Read-IntegrityEntry $originalIntegrity 'AppxSignature.p7x') -ceq (Read-IntegrityEntry $payloadCorruption 'AppxSignature.p7x') -and (Read-IntegrityEntry $originalIntegrity 'AppxBlockMap.xml') -ceq (Read-IntegrityEntry $payloadCorruption 'AppxBlockMap.xml'))
Check 'payload mutation changes archive bytes' ((Get-FileHash -LiteralPath $originalIntegrity).Hash -cne (Get-FileHash -LiteralPath $payloadCorruption).Hash)
Reject 'payload fixture never overwrites an existing artifact' {New-MsixPayloadCorruption $originalIntegrity $payloadCorruption}
$clearDesktopJson='{"schema_version":1,"visible_installer_windows":0,"installer_foreground":false,"foreground_process_id":400}'
$clearDesktop=Read-MsixDesktopResult $clearDesktopJson
$blockedDesktop=Read-MsixDesktopResult '{"schema_version":1,"visible_installer_windows":1,"installer_foreground":false,"foreground_process_id":400}'
Check 'desktop probe parses fixed metadata without content' ($clearDesktop.visible_installer_windows -eq 0)
foreach($bad in @($clearDesktopJson.Replace('"schema_version":1','"schema_version":"1"'),
    $clearDesktopJson.Replace('"visible_installer_windows":0','"visible_installer_windows":-1'),
    $clearDesktopJson.Replace('"visible_installer_windows":0','"visible_installer_windows":257'),
    $clearDesktopJson.Replace('"installer_foreground":false','"installer_foreground":true'),
    $clearDesktopJson.Replace('"installer_foreground":false','"installer_foreground":"false"'),
    $clearDesktopJson.Replace('}',',"document":"PRIVATE_TEXT"}'))){
    Reject 'invalid or content-bearing desktop result rejected' {Read-MsixDesktopResult $bad}
}
Reject 'desktop probe output bounded' {Read-MsixDesktopResult ('x'*4097)}
Check 'unattended version observation can qualify automatic update' ((Get-MsixAutomaticDecision '0.2.0.2' '0.2.0.2' $clearDesktop) -ceq 'updated')
Check 'old version cannot qualify automatic update' ((Get-MsixAutomaticDecision '0.2.0.1' '0.2.0.2' $clearDesktop) -ceq 'pending')
Check 'dialog with new version never becomes automatic PASS' ((Get-MsixAutomaticDecision '0.2.0.2' '0.2.0.2' $blockedDesktop) -ceq 'interactive_dialog')
Check 'background installer blocks even when not foreground' ((Get-MsixAutomaticDecision '0.2.0.1' '0.2.0.2' $blockedDesktop) -ceq 'interactive_dialog')
$idleRecord=[ordered]@{}
Wait-MsixInstallerIdle { $clearDesktop } $idleRecord 'clear' 2
Check 'clear desktop records stable idle without operator action' ($idleRecord.installer_desktop.clear.idle -and -not $idleRecord.installer_desktop.clear.dialog_observed)
$script:desktopSampleCount=0
Wait-MsixInstallerIdle { $script:desktopSampleCount++;if($script:desktopSampleCount -le 2){$blockedDesktop}else{$clearDesktop} } $idleRecord 'operator_closed' 3
Check 'dialog closure resumes only after stable idle and records intervention' ($idleRecord.installer_desktop.operator_closed.idle -and $idleRecord.installer_desktop.operator_closed.dialog_observed)
$lateRecord=[ordered]@{checks=[ordered]@{automatic_update=$true}}
$script:desktopSampleCount=0
Wait-MsixInstallerIdle { $script:desktopSampleCount++;if($script:desktopSampleCount -le 2){$blockedDesktop}else{$clearDesktop} } $lateRecord 'after_manual_update' 3
Check 'late installer dialog revokes earlier unattended qualification' (-not $lateRecord.checks.automatic_update -and $lateRecord.automatic_update_not_completed_reason -ceq 'interactive_app_installer' -and $lateRecord.installer_desktop.after_manual_update.idle)
$timeoutRecord=[ordered]@{}
Reject 'dialog timeout stops before subsequent phase' {Wait-MsixInstallerIdle { $blockedDesktop } $timeoutRecord 'before_gui' 1}
Check 'timed-out phase is explicit and never recorded idle' ($timeoutRecord.desktop_blocked_phase -ceq 'before_gui' -and -not $timeoutRecord.installer_desktop.before_gui.idle)
$tokens=$null;$syntaxErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'msix_desktop.ps1'),[ref]$tokens,[ref]$syntaxErrors)
Check 'desktop orchestration PowerShell syntax' ($syntaxErrors.Count -eq 0)
$capabilityTemplate=Read-ReleaseXml ([IO.File]::ReadAllText((Join-Path $repo 'packaging/AppxManifest.xml')))
Check 'production template has no experimental package-management capabilities' (-not (@($capabilityTemplate.Package.Capabilities.ChildNodes)|Where-Object {$_.Name -like '*Capability' -and $_.GetAttribute('Name') -in @('packageManagement','packageQuery')}))
Reject 'capability helper rejects production even if caller sets a test flag' {Add-TestUpdatePolicyCapabilities $capabilityTemplate $true}
$capabilityTemplate.Package.Identity.Name='WordSuggestor.Windows.LocalTest.123456abcdef'
$capabilityTemplate.Package.Identity.Publisher='CN=WordSuggestor Local Test 123456abcdef'
Reject 'capability helper requires explicit local-test context' {Add-TestUpdatePolicyCapabilities $capabilityTemplate $false}
Add-TestUpdatePolicyCapabilities $capabilityTemplate $true
Check 'test capability variant declares precisely the two documented capabilities' (@($capabilityTemplate.Package.Capabilities.ChildNodes | Where-Object {$_.GetAttribute('Name') -in @('packageManagement','packageQuery')}).Count -eq 2)
$capabilityTemplate.Package.Identity.Publisher='CN=Wrong'
Reject 'test capability helper rejects publisher mismatch' {Add-TestUpdatePolicyCapabilities $capabilityTemplate $true}
$capGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'package_app.ps1'),'-TestUpdatePolicyCapabilities') 20
Check 'public packaging rejects capability experiment before publishing' ($capGuard.ExitCode -ne 0 -and ($capGuard.Output+$capGuard.Error).Contains('require LocalTestIdentity'))
$diagGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-UpdatePolicyDiagnostic') 20
Check 'policy diagnostic requires a prepared isolated experiment before trust' ($diagGuard.ExitCode -ne 0 -and ($diagGuard.Output+$diagGuard.Error).Contains('requires prepared isolated capability-experiment'))
. (Join-Path $PSScriptRoot 'settings_qualification.ps1')
$scheduleProof=[pscustomobject]@{steps=@([pscustomobject]@{enabled=$false;verified=$true;code='saved';stage='verify_after';hresult=$null},[pscustomobject]@{enabled=$true;verified=$true;code='saved';stage='verify_after';hresult=$null})}
Check 'native qualification requires verified off and on modes' (Test-SettingsPolicyScheduleEvidence $scheduleProof)
$scheduleProof.steps[1].enabled=$false
Check 'repeating one mode cannot qualify both modes' (-not (Test-SettingsPolicyScheduleEvidence $scheduleProof))
$scheduleProof.steps[1].enabled=$true;$scheduleProof.steps[1].verified=$false
Check 'a saved code without actual native verification cannot qualify' (-not (Test-SettingsPolicyScheduleEvidence $scheduleProof))
$scheduleProof.steps=@($scheduleProof.steps[0])
Check 'legacy single-transition evidence cannot qualify off/on' (-not (Test-SettingsPolicyScheduleEvidence $scheduleProof))
$experimentNonce=[guid]::NewGuid().ToString('N').Substring(0,12)
$experimentRoot=Join-Path $repo ('.artifacts\msix-tests\prepared-'+$experimentNonce)
New-Item -ItemType Directory -Path $experimentRoot | Out-Null
[IO.File]::WriteAllText((Join-Path $experimentRoot 'prepared-session.json'),(@{schema_version=1;package_name=('WordSuggestor.Windows.LocalTest.'+$experimentNonce);trust_changed=$false;installed=$false;key_removed=$true;negative_bundle=$bundle;update_policy_capability_experiment=$true}|ConvertTo-Json))
$experimentGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-PreparedLocalTestSession',$experimentRoot) 20
Check 'experimental capability receipts cannot enter default native qualification' ($experimentGuard.ExitCode -ne 0 -and ($experimentGuard.Output+$experimentGuard.Error).Contains('require UpdatePolicyDiagnostic'))
$nativeControlGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'package_app.ps1'),'-TestUpdatePolicyNativeControlPath','fixture.exe') 20
Check 'native C++ control cannot enter production packaging' ($nativeControlGuard.ExitCode -ne 0 -and ($nativeControlGuard.Output+$nativeControlGuard.Error).Contains('Native policy controls require LocalTestIdentity'))
$nativePrepareGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'prepare_msix_test.ps1'),'-UpdatePolicyNativeControlPath','fixture.exe') 20
Check 'native C++ control preparation requires isolated diagnostic context' ($nativePrepareGuard.ExitCode -ne 0 -and ($nativePrepareGuard.Output+$nativePrepareGuard.Error).Contains('requires the isolated capability experiment'))
$intervalGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-UpdatePolicyDiagnosticHours','6') 20
Check 'diagnostic interval cannot alter the normal release qualification flow' ($intervalGuard.ExitCode -ne 0 -and ($intervalGuard.Output+$intervalGuard.Error).Contains('require UpdatePolicyDiagnostic'))
$mockNames=@('Get-AppxPackage','Get-AppxPackageAutoUpdateSettings','Set-AppxPackageAutoUpdateSettings','Test-SettingsProcessElevated')
$savedFunctions=@{}
foreach($name in $mockNames){$existing=Get-Item -LiteralPath ('Function:\'+$name) -ErrorAction SilentlyContinue;$savedFunctions[$name]=if($existing){$existing.ScriptBlock}else{$null}}
try {
    Set-Item Function:\Get-AppxPackage { [pscustomobject]@{Name='WordSuggestor.Windows.LocalTest.123456abcdef';Publisher='CN=WordSuggestor Local Test 123456abcdef';PackageFamilyName='WordSuggestor.Windows.LocalTest.123456abcdef_123456789abcd'} }
    Set-Item Function:\Test-SettingsProcessElevated { return $false }
    Set-Item Function:\Get-AppxPackageAutoUpdateSettings { $script:zeroState | Select-Object * }
    Set-Item Function:\Set-AppxPackageAutoUpdateSettings {
        [CmdletBinding()]param($PackageFamilyName,$EnableAutomaticBackgroundTask,$CheckOnLaunch,$HoursBetweenUpdateChecks,$Version,$AppInstallerUri)
        if($HoursBetweenUpdateChecks -eq 0 -and $script:rejectZero){
            if($script:dirtyZeroFlags){$script:zeroState.CheckOnLaunch=$false}
            throw [Runtime.InteropServices.COMException]::new('PRIVATE_NATIVE_ERROR',[int]0x800706f7)
        }
        $script:zeroState.HoursBetweenUpdateChecks=$HoursBetweenUpdateChecks
        $script:zeroState.EnableAutomaticBackgroundTask=$EnableAutomaticBackgroundTask
        $script:zeroState.CheckOnLaunch=$CheckOnLaunch
    }
    foreach($rejectZero in @($true,$false)){
        $script:dirtyZeroFlags=$false
        $script:rejectZero=$rejectZero
        $script:zeroState=[pscustomobject]@{HoursBetweenUpdateChecks=6;EnableAutomaticBackgroundTask=$true;CheckOnLaunch=$true;Version='0.2.0.1';AppInstallerUri='file:///fixture.appinstaller'}
        $zeroResult=Get-SettingsZeroIntervalControl 'WordSuggestor.Windows.LocalTest.123456abcdef_123456789abcd'
        Check 'zero-interval experiment preserves original policy on rejection or acceptance' ($zeroResult.accepted -eq (-not $rejectZero) -and $zeroResult.restored -and $script:zeroState.HoursBetweenUpdateChecks -eq 6 -and ($rejectZero -eq ($zeroResult.hresult -ceq '0x800706F7')))
    }
    $script:rejectZero=$true;$script:dirtyZeroFlags=$true
    $zeroResult=Get-SettingsZeroIntervalControl 'WordSuggestor.Windows.LocalTest.123456abcdef_123456789abcd'
    Check 'zero-interval rejection restores scheduling-only side effects' (-not $zeroResult.accepted -and $zeroResult.restored -and $script:zeroState.CheckOnLaunch -and $script:zeroState.EnableAutomaticBackgroundTask -and $script:zeroState.HoursBetweenUpdateChecks -eq 6)
}finally{
    foreach($name in $mockNames){if($savedFunctions[$name]){Set-Item -LiteralPath ('Function:\'+$name) -Value $savedFunctions[$name]}else{Remove-Item -LiteralPath ('Function:\'+$name) -ErrorAction SilentlyContinue}}
}
$nativeBuildGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'prepare_msix_test.ps1'),'-BuildUpdatePolicyNativeControl') 20
Check 'automatic C++ build also requires isolated diagnostic context' ($nativeBuildGuard.ExitCode -ne 0 -and ($nativeBuildGuard.Output+$nativeBuildGuard.Error).Contains('requires the isolated capability experiment'))
$schemaGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-UpdatePolicyDiagnosticSchema','2021') 20
Check 'diagnostic schema cannot alter the normal release flow' ($schemaGuard.ExitCode -ne 0 -and ($schemaGuard.Output+$schemaGuard.Error).Contains('require UpdatePolicyDiagnostic'))
$settingsUiGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-UpdatePolicySettingsUiProbe') 20
Check 'Settings UI probe requires isolated diagnostic context before launch' ($settingsUiGuard.ExitCode -ne 0 -and ($settingsUiGuard.Output+$settingsUiGuard.Error).Contains('require UpdatePolicyDiagnostic'))
Reject 'Settings UI probe rejects production target before launch' {Get-SettingsUpdateUiEvidence 'WordSuggestor.Windows.Beta_fixture'}
$comparisonGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-CompareUpdatePolicySchemas') 20
Check 'cross-schema comparison cannot run outside the isolated diagnostic' ($comparisonGuard.ExitCode -ne 0 -and ($comparisonGuard.Output+$comparisonGuard.Error).Contains('require UpdatePolicyDiagnostic'))
$displayDocument=Read-ReleaseXml ([IO.File]::ReadAllText((Join-Path $repo 'packaging/AppxManifest.xml')))
Reject 'diagnostic display helper rejects production identity' {Set-TestUpdatePolicyDisplayName $displayDocument $true}
$displayDocument.Package.Identity.Name='WordSuggestor.Windows.LocalTest.123456abcdef'
$displayDocument.Package.Identity.Publisher='CN=WordSuggestor Local Test 123456abcdef'
Reject 'diagnostic display helper requires explicit LocalTest context' {Set-TestUpdatePolicyDisplayName $displayDocument $false}
$identityBefore=$displayDocument.Package.Identity.OuterXml
Set-TestUpdatePolicyDisplayName $displayDocument $true
Check 'diagnostic package shows its unique nonce in app and publisher labels' ($displayDocument.Package.Properties.DisplayName -ceq 'WordSuggestor LocalTest 123456abcdef' -and $displayDocument.Package.Properties.PublisherDisplayName -ceq 'WordSuggestor LocalTest 123456abcdef' -and $displayDocument.SelectSingleNode('//*[local-name()="VisualElements"]').GetAttribute('DisplayName') -ceq 'WordSuggestor LocalTest 123456abcdef')
Check 'diagnostic display labels do not alter the signed identity' ($displayDocument.Package.Identity.OuterXml -ceq $identityBefore)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$targetNode=[pscustomobject]@{Current=[pscustomobject]@{Name='WordSuggestor LocalTest 123456abcdef';AutomationId='Target'}}
$targetNode | Add-Member ScriptMethod GetCurrentPropertyValue {param($property) return $false}
$toggleNode=[pscustomobject]@{Current=[pscustomobject]@{Name='Automatically update this app';AutomationId='AutoUpdate'}}
$toggleNode | Add-Member ScriptMethod GetCurrentPropertyValue {param($property) return $true}
$staleNode=[pscustomobject]@{}
$staleNode | Add-Member ScriptProperty Current {throw [Runtime.InteropServices.COMException]::new('PRIVATE_UI_TEXT',[int]0x80040201)}
$uiEvidence=Get-SettingsUiNodeEvidence @($staleNode,$targetNode,$toggleNode) '123456abcdef'
Check 'one stale UI node does not discard verified target and update control' ($uiEvidence.page_target_verified -and $uiEvidence.update_toggle_found -and $uiEvidence.node_read_errors -eq 1 -and $uiEvidence.toggle_count -eq 1)
$wrongUi=Get-SettingsUiNodeEvidence @($targetNode,$toggleNode) 'abcdef123456'
Check 'unverified Settings page exposes no control IDs or counts' (-not $wrongUi.page_target_verified -and -not $wrongUi.update_toggle_found -and $wrongUi.toggle_count -eq 0 -and @($wrongUi.toggle_ids).Count -eq 0)
Check 'UI diagnostics omit node captions and exception messages' (-not ($uiEvidence|ConvertTo-Json -Depth 4).Contains('PRIVATE_UI_TEXT') -and -not ($uiEvidence|ConvertTo-Json -Depth 4).Contains('Automatically update'))
$uiFailure=Get-SettingsUiFailure 'read_nodes' ([InvalidOperationException]::new('PRIVATE_WRAPPER',[Runtime.InteropServices.COMException]::new('PRIVATE_INNER',[int]0x80004005)))
Check 'UI failure reports fixed phase and underlying HRESULT only' ($uiFailure.error_stage -ceq 'read_nodes' -and $uiFailure.hresult -ceq '0x80004005' -and -not ($uiFailure|ConvertTo-Json).Contains('PRIVATE'))
$uiOnlyGuard=Invoke-ReleaseProcess $powershell @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'test_msix.ps1'),'-UpdatePolicySettingsUiOnly') 20
Check 'Settings UI only mode cannot bypass isolated diagnostic context' ($uiOnlyGuard.ExitCode -ne 0 -and ($uiOnlyGuard.Output+$uiOnlyGuard.Error).Contains('require UpdatePolicyDiagnostic'))
Write-Output "PASS RELEASE $script:passed checks. Synthetic artifacts: $root"
