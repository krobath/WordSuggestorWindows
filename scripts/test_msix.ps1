param(
    [string]$PreviousBundleDirectory,
    [string]$CandidateBundleDirectory,
    [string]$RollbackBundleDirectory,
    [string]$PreparedLocalTestSession,
    [switch]$LocalTestIdentity,
    [string]$CoreCliPath,
    [switch]$SkipBuild,
    [switch]$SkipAutomaticUpdateProbe,
    [switch]$AllowTemporaryMachineTrust
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
. (Join-Path $PSScriptRoot 'release_common.ps1')
. (Join-Path $PSScriptRoot 'release_transport.ps1')
. (Join-Path $PSScriptRoot 'app_process.ps1')
if ($PreparedLocalTestSession) {
    if($PreviousBundleDirectory -or $CandidateBundleDirectory -or $RollbackBundleDirectory){throw 'Prepared session and explicit bundles are mutually exclusive.'}
    $preparedRoot=[IO.Path]::GetFullPath($PreparedLocalTestSession)
    $allowed=[IO.Path]::GetFullPath((Join-Path $windowsRepo '.artifacts\msix-tests'))+'\'
    if(-not $preparedRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($preparedRoot) -cnotmatch '^prepared-[a-f0-9]{12}$'){throw 'Prepared session must be an owned local-test directory.'}
    $prepared=([IO.File]::ReadAllText((Join-Path $preparedRoot 'prepared-session.json'))|ConvertFrom-Json)
    if($prepared.schema_version -ne 1 -or $prepared.package_name -cne ('WordSuggestor.Windows.LocalTest.'+[IO.Path]::GetFileName($preparedRoot).Substring(9)) -or $prepared.trust_changed -ne $false -or $prepared.installed -ne $false -or $prepared.key_removed -ne $true){throw 'Prepared session is not safe for qualification.'}
    foreach($path in @($prepared.previous_bundle,$prepared.candidate_bundle,$prepared.rollback_bundle)) {
        if(-not $path -or -not ([IO.Path]::GetFullPath($path)).StartsWith(($windowsRepo+'\.artifacts\release\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Prepared bundle is outside release artifacts.'}
        $input=Test-ReleaseBundle $path -LocalTest
        if($input.Identity.Name -cne $prepared.package_name -or $input.Identity.Publisher -cne $prepared.publisher){throw 'Prepared identity/publisher mismatch.'}
    }
    $PreviousBundleDirectory=$prepared.previous_bundle;$CandidateBundleDirectory=$prepared.candidate_bundle;$RollbackBundleDirectory=$prepared.rollback_bundle
    $LocalTestIdentity=$true
}
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run qualification as an ordinary user; only the test leaf-trust helper elevates.' }
if ([bool]$PreviousBundleDirectory -ne [bool]$CandidateBundleDirectory) { throw 'Provide both production bundles, or neither for isolated local testing.' }
$providedBundles = [bool]$CandidateBundleDirectory
$localTest = $LocalTestIdentity -or -not $providedBundles
if($SkipAutomaticUpdateProbe -and -not $localTest){throw 'Skipping automatic qualification is restricted to isolated LocalTest fixtures.'}
if ($RollbackBundleDirectory -and -not $localTest) { throw 'Synthetic rollback/draft qualification requires an isolated LocalTest identity.' }
if ($localTest -and -not $AllowTemporaryMachineTrust) {
    Write-Warning 'BLOCKED MSIX: isolated local qualification changes machine TrustedPeople. Review the runbook and explicitly pass -AllowTemporaryMachineTrust in a disposable test environment; no certificate or package was changed.'
    exit 3
}
if (Get-Process -Name WordSuggestorWindows.App -ErrorAction SilentlyContinue) { throw 'Close other WordSuggestor instances before isolated installation qualification.' }
$id = [guid]::NewGuid().ToString('N')
$root = Join-Path $windowsRepo ('.artifacts\msix-tests\' + $id)
$feedRoot = Join-Path $root 'feed'
New-Item -ItemType Directory -Path $feedRoot -Force | Out-Null
$data = if ($localTest) { Join-Path $env:LOCALAPPDATA ('WordSuggestor\LocalTests\' + $id.Substring(0,12)) }
    else { Join-Path $env:LOCALAPPDATA 'WordSuggestor' }
if (-not $localTest -and (Test-Path -LiteralPath $data) -and @(Get-ChildItem -LiteralPath $data -Force).Count) {
    throw 'Production-identity qualification requires a clean account/VM; existing WordSuggestor user data is preserved.'
}
New-Item -ItemType Directory -Path $data -Force | Out-Null
$checks = [ordered]@{}
$record = [ordered]@{ schema_version=1; local_test_identity=$localTest; started_at=[DateTimeOffset]::UtcNow.ToString('o'); checks=$checks }
$cert = $null; $trustProcess = $null; $ownedPackageName = $null; $installedByThisRun = $false; $finished = $false
$environmentBefore = @{}
foreach ($name in @('WORDSUGGESTOR_DATA_ROOT','WORDSUGGESTOR_EXPECT_PACKAGE_VERSION','WORDSUGGESTOR_WINDOWS_STARTUP_TEXT','WORDSUGGESTOR_SUGGEST_CLI_PATH','WORDSUGGESTOR_TEST_DATA_ROOT','WORDSUGGESTOR_TEST_APP_ID','WORDSUGGESTOR_TEST_APP_PATH')) {
    $environmentBefore[$name] = [Environment]::GetEnvironmentVariable($name,'Process')
}
function Pass([string]$Name) { $checks[$Name] = $true; Write-Output "PASS MSIX $Name" }
function Installed { return Get-AppxPackage -Name $ownedPackageName }
function Wait-Version([string]$Version, [int]$Seconds = 30) {
    $limit = [DateTime]::UtcNow.AddSeconds($Seconds)
    do { $installed = Installed; if ($installed -and $installed.Version -eq $Version) { return $installed }; Start-Sleep -Milliseconds 500 }
    while ([DateTime]::UtcNow -lt $limit)
    throw 'Installed MSIX version did not reach the expected version.'
}
function Build-LocalBundle([string]$Version) {
    $built = $null
    & (Join-Path $PSScriptRoot 'package_app.ps1') -PackageVersion $Version -DisplayVersion ('0.2.0-local.' + $Version.Split('.')[-1]) `
        -CertificateThumbprint $cert.Thumbprint -Publisher $cert.Subject -LocalTestIdentity -TestPackageName $ownedPackageName `
        -LocalTestFeedDirectory $feedRoot -SkipCoreBuild -CoreCliPath $CoreCliPath |
        ForEach-Object { if ($_ -and $_.PSObject.Properties['BundleDirectory']) { $built = $_ } else { Write-Output $_ } }
    if (-not $built) { throw 'Package build did not return its bundle.' }
    return $built.BundleDirectory
}
function Make-TestFeed($Bundle) {
    $r = $Bundle.Manifest.release
    Copy-Item -LiteralPath (Join-Path $Bundle.Directory $r.artifact.filename) -Destination $feedRoot
    $xml = Read-ReleaseXml ([IO.File]::ReadAllText((Join-Path $Bundle.Directory $r.update_feed.filename)))
    $xml.AppInstaller.Uri = ([uri](Join-Path $feedRoot 'WordSuggestor.Beta.appinstaller')).AbsoluteUri
    $xml.AppInstaller.MainPackage.Uri = ([uri](Join-Path $feedRoot $r.artifact.filename)).AbsoluteUri
    $xml.AppInstaller.UpdateSettings.OnLaunch.HoursBetweenUpdateChecks = '0'
    $xml.Save((Join-Path $feedRoot 'WordSuggestor.Beta.appinstaller'))
}
function Probe-InstalledData($Package, [string]$Mode) {
    $resultPath = Join-Path $data ('diagnostics\qualification-' + [guid]::NewGuid().ToString('N') + '.json')
    if(Test-Path -LiteralPath $resultPath){throw 'Probe result must be fresh.'}
    $arguments = '--qualification-probe ' + $Mode + ' --qualification-root "' + $data + '" --qualification-result "' + $resultPath + '"'
    $process = Invoke-ReleaseProcess $runner @('--activate-app',($Package.PackageFamilyName + '!App'),$arguments) 30
    $record.probe_exit_code=$process.ExitCode
    $record.probe_result_present=Test-Path -LiteralPath $resultPath
    # This runner mode emits fixed activation metadata only, never app/user text.
    $detail=($process.Output+"`n"+$process.Error)
    if($detail -match 'FAIL package activation: ([A-Za-z0-9_.]+) (0x[A-F0-9]{8})') {
        $record.activation_error_category=$Matches[1];$record.activation_hresult=$Matches[2]
    }
    if ($process.ExitCode -ne 0) {
        throw 'Installed package activation/Core probe failed.'
    }
    $null=Wait-ReleaseProbeResult $resultPath $Package.Name ([string]$Package.Version)
    $record.probe_result_present=$true
    Write-Output 'PASS MSIX packaged Core/DA and user-word probe'
}
function Close-OwnedTestInstances {
    foreach ($auto in @(Get-Process -Name WordSuggestorWindows.App -ErrorAction SilentlyContinue)) {
        if($auto.HasExited){continue}
        $executable=$auto.MainModule.FileName
        $directoryName=[IO.Path]::GetFileName([IO.Path]::GetDirectoryName($executable))
        if(-not $directoryName.StartsWith(($ownedPackageName+'_'),[StringComparison]::Ordinal)){continue}
        # A newly relaunched update may not have created its toolbar yet. A
        # completed probe may instead be exiting without ever showing a window.
        if($auto.WaitForExit(500)){continue}
        try {$null=$auto.WaitForInputIdle(10000)}
        catch [InvalidOperationException] {if($auto.HasExited){continue};throw}
        if(-not $auto.HasExited){Close-WordSuggestorAppGracefully $auto $executable 15}
    }
}
function Get-RetentionPayload($Bundle,$Package) {
    # Loose test processes cannot execute a packaged binary in WindowsApps.
    # Extract only the already verified signed MSIX; do not change its ACLs.
    $payload=Join-Path $root ('retention-'+$Bundle.Identity.Version)
    $makeAppx=Get-ReleaseSdkTool 'makeappx'
    $null=& $makeAppx unpack /p (Join-Path $Bundle.Directory $Bundle.Manifest.release.artifact.filename) /d $payload /o 2>&1
    if($LASTEXITCODE -ne 0){throw 'Signed retention payload extraction failed.'}
    foreach($file in @('Core\WordSuggestorSuggestCLI.exe','Packs\da_lexicon.sqlite')) {
        if((Get-FileHash -LiteralPath (Join-Path $payload $file) -Algorithm SHA256).Hash -cne
           (Get-FileHash -LiteralPath (Join-Path $Package.InstallLocation $file) -Algorithm SHA256).Hash){throw 'Retention tool differs from installed signed payload.'}
    }
    Write-Host 'PASS MSIX retention fixture Core/pack bytes match installed signed payload'
    return $payload
}
function Reject-Feed([string]$Name, [scriptblock]$Mutate) {
    $xml = Read-ReleaseXml ([IO.File]::ReadAllText((Join-Path $feedRoot 'WordSuggestor.Beta.appinstaller')))
    & $Mutate $xml
    $path = Join-Path $feedRoot ($Name + '.appinstaller')
    $xml.Save($path)
    $before = [string](Installed).Version
    $rejected = $false
    try { Add-AppxPackage -AppInstallerFile -Path $path -ErrorAction Stop } catch { $rejected = $true }
    if (-not $rejected -or [string](Installed).Version -ne $before) { throw "Windows did not reject $Name safely." }
    Pass $Name
}
try {
    if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'build_app.ps1') }
    if ($localTest) {
        $shortId = $id.Substring(0,12)
        if ($providedBundles) {
            $inputBundle = Test-ReleaseBundle $CandidateBundleDirectory -LocalTest
            $ownedPackageName = $inputBundle.Identity.Name
            $publicCert = (Get-AuthenticodeSignature -LiteralPath (Join-Path $inputBundle.Directory $inputBundle.Manifest.release.artifact.filename)).SignerCertificate
            if (-not $publicCert -or $publicCert.Subject -cne $inputBundle.Identity.Publisher -or
                $publicCert.Subject -cnotmatch '^CN=WordSuggestor Local Test [a-f0-9]{12}$') { throw 'Existing test bundle has no matching isolated test signer.' }
        } else {
            $ownedPackageName = 'WordSuggestor.Windows.LocalTest.' + $shortId
            $cert = New-SelfSignedCertificate -Type Custom -Subject ('CN=WordSuggestor Local Test ' + $shortId) `
                -CertStoreLocation 'Cert:\CurrentUser\My' -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 `
                -KeyExportPolicy NonExportable -KeyUsage DigitalSignature -NotAfter ([DateTime]::Now.AddDays(7)) `
                -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}CA=false')
            $publicCert = $cert
        }
        $data = Join-Path $env:LOCALAPPDATA ('WordSuggestor\LocalTests\' + $ownedPackageName.Split('.')[-1])
        if ((Test-Path -LiteralPath $data) -and @(Get-ChildItem -LiteralPath $data -Force).Count) { throw 'Existing isolated test data is preserved; use a fresh fixture identity.' }
        if (Installed) { throw 'Existing test installation is preserved; use a fresh fixture identity.' }
        New-Item -ItemType Directory -Path $data -Force | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $root 'local-test-public.cer'),$publicCert.RawData)
        $helper = Join-Path $PSScriptRoot 'local_test_trust.ps1'
        $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"' + $helper + '"'),
            '-SessionDirectory',('"' + $root + '"'),'-Thumbprint',$publicCert.Thumbprint)
        $trustProcess = Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" `
            -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -PassThru
        $limit = [DateTime]::UtcNow.AddSeconds(90)
        while (-not (Test-Path -LiteralPath (Join-Path $root 'trust-ready.json'))) {
            if ($trustProcess.HasExited -or [DateTime]::UtcNow -gt $limit) { throw 'Temporary test certificate trust was not established.' }
            Start-Sleep -Milliseconds 250
        }
        if (-not $providedBundles -and -not $CoreCliPath) {
            & (Join-Path $PSScriptRoot 'bootstrap_core_cli.ps1') -Configuration release
            $CoreCliPath = $env:WORDSUGGESTOR_SUGGEST_CLI_PATH
        }
        if (-not $providedBundles) {
            $PreviousBundleDirectory = Build-LocalBundle '0.2.0.1' | Select-Object -Last 1
            $CandidateBundleDirectory = Build-LocalBundle '0.2.0.2' | Select-Object -Last 1
        }
    }
    if($PreparedLocalTestSession) {
        foreach($path in @($PreviousBundleDirectory,$CandidateBundleDirectory,$RollbackBundleDirectory)) {
            $input=Test-ReleaseBundle $path -LocalTest
            $attestation=Test-MsixSignature (Join-Path $input.Directory $input.Manifest.release.artifact.filename) $input.Identity.Publisher
            $actualSigner=(Get-AuthenticodeSignature -LiteralPath (Join-Path $input.Directory $input.Manifest.release.artifact.filename)).SignerCertificate
            if($actualSigner.Thumbprint -ine $prepared.certificate_thumbprint){throw 'Prepared signing identity changed.'}
            $input.Manifest.release.signing=$attestation
            [IO.File]::WriteAllText((Join-Path $input.Directory 'release-manifest.json'),($input.Manifest|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
        }
    }
    $previous = Test-ReleaseBundle $PreviousBundleDirectory -LocalTest:$localTest -VerifySignature
    $candidate = Test-ReleaseBundle $CandidateBundleDirectory -LocalTest:$localTest -VerifySignature
    $ownedPackageName = $candidate.Identity.Name
    if ($candidate.Identity.Name -cne $previous.Identity.Name -or $candidate.Identity.Publisher -cne $previous.Identity.Publisher -or
        [version]$candidate.Identity.Version -le [version]$previous.Identity.Version) { throw 'N and N+1 must have the same identity/publisher and increasing versions.' }
    if (Installed) { $ownedPackageName = $null; throw 'Existing user installation preserved. Use a clean test account/VM.' }
    $record.package_name = $candidate.Identity.Name; $record.publisher = $candidate.Identity.Publisher
    $record.package_version = $candidate.Identity.Version
    $record.package_sha256 = $candidate.Manifest.release.artifact.sha256
    $record.feed_sha256 = $candidate.Manifest.release.update_feed.sha256
    $record.previous_package_version = $previous.Identity.Version
    $record.bundle_directory = $candidate.Directory
    Make-TestFeed $previous
    Add-AppxPackage -AppInstallerFile -Path (Join-Path $feedRoot 'WordSuggestor.Beta.appinstaller') -ErrorAction Stop
    $installedByThisRun = $true
    $installed = Wait-Version $previous.Identity.Version
    Pass 'clean_install'; Pass 'standard_user'
    New-Item -ItemType Directory -Path (Join-Path $data 'Packs'),(Join-Path $data 'settings') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $installed.InstallLocation 'Packs\da_lexicon.sqlite') -Destination (Join-Path $data 'Packs\da_lexicon.sqlite')
    [IO.File]::WriteAllText((Join-Path $data 'settings\settings-v1.json'),'{"schemaVersion":6,"selectedLanguageCode":"da-DK","readingSpeedDelta":0.25}')
    $runner = Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe'
    Probe-InstalledData $installed 'add'
    Pass 'core_suggestions'
    Close-OwnedTestInstances
    $runner = Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe'
    if($localTest) {
        $retentionPayload=Get-RetentionPayload $previous $installed
        & $runner --retention-data seed $data $retentionPayload
        if($LASTEXITCODE -ne 0){throw 'Installed-data fixture seeding failed.'}
        Pass 'seed_extended_user_data'
    }
    $sentinels = @('settings\settings-v1.json','Packs\da_lexicon.sqlite','UserWords\local_lexicon_overlay_v2.json')
    if($localTest){$sentinels+=@('editor\workspace-v1.json','learning\word-choices-v1.json','UnknownWords\learned-v1.json','UnknownWords\sharing-v1.json','fixtures\one.txt','fixtures\two.txt')}
    $hashes = @{}
    foreach ($file in $sentinels) { $hashes[$file] = (Get-FileHash -LiteralPath (Join-Path $data $file) -Algorithm SHA256).Hash }
    Make-TestFeed $candidate
    # A real automatic check is attempted through the registered App Installer
    # association. No artificial package or feed is sent to the public server.
    $env:WORDSUGGESTOR_DATA_ROOT = $data
    $env:WORDSUGGESTOR_WINDOWS_STARTUP_TEXT = $null # The automatic-update fixture contains only clean, saved synthetic tabs.
    # Shell activation participates in App Installer's launch/update lifecycle;
    # starting a loose executable directly would not prove automatic checks.
    if(-not $SkipAutomaticUpdateProbe) {
    $null = Start-Process -FilePath "$env:SystemRoot\explorer.exe" -ArgumentList ('shell:AppsFolder\' + $installed.PackageFamilyName + '!App') -WindowStyle Hidden -PassThru
    Start-Sleep -Seconds 3
    Close-OwnedTestInstances
    try { $installed = Wait-Version $candidate.Identity.Version 60; Pass 'automatic_update' }
    catch { $checks.automatic_update = $false; Write-Output 'PENDING MSIX automatic background/on-launch update did not complete in the observation window.' }
    } else {
        $checks.automatic_update=$false
        $record.automatic_update_not_run_reason='explicit_local_manual_probe'
        Write-Output 'NOT RUN MSIX automatic update: explicit isolated manual qualification; not a complete release gate.'
    }
    Close-OwnedTestInstances
    Add-AppxPackage -AppInstallerFile -Path (Join-Path $feedRoot 'WordSuggestor.Beta.appinstaller') -ErrorAction Stop
    $installed = Wait-Version $candidate.Identity.Version
    Pass 'manual_update'; Pass 'n_to_n1'
    foreach ($file in $sentinels) {
        if ((Get-FileHash -LiteralPath (Join-Path $data $file) -Algorithm SHA256).Hash -cne $hashes[$file]) { throw 'Installed update changed a user-data sentinel.' }
    }
    Probe-InstalledData $installed 'check'
    Close-OwnedTestInstances
    Pass 'data_retention'
    if($localTest) {
        $retentionPayload=Get-RetentionPayload $candidate $installed
        & $runner --retention-data check $data $retentionPayload
        if($LASTEXITCODE -ne 0){throw 'Extended user data failed installed update readback.'}
        Pass 'extended_user_data_readback'
    }
    if($RollbackBundleDirectory) {
        $rollback=Test-ReleaseBundle $RollbackBundleDirectory -LocalTest:$localTest -VerifySignature
        if($rollback.Identity.Name -cne $candidate.Identity.Name -or $rollback.Identity.Publisher -cne $candidate.Identity.Publisher -or [version]$rollback.Identity.Version -le [version]$candidate.Identity.Version){throw 'Rollback must retain identity/publisher and increase version.'}
        # App Installer can relaunch after the earlier version observation. Finish
        # that normal lifecycle before writing an offline dirty-draft fixture.
        Close-OwnedTestInstances
        & $runner --retention-data dirty $data $retentionPayload
        if($LASTEXITCODE -ne 0){throw 'Dirty recovery fixture unavailable.'}
        $draftHash=(Get-FileHash -LiteralPath (Join-Path $data 'editor\workspace-v1.json') -Algorithm SHA256).Hash
        Make-TestFeed $rollback
        Add-AppxPackage -AppInstallerFile -Path (Join-Path $feedRoot 'WordSuggestor.Beta.appinstaller') -ErrorAction Stop
        $installed=Wait-Version $rollback.Identity.Version
        if((Get-FileHash -LiteralPath (Join-Path $data 'editor\workspace-v1.json') -Algorithm SHA256).Hash -cne $draftHash){throw 'Higher-version rollback changed dirty drafts.'}
        $retentionPayload=Get-RetentionPayload $rollback $installed
        & $runner --retention-data check-dirty $data $retentionPayload
        if($LASTEXITCODE -ne 0){throw 'Higher-version rollback data readback failed.'}
        $record.rollback_version=$rollback.Identity.Version;Pass 'higher_version_rollback';Pass 'dirty_draft_retention'
    }
    if($localTest) {
        . (Join-Path $PSScriptRoot 'settings_qualification.ps1')
        $policyCode=Invoke-SettingsGateProcess (Join-Path $PSScriptRoot 'test_settings_platform.ps1') @('-PackagedUpdateProbe','-TestPackageFamilyName',$installed.PackageFamilyName,'-RunnerPath',$runner,'-OutputDirectory',(Join-Path $root 'update-policy')) (Join-Path $root 'update-policy.log') 6>$null
        $checks.update_policy_write_read_restore=$policyCode -eq 0
        if($policyCode -notin @(0,3)){throw 'Installed native policy qualification failed.'}
        if($policyCode -eq 3){Write-Output 'BLOCKED MSIX native update-policy mutation; G05 remains read-only.'}else{Pass 'update_policy_write_read_restore'}
    }
    $env:WORDSUGGESTOR_EXPECT_PACKAGE_VERSION = [string]$installed.Version
    $env:WORDSUGGESTOR_TEST_DATA_ROOT = Join-Path $data ('GuiSmoke\' + [guid]::NewGuid().ToString('N'))
    & (Join-Path $PSScriptRoot 'test_gui.ps1') -SkipBuild -AppId ($installed.PackageFamilyName + '!App')
    Pass 'desktop_smoke'; Pass 'installed_relaunch'
    Close-OwnedTestInstances
    Reject-Feed 'wrong_identity' { param($xml) $xml.AppInstaller.MainPackage.Name = 'Other.Unrelated.App' }
    Reject-Feed 'wrong_publisher' { param($xml) $xml.AppInstaller.MainPackage.Publisher = 'CN=Unrelated Publisher' }
    Reject-Feed 'offline_feed' { param($xml) $next=([version](Installed).Version);$v=[version]::new($next.Major,$next.Minor,$next.Build,($next.Revision+1));$xml.AppInstaller.Version=$v.ToString();$xml.AppInstaller.MainPackage.Version=$v.ToString();$xml.AppInstaller.MainPackage.Uri = ([uri](Join-Path $feedRoot 'missing.msix')).AbsoluteUri }
    $badFeed = Join-Path $feedRoot 'invalid.appinstaller'
    [IO.File]::WriteAllText($badFeed,'<invalid')
    $rejected = $false
    try { Add-AppxPackage -AppInstallerFile -Path $badFeed -ErrorAction Stop } catch { $rejected = $true }
    if (-not $rejected) { throw 'Malformed App Installer accepted.' }; Pass 'invalid_feed'
    $tampered = Join-Path $root 'tampered.msix'
    $currentBundle=if($rollback){$rollback}else{$candidate}
    Copy-Item -LiteralPath (Join-Path $currentBundle.Directory $currentBundle.Manifest.release.artifact.filename) -Destination $tampered
    $stream = [IO.File]::Open($tampered,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite)
    try { $stream.Position = 100; $byte = $stream.ReadByte(); $stream.Position = 100; $stream.WriteByte([byte]($byte -bxor 1)) } finally { $stream.Dispose() }
    $signTool = Get-ReleaseSdkTool 'signtool'; $null = & $signTool verify /pa /all /v $tampered 2>&1
    if ($LASTEXITCODE -eq 0) { throw 'Tampered package passed trust verification.' }
    $rejected = $false
    try { Add-AppxPackage -Path $tampered -ErrorAction Stop } catch { $rejected = $true }
    if (-not $rejected) { throw 'Tampered MSIX accepted.' }; Pass 'tampered_package'
    $rejected = $false
    try { Add-AppxPackage -Path (Join-Path $previous.Directory $previous.Manifest.release.artifact.filename) -ErrorAction Stop } catch { $rejected = $true }
    if (-not $rejected -or (Installed).Version -ne $installed.Version) { throw 'Ordinary update allowed an older package.' }
    Pass 'older_version_rejected'
    Add-AppxPackage -Path (Join-Path $currentBundle.Directory $currentBundle.Manifest.release.artifact.filename) -ErrorAction Stop
    if ((Installed).Version -ne $installed.Version) { throw 'Same-version operation changed installed version.' }
    Pass 'same_version_unchanged'
    Remove-AppxPackage -Package (Installed).PackageFullName -ErrorAction Stop
    foreach ($file in $sentinels) { if (-not (Test-Path -LiteralPath (Join-Path $data $file))) { throw 'Uninstall removed retained user data.' } }
    Pass 'uninstall_retention'
    $finished = $true
} catch {
    $record.failure_category = $_.Exception.GetType().Name
    $record.failure_hresult = ('0x' + $_.Exception.HResult.ToString('X8'))
    throw
} finally {
    if ($installedByThisRun -and $ownedPackageName) {
        $remaining = Installed
        if ($remaining -and $remaining.Publisher -ceq $candidate.Identity.Publisher) { Remove-AppxPackage -Package $remaining.PackageFullName -ErrorAction Continue }
    }
    foreach ($name in $environmentBefore.Keys) { [Environment]::SetEnvironmentVariable($name,$environmentBefore[$name],'Process') }
    if ($trustProcess) {
        [IO.File]::WriteAllText((Join-Path $root 'cleanup.request'),'cleanup')
        try { $null = $trustProcess.WaitForExit(10000) } catch { }
        $record.test_trust_cleaned = Test-Path -LiteralPath (Join-Path $root 'trust-cleaned.json')
    }
    if ($cert) {
        $ownedCert = Get-Item -LiteralPath ('Cert:\CurrentUser\My\' + $cert.Thumbprint) -ErrorAction SilentlyContinue
        if ($ownedCert -and $ownedCert.Subject -ceq $cert.Subject) { Remove-Item -LiteralPath ('Cert:\CurrentUser\My\' + $cert.Thumbprint) -DeleteKey -Force }
    }
    $record.all_checks_passed = $finished -and @($checks.Values | Where-Object { $_ -ne $true }).Count -eq 0
    $record.release_qualified = $false # isolated test identity never qualifies official production signing
    $record.completed = $finished
    $record.finished_at = [DateTimeOffset]::UtcNow.ToString('o')
    $recordPath = Join-Path $root 'msix-qualification.json'
    [IO.File]::WriteAllText($recordPath,($record | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    Write-Output "MSIX qualification record: $recordPath"
}

if ($finished -and -not $record.all_checks_passed) { Write-Warning 'Partial native qualification: see the false checks; production gates remain closed.'; exit 3 }
