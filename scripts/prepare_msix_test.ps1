# Prepare N/N+1/rollback and newer rejection fixtures without device trust.
[CmdletBinding(SupportsShouldProcess=$true)]
param([string]$CoreCliPath,[switch]$SkipCoreBuild,[switch]$UpdatePolicyCapabilityExperiment,[string]$UpdatePolicyNativeControlPath,[switch]$BuildUpdatePolicyNativeControl)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'release_common.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(($UpdatePolicyNativeControlPath -or $BuildUpdatePolicyNativeControl) -and -not $UpdatePolicyCapabilityExperiment){throw 'Native control requires the isolated capability experiment.'}
if($UpdatePolicyNativeControlPath -and $BuildUpdatePolicyNativeControl){throw 'Choose a native control path or build it, not both.'}
if (-not $PSCmdlet.ShouldProcess('isolated LocalTest MSIX series','Build/sign using an ephemeral non-exportable user-store key; no trust or installation')) { return }
if($BuildUpdatePolicyNativeControl){
    & (Join-Path $PSScriptRoot 'build_update_policy_control.ps1') | ForEach-Object {if($_ -and $_.PSObject.Properties['NativeControlPath']){$UpdatePolicyNativeControlPath=$_.NativeControlPath}else{Write-Output $_}}
    if(-not $UpdatePolicyNativeControlPath){throw 'Native control build returned no executable.'}
}
$nonce=[guid]::NewGuid().ToString('N').Substring(0,12)
$name='WordSuggestor.Windows.LocalTest.'+$nonce
$subject='CN=WordSuggestor Local Test '+$nonce
$root=Join-Path $repo ('.artifacts\msix-tests\prepared-'+$nonce)
$feed=Join-Path $root 'feed'
New-Item -ItemType Directory -Path $feed -Force | Out-Null
$cert=$null
try {
    if (-not $SkipCoreBuild) { & (Join-Path $PSScriptRoot 'bootstrap_core_cli.ps1') -Configuration release; $CoreCliPath=$env:WORDSUGGESTOR_SUGGEST_CLI_PATH }
    if (-not $CoreCliPath -or -not (Test-Path -LiteralPath $CoreCliPath)) { throw 'Provide the qualified Core executable or allow bootstrap.' }
    $cert=New-SelfSignedCertificate -Type Custom -Subject $subject -CertStoreLocation 'Cert:\CurrentUser\My' `
        -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -KeyUsage DigitalSignature `
        -NotAfter ([DateTime]::Now.AddDays(7)) -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}CA=false')
    [IO.File]::WriteAllBytes((Join-Path $root 'local-test-public.cer'),$cert.RawData)
    $bundles=@()
    foreach($version in @('0.2.0.1','0.2.0.2','0.2.0.3','0.2.0.4')) {
        $built=$null
        & (Join-Path $PSScriptRoot 'package_app.ps1') -PackageVersion $version -DisplayVersion ('0.2.0-local.'+$version.Split('.')[-1]) `
            -BuildNumber ('local-'+$version.Split('.')[-1]) -LocalTestIdentity -TestPackageName $name -Publisher $subject `
            -CertificateThumbprint $cert.Thumbprint -LocalTestFeedDirectory $feed -PrepareUntrustedLocalTest -SkipCoreBuild -CoreCliPath $CoreCliPath -TestUpdatePolicyCapabilities:($UpdatePolicyCapabilityExperiment -and $version -ne '0.2.0.1') -TestUpdatePolicyNativeControlPath $UpdatePolicyNativeControlPath |
            ForEach-Object { if($_ -and $_.PSObject.Properties['BundleDirectory']){$built=$_}else{Write-Output $_} }
        if(-not $built){throw 'Local package preparation returned no bundle.'}
        $bundle=Test-ReleaseBundle $built.BundleDirectory -LocalTest
        if($bundle.Manifest.release.signing.signature_verified){throw 'Prepared bundle must not claim trusted signing.'}
        $bundles+=$built.BundleDirectory
    }
    $receipt=[ordered]@{schema_version=1;package_name=$name;publisher=$subject;certificate_thumbprint=$cert.Thumbprint;
        previous_bundle=$bundles[0];candidate_bundle=$bundles[1];rollback_bundle=$bundles[2];negative_bundle=$bundles[3];trust_changed=$false;installed=$false;key_removed=$false;update_policy_capability_experiment=[bool]$UpdatePolicyCapabilityExperiment}
    [IO.File]::WriteAllText((Join-Path $root 'prepared-session.json'),($receipt|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    Write-Output ('Prepared local test session: '+$root)
} finally {
    if($cert){
        $path='Cert:\CurrentUser\My\'+$cert.Thumbprint
        $owned=Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
        if($owned -and $owned.Subject -ceq $subject){Remove-Item -LiteralPath $path -DeleteKey -Force}
        $receiptPath=Join-Path $root 'prepared-session.json'
        if(Test-Path -LiteralPath $receiptPath){
            $receipt.key_removed=-not (Test-Path -LiteralPath $path)
            [IO.File]::WriteAllText($receiptPath,($receipt|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
        }
    }
}
