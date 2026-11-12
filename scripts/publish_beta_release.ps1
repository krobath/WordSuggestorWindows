[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='Medium')]
param(
    [Parameter(Mandatory=$true)][string]$BundleDirectory,
    [ValidateSet('Validate','RegisterPending','Publish')][string]$Action = 'Validate',
    [Parameter(Mandatory=$true)][string]$HostName,
    [Parameter(Mandatory=$true)][string]$UserName,
    [Parameter(Mandatory=$true)][string]$KnownHostsFile,
    [Parameter(Mandatory=$true)][string]$HostKeyFingerprint,
    [int]$Port = 22,
    [string]$IdentityFile,
    [Parameter(Mandatory=$true)][string]$BackendDirectory,
    [string]$QualificationPath,
    [switch]$FirstRelease,
    [string]$SshPath = "$env:SystemRoot\System32\OpenSSH\ssh.exe",
    [string]$SftpPath = "$env:SystemRoot\System32\OpenSSH\sftp.exe",
    [ValidateRange(1,3600)][int]$CommandTimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release_common.ps1')
. (Join-Path $PSScriptRoot 'release_transport.ps1')
$candidate = Test-ReleaseBundle $BundleDirectory -VerifySignature:(!$WhatIfPreference)
$r = $candidate.Manifest.release
if ($BackendDirectory -cnotmatch '^/[A-Za-z0-9_./-]+$' -or @($BackendDirectory.Split('/') | Where-Object { $_ -eq '..' -or $_ -eq '.' }).Count) {
    throw 'Backend directory must be a safe absolute server path.'
}
$sshOptions = @(Get-PinnedSshOptions $HostName $Port $UserName $KnownHostsFile $HostKeyFingerprint $IdentityFile)
$operation = switch ($Action) { 'RegisterPending' { 'register' }; 'Publish' { 'publish' }; default { 'validate' } }
if (($Action -eq 'RegisterPending' -and $r.status -cne 'pending') -or ($Action -eq 'Publish' -and $r.status -cne 'active')) {
    throw 'RegisterPending requires pending; Publish requires a newly uploaded verified active manifest.'
}
$stagingId = 'windows-x64-beta-' + $r.package_version + '-' + [guid]::NewGuid().ToString('D')
$stagingRoot = '/data/docker_data/volumes/wordsuggestorbackend_beta_staging'
$temporary = "$stagingRoot/$stagingId.uploading"
$ready = "$stagingRoot/$stagingId"
$filenames = @('release-manifest.json', $r.artifact.filename, $r.update_feed.filename)
if (-not $PSCmdlet.ShouldProcess("$HostName Windows x64 beta $($r.package_version)", "$Action through O-052 private staging")) {
    [pscustomobject]@{ what_if=[bool]$WhatIfPreference; cancelled=(-not $WhatIfPreference); action=$Action; staging_id=$stagingId; staging_path=$temporary;
        files=$filenames; signature_checked=$false; network_contacted=$false }
    return
}

function Assert-Qualification {
    if (-not $QualificationPath) { throw 'Publish requires the actual MSIX integration qualification record.' }
    $null = Assert-ReleaseFile $QualificationPath
    $q = [IO.File]::ReadAllText([IO.Path]::GetFullPath($QualificationPath)) | ConvertFrom-Json
    if ($q.schema_version -ne 1 -or $q.local_test_identity -ne $false -or $q.package_name -cne $candidate.Identity.Name -or
        $q.publisher -cne $candidate.Identity.Publisher -or $q.package_version -cne $r.package_version -or
        $q.package_sha256 -cne $r.artifact.sha256 -or $q.feed_sha256 -cne $r.update_feed.sha256) {
        throw 'Qualification does not cover this exact production bundle.'
    }
    foreach ($check in @('clean_install','standard_user','core_suggestions','desktop_smoke','n_to_n1','data_retention',
        'installed_relaunch','manual_update','automatic_update','offline_feed','invalid_feed','wrong_identity',
        'wrong_publisher','tampered_package','older_version_rejected','uninstall_retention')) {
        if ($q.checks.$check -isnot [bool] -or -not $q.checks.$check) { throw "Required qualification gate missing: $check." }
    }
}

function Assert-PublicVersionAdvance {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri ($script:CanonicalBetaBase + 'WordSuggestor.Beta.appinstaller') -TimeoutSec 20 -MaximumRedirection 0
    } catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        if ($status -eq 404 -and $FirstRelease) { return }
        throw 'Existing public feed cannot be verified. Only an explicit first release may accept HTTP 404.'
    }
    if ($response.RawContentLength -gt 131072) { throw 'Existing public feed is too large.' }
    $xml = Read-ReleaseXml $response.Content
    $package = $xml.AppInstaller.MainPackage
    if ($package.Name -cne $candidate.Identity.Name -or $package.Publisher -cne $candidate.Identity.Publisher -or
        $package.ProcessorArchitecture -cne 'x64') { throw 'Existing public feed identity/publisher differs.' }
    Assert-PackageVersion ([string]$package.Version)
    if ([version]$r.package_version -le [version]$package.Version) { throw 'Published package versions must increase; rollback also needs a higher version.' }
}

