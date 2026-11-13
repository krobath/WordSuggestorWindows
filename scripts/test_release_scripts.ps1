$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release_common.ps1')
. (Join-Path $PSScriptRoot 'release_transport.ps1')
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
    try { $writer.Write('<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"><Identity Name="WordSuggestor.Windows.Beta" Publisher="CN=Fixture" Version="0.2.0.1" ProcessorArchitecture="x64" /></Package>') }
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
Write-Output "PASS RELEASE $script:passed checks. Synthetic artifacts: $root"
