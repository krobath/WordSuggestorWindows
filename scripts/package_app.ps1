param(
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$Version = '0.2.0.0',
    [string]$Publisher = 'CN=WordSuggestor',
    [string]$CoreCliPath = $env:WORDSUGGESTOR_SUGGEST_CLI_PATH,
    [string]$SwiftRuntimeDirectory,
    [string]$DanishPackPath,
    [string]$CertificateThumbprint,
    [uri]$FeedBaseUri,
    [uri]$TimestampUri = 'https://timestamp.digicert.com',
    [switch]$SkipCoreBuild,
    [switch]$FrameworkDependent
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if ($FeedBaseUri -and ($FeedBaseUri.Scheme -ne 'https' -or -not [string]::IsNullOrEmpty($FeedBaseUri.UserInfo) -or $FeedBaseUri.Query -or $FeedBaseUri.Fragment)) {
    throw 'Release feed must use HTTPS without credentials, query or fragment.'
}
if ($FeedBaseUri -and -not $CertificateThumbprint) { throw 'A signed package is required before generating update metadata.' }
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
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$project = Join-Path $windowsRepo 'src\WordSuggestorWindows.App\WordSuggestorWindows.App.csproj'
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
dotnet publish $project -c Release -r win-x64 --self-contained $selfContained -o $stage -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version
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
$manifest.Package.Identity.Publisher = $Publisher
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
# Record reproducible input identities without absolute machine paths or content.
$coreRepo = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore'
$coreCommit = if (Test-Path -LiteralPath (Join-Path $coreRepo '.git')) { git -C $coreRepo rev-parse HEAD } else { 'provided-binary' }
@{ version=$Version; coreCommit=$coreCommit; coreSha256=(Get-FileHash -LiteralPath $CoreCliPath -Algorithm SHA256).Hash;
   danishPackSha256=(Get-FileHash -LiteralPath $DanishPackPath -Algorithm SHA256).Hash;
   frameworkDependent=[bool]$FrameworkDependent } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'build-info.json') -Encoding UTF8
$suffix = if ($CertificateThumbprint) { '' } else { '.unsigned' }
$package = Join-Path $releaseRoot ("WordSuggestor.Beta.$Version$suffix.msix")
& $makeAppx pack /d $stage /p $package /o /v 2>&1 | Set-Content -LiteralPath (Join-Path $releaseRoot 'makeappx.log')
if ($LASTEXITCODE -ne 0) { throw "MSIX validation/pack failed ($LASTEXITCODE)." }
if ($CertificateThumbprint) {
    & $signTool sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUri.AbsoluteUri /td SHA256 $package
    if ($LASTEXITCODE -ne 0) { throw 'Package signing failed.' }
    & $signTool verify /pa /all $package
    if ($LASTEXITCODE -ne 0) { throw 'Signed package trust verification failed; feed not generated.' }
    if ($FeedBaseUri) {
        $feed = $FeedBaseUri.AbsoluteUri.TrimEnd('/')
        $escapedPublisher = [System.Security.SecurityElement]::Escape($Publisher)
        $escapedFeed = [System.Security.SecurityElement]::Escape($feed)
        $packageName = [System.IO.Path]::GetFileName($package)
        $appInstaller = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018" Version="$Version" Uri="$escapedFeed/WordSuggestor.Beta.appinstaller">
 <MainPackage Name="WordSuggestor.Windows.Beta" Publisher="$escapedPublisher" Version="$Version" ProcessorArchitecture="x64" Uri="$escapedFeed/$packageName" />
 <UpdateSettings><OnLaunch HoursBetweenUpdateChecks="6" ShowPrompt="true" UpdateBlocksActivation="false" /><AutomaticBackgroundTask /><ForceUpdateFromAnyVersion>false</ForceUpdateFromAnyVersion></UpdateSettings>
</AppInstaller>
"@
        [System.IO.File]::WriteAllText((Join-Path $releaseRoot 'WordSuggestor.Beta.appinstaller'), $appInstaller, [System.Text.UTF8Encoding]::new($false))
    }
}
Get-FileHash -LiteralPath $package -Algorithm SHA256 | Select-Object Hash
Write-Output "Package: $package"
if (-not $CertificateThumbprint) { Write-Output 'SIGNING PREPARATION ONLY: unsigned MSIX; no update feed generated.' }