function Test-MacWebsiteRegression {
    try {
        $page = Invoke-WebRequest -UseBasicParsing -Uri 'https://wordsuggestor.com/beta' -TimeoutSec 20 -MaximumRedirection 0
        $matches = [regex]::Matches($page.Content, 'href="(https://wordsuggestor\.com/beta-artifacts/[A-Za-z0-9_./-]+\.dmg)"')
        if ($matches.Count -eq 0) { throw 'Mac download absent.' }
        foreach ($match in $matches) {
            $null = Invoke-WebRequest -UseBasicParsing -Method Head -Uri $match.Groups[1].Value -TimeoutSec 20 -MaximumRedirection 0
        }
        $feed = Invoke-WebRequest -UseBasicParsing -Uri 'https://wordsuggestor.com/beta-artifacts/appcast-beta.xml' -TimeoutSec 20 -MaximumRedirection 0
        if ($feed.RawContentLength -gt 1048576 -or $feed.Content -notmatch 'edSignature') { throw 'Mac signed appcast unavailable.' }
    } catch { throw 'Public Mac page/download/appcast regression gate failed.' }
}

if ($Action -eq 'Publish') {
    Assert-Qualification
    Assert-PublicVersionAdvance
    Test-MacWebsiteRegression
    # Re-run real payload smoke from the package to exclude a stale app directory.
    & (Join-Path $PSScriptRoot 'test_release_candidate.ps1') -BundleDirectory $candidate.Directory
}

$endpoint = [pscustomobject]@{ HostName=$HostName; Port=$Port; UserName=$UserName; KnownHostsFile=$KnownHostsFile;
    HostKeyFingerprint=$HostKeyFingerprint; IdentityFile=$IdentityFile; SshPath=$SshPath; SftpPath=$SftpPath;
    BackendDirectory=$BackendDirectory; TimeoutSeconds=$CommandTimeoutSeconds }
$upload = Invoke-PrivateReleaseStaging $candidate $endpoint $operation $stagingId
$final = $upload.result
if ($Action -eq 'RegisterPending' -and $final.status -cne 'pending') { throw 'Publisher did not register a pending target.' }
if ($Action -eq 'Publish') {
    if ($final.status -cne 'active' -or $final.artifact_url -cne ($script:CanonicalBetaBase + $r.artifact.filename) -or
        $final.update_feed_url -cne ($script:CanonicalBetaBase + 'WordSuggestor.Beta.appinstaller')) { throw 'Publisher returned unexpected public URLs/state.' }
    $verifyRoot = Join-Path ([IO.Path]::GetTempPath()) ('wordsuggestor-public-verify-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $verifyRoot | Out-Null
    foreach ($entry in @(@($r.artifact,$final.artifact_url), @($r.update_feed,$final.update_feed_url))) {
        $copy = Join-Path $verifyRoot $entry[0].filename
        $null = Invoke-WebRequest -UseBasicParsing -Uri $entry[1] -OutFile $copy -TimeoutSec 180 -MaximumRedirection 0
        $actual = Get-ReleaseFileContract $copy
        if ($actual.sha256 -cne $entry[0].sha256 -or $actual.size_bytes -ne $entry[0].size_bytes) {
            throw 'Post-publication HTTPS verification failed. Stop the feed and withdraw through the documented server controls.'
        }
    }
    Test-MacWebsiteRegression
}
[pscustomobject]@{ staging_id=$stagingId; result=$final }
