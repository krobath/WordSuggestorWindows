param(
    [Alias('Version')][string]$PackageVersion = '0.2.0.1',
    [string]$DisplayVersion = '0.2.0-beta.1',
    [string]$BuildNumber = '2001',
    [ValidateSet('pending','active')][string]$ReleaseStatus = 'pending',
    [string[]]$ReleaseNotes = @('Native Windows editor and WordSuggestorCore-based Danish suggestions.'),
    [string[]]$KnownIssues = @('External insertion is currently qualified for Notepad only.'),
    [string]$Publisher = 'CN=WordSuggestor',
    [string]$CoreCliPath = $env:WORDSUGGESTOR_SUGGEST_CLI_PATH,
    [string]$SwiftRuntimeDirectory,
    [string]$DanishPackPath,
    [string]$CertificateThumbprint,
    [uri]$FeedBaseUri,
    [uri]$TimestampUri = 'https://timestamp.digicert.com',
    [switch]$SkipCoreBuild,
    [switch]$FrameworkDependent,
    [switch]$LocalTestIdentity,
    [string]$TestPackageName,
    [string]$LocalTestFeedDirectory
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
. (Join-Path $PSScriptRoot 'release_common.ps1')
$Version = $PackageVersion
Assert-PackageVersion $PackageVersion
Assert-ReleaseText $DisplayVersion 128
Assert-ReleaseText $BuildNumber 128
$packageName = $script:ProductionPackageName
if ($LocalTestIdentity) {
    if ($TestPackageName -cnotmatch '^WordSuggestor\.Windows\.LocalTest\.[a-f0-9]{12}$' -or
        $Publisher -cne ('CN=WordSuggestor Local Test ' + $TestPackageName.Split('.')[-1]) -or -not $LocalTestFeedDirectory -or -not $CertificateThumbprint) {
        throw 'Local testing requires a separate test package, test publisher, local feed directory and certificate.'
    }
    $packageName = $TestPackageName
    $testFeedPath = [IO.Path]::GetFullPath($LocalTestFeedDirectory)
    if (-not $testFeedPath.StartsWith(($windowsRepo + '\.artifacts\'),[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Local test feed must remain under this repository .artifacts directory.'
    }
    $FeedBaseUri = [uri]($testFeedPath.TrimEnd('\') + '\')
} elseif ($TestPackageName -or $LocalTestFeedDirectory) { throw 'Test parameters require LocalTestIdentity.' }
elseif ($CertificateThumbprint) {
    if (-not $FeedBaseUri) { $FeedBaseUri = [uri]$script:CanonicalBetaBase }
    if ($FeedBaseUri.AbsoluteUri -cne $script:CanonicalBetaBase -or $Publisher -match 'Local.?Test|Development|Test Only') {
        throw 'Production builds require the canonical feed and a production publisher.'
    }
}
if ($FeedBaseUri -and -not $CertificateThumbprint) { throw 'A signed package is required before generating update metadata.' }
if ($ReleaseStatus -eq 'active' -and (-not $CertificateThumbprint -or $FrameworkDependent -or $LocalTestIdentity)) {
    throw 'Only a signed self-contained production package can be active.'
}
if ($TimestampUri.Scheme -ne 'https') { throw 'Timestamp endpoint must use HTTPS.' }
if (-not $SkipCoreBuild) { & (Join-Path $PSScriptRoot 'bootstrap_core_cli.ps1') -Configuration release; $CoreCliPath = $env:WORDSUGGESTOR_SUGGEST_CLI_PATH }
if (-not $CoreCliPath) {
    $CoreCliPath = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore\.build\x86_64-unknown-windows-msvc\debug\WordSuggestorSuggestCLI.exe'
}
if (-not (Test-Path -LiteralPath $CoreCliPath)) { throw 'Core CLI executable missing.' }
if (-not $DanishPackPath) { $DanishPackPath = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore\Ressources\da_lexicon.sqlite' }
if (-not (Test-Path -LiteralPath $DanishPackPath)) { throw 'Danish language pack missing.' }
if (-not $SwiftRuntimeDirectory) {
    $runtimeRoot = Join-Path $env:LOCALAPPDATA 'Programs\Swift\Runtimes'
    $runtime = Get-ChildItem -LiteralPath $runtimeRoot -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if ($runtime) { $SwiftRuntimeDirectory = Join-Path $runtime.FullName 'usr\bin' }
}
if (-not $SwiftRuntimeDirectory -or -not (Test-Path -LiteralPath (Join-Path $SwiftRuntimeDirectory 'swiftCore.dll'))) { throw 'Swift runtime directory missing; pass -SwiftRuntimeDirectory.' }
$sdkBin = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
    Where-Object { $_.Name -match '^10\.0\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'x64\makeappx.exe')) } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (-not $sdkBin) { throw 'Windows SDK makeappx.exe is required.' }
$makeAppx = Join-Path $sdkBin.FullName 'x64\makeappx.exe'
$signTool = Join-Path $sdkBin.FullName 'x64\signtool.exe'
$releaseRoot = Join-Path $windowsRepo ('.artifacts\release\' + $Version + '-' + [guid]::NewGuid().ToString('N'))
$stage = Join-Path $releaseRoot 'app'
$bundle = Join-Path $releaseRoot 'bundle'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
New-Item -ItemType Directory -Path $bundle -Force | Out-Null
$project = Join-Path $windowsRepo 'src\WordSuggestorWindows.App\WordSuggestorWindows.App.csproj'
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
dotnet publish $project -c Release -r win-x64 --self-contained $selfContained -o $stage -p:DebugType=None -p:DebugSymbols=false -p:Version=$DisplayVersion -p:AssemblyVersion=$PackageVersion -p:FileVersion=$PackageVersion
if ($LASTEXITCODE -ne 0) { throw "Publish failed ($LASTEXITCODE)." }
$coreStage = Join-Path $stage 'Core'
$packStage = Join-Path $stage 'Packs'
$assetStage = Join-Path $stage 'Assets'
New-Item -ItemType Directory -Path $coreStage,$packStage,$assetStage -Force | Out-Null
Copy-Item -LiteralPath $CoreCliPath -Destination (Join-Path $coreStage 'WordSuggestorSuggestCLI.exe')
Get-ChildItem -LiteralPath $SwiftRuntimeDirectory -Filter '*.dll' -File | Copy-Item -Destination $coreStage
$cliDirectory = Split-Path $CoreCliPath -Parent
Get-ChildItem -LiteralPath $cliDirectory -Directory -Filter '*.resources' | Copy-Item -Destination $coreStage -Recurse
Copy-Item -LiteralPath $DanishPackPath -Destination (Join-Path $packStage 'da_lexicon.sqlite')
# Reuse the existing product icon for all required MSIX tile sizes.
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile((Join-Path $windowsRepo 'src\WordSuggestorWindows.App\Assets\AppIcon.png'))
try {
    foreach ($asset in @(@('StoreLogo.png',50), @('Square150x150Logo.png',150), @('Square44x44Logo.png',44))) {
        $bitmap = New-Object System.Drawing.Bitmap ([int]$asset[1]),([int]$asset[1])
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try { $graphics.DrawImage($icon,0,0,[int]$asset[1],[int]$asset[1]); $bitmap.Save((Join-Path $assetStage $asset[0]),[System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $graphics.Dispose(); $bitmap.Dispose() }
    }
} finally { $icon.Dispose() }
$manifest = [xml](Get-Content -LiteralPath (Join-Path $windowsRepo 'packaging\AppxManifest.xml') -Raw -Encoding UTF8)
$manifest.Package.Identity.Version = $Version
$manifest.Package.Identity.Name = $packageName
$manifest.Package.Identity.Publisher = $Publisher
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
# Record reproducible input identities without absolute machine paths or content.
$coreRepo = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore'
$coreCommit = if (Test-Path -LiteralPath (Join-Path $coreRepo '.git')) { git -C $coreRepo rev-parse HEAD } else { 'provided-binary' }
@{ version=$Version; displayVersion=$DisplayVersion; buildNumber=$BuildNumber; packageName=$packageName; localTestIdentity=[bool]$LocalTestIdentity; coreCommit=$coreCommit; coreSha256=(Get-FileHash -LiteralPath $CoreCliPath -Algorithm SHA256).Hash;
   danishPackSha256=(Get-FileHash -LiteralPath $DanishPackPath -Algorithm SHA256).Hash;
   frameworkDependent=[bool]$FrameworkDependent } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'build-info.json') -Encoding UTF8
$suffix = if ($CertificateThumbprint) { '' } else { '.unsigned' }
$package = Join-Path $bundle ("WordSuggestor.Beta_$($Version)_x64$suffix.msix")
& $makeAppx pack /d $stage /p $package /o /v 2>&1 | Set-Content -LiteralPath (Join-Path $releaseRoot 'makeappx.log')
if ($LASTEXITCODE -ne 0) { throw "MSIX validation/pack failed ($LASTEXITCODE)." }
if ($CertificateThumbprint) {
    if ($CertificateThumbprint -notmatch '^[a-fA-F0-9]{40}$') { throw 'Invalid certificate thumbprint.' }
    if ($LocalTestIdentity) { & $signTool sign /sha1 $CertificateThumbprint /fd SHA256 $package }
    else { & $signTool sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUri.AbsoluteUri /td SHA256 $package }
    if ($LASTEXITCODE -ne 0) { throw 'Package signing failed.' }
    $attestation = Test-MsixSignature $package $Publisher
    if ($FeedBaseUri) {
        $feed = $FeedBaseUri.AbsoluteUri.TrimEnd('/')
        $escapedPublisher = [System.Security.SecurityElement]::Escape($Publisher)
        $escapedFeed = [System.Security.SecurityElement]::Escape($feed)
        $packageName = [System.IO.Path]::GetFileName($package)
        $appInstaller = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018" Version="$Version" Uri="$escapedFeed/WordSuggestor.Beta.appinstaller">
 <MainPackage Name="$($manifest.Package.Identity.Name)" Publisher="$escapedPublisher" Version="$Version" ProcessorArchitecture="x64" Uri="$escapedFeed/$packageName" />
 <UpdateSettings><OnLaunch HoursBetweenUpdateChecks="6" ShowPrompt="true" UpdateBlocksActivation="false" /><AutomaticBackgroundTask /><ForceUpdateFromAnyVersion>false</ForceUpdateFromAnyVersion></UpdateSettings>
</AppInstaller>
"@
        if ($LocalTestIdentity) { $appInstaller = $appInstaller.Replace('HoursBetweenUpdateChecks="6"','HoursBetweenUpdateChecks="0"') }
        $feedPath = Join-Path $bundle 'WordSuggestor.Beta.appinstaller'
        [System.IO.File]::WriteAllText($feedPath, $appInstaller, [System.Text.UTF8Encoding]::new($false))
        $release = [ordered]@{ schema_version=1; release=[ordered]@{
            platform='windows'; architecture='x64'; channel='beta'; version=$DisplayVersion; build_number=$BuildNumber;
            package_version=$PackageVersion; package_type='msix'; status=$ReleaseStatus;
            minimum_os='Windows 10 version 2004 or later, x64'; status_message='';
            release_notes=@($ReleaseNotes); known_issues=@($KnownIssues);
            artifact=(Get-ReleaseFileContract $package); update_feed=(Get-ReleaseFileContract $feedPath); signing=$attestation } }
        [IO.File]::WriteAllText((Join-Path $bundle 'release-manifest.json'),($release | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
        $null = Test-ReleaseBundle $bundle -LocalTest:$LocalTestIdentity -VerifySignature
    }
}
Get-FileHash -LiteralPath $package -Algorithm SHA256 | Select-Object Hash
Write-Output "Package: $package"
Write-Output "Bundle: $bundle"
if (-not $CertificateThumbprint) { Write-Output 'SIGNING PREPARATION ONLY: unsigned MSIX; no update feed generated.' }
[pscustomobject]@{ ReleaseDirectory=$releaseRoot; BundleDirectory=$bundle; AppDirectory=$stage; PackagePath=$package; LocalTestIdentity=[bool]$LocalTestIdentity }
