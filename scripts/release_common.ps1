$ErrorActionPreference = 'Stop'
$script:CanonicalBetaBase = 'https://wordsuggestor.com/beta-artifacts/windows/x64/beta/'
$script:ProductionPackageName = 'WordSuggestor.Windows.Beta'

function Get-ReleaseSdkTool([string]$Name) {
    $sdk = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
        Where-Object { $_.Name -match '^10\.0\.' -and (Test-Path -LiteralPath (Join-Path $_.FullName "x64\$Name.exe")) } |
        Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if (-not $sdk) { throw "Windows SDK $Name.exe is required." }
    return Join-Path $sdk.FullName "x64\$Name.exe"
}

function Read-ReleaseXml([string]$Text) {
    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create((New-Object System.IO.StringReader $Text), $settings)
    try {
        $document = New-Object System.Xml.XmlDocument
        $document.XmlResolver = $null
        $document.Load($reader)
        return ,$document
    } finally { $reader.Dispose() }
}

function Assert-PackageVersion([string]$Value) {
    if ($Value -cnotmatch '^(0|[1-9][0-9]{0,4})\.(0|[1-9][0-9]{0,4})\.(0|[1-9][0-9]{0,4})\.(0|[1-9][0-9]{0,4})$') {
        throw 'MSIX package version must have four canonical numeric components.'
    }
    foreach ($part in $Value.Split('.')) { if ([int]$part -gt 65535) { throw 'MSIX version component exceeds 65535.' } }
}

function Assert-ReleaseFile([string]$Path) {
    $item = Get-Item -LiteralPath $Path -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Release inputs must be regular files, not links or directories.'
    }
    return $item
}

function Get-ReleaseFileContract([string]$Path) {
    $item = Assert-ReleaseFile $Path
    if ($item.Name -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,191}$' -or $item.Name.Contains('..') -or $item.Length -le 0) {
        throw 'Invalid release basename or size.'
    }
    return [ordered]@{ filename=$item.Name; size_bytes=[long]$item.Length; sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
}

function Set-TestUpdatePolicyDisplayName($Document,[bool]$LocalTest) {
    if(-not $LocalTest -or $Document.Package.Identity.Name -cnotmatch '^WordSuggestor\.Windows\.LocalTest\.[a-f0-9]{12}$' -or
        $Document.Package.Identity.Publisher -cne ('CN=WordSuggestor Local Test '+$Document.Package.Identity.Name.Split('.')[-1])){throw 'Diagnostic display names require an isolated LocalTest identity.'}
    $label='WordSuggestor LocalTest '+$Document.Package.Identity.Name.Split('.')[-1]
    $Document.Package.Properties.DisplayName=$label
    $Document.Package.Properties.PublisherDisplayName=$label
    $visual=$Document.SelectSingleNode('//*[local-name()="VisualElements"]')
    if(-not $visual){throw 'Diagnostic package visual element missing.'}
    $visual.SetAttribute('DisplayName',$label)
}

function Add-TestUpdatePolicyCapabilities($Document,[bool]$LocalTest) {
    if(-not $LocalTest -or $Document.Package.Identity.Name -cnotmatch '^WordSuggestor\.Windows\.LocalTest\.[a-f0-9]{12}$' -or
        $Document.Package.Identity.Publisher -cne ('CN=WordSuggestor Local Test '+$Document.Package.Identity.Name.Split('.')[-1])){throw 'Update-policy capability experiments require an isolated LocalTest identity.'}
    foreach($name in @('packageManagement','packageQuery')) {
        $element=$Document.CreateElement('rescap','Capability','http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities')
        $element.SetAttribute('Name',$name)
        $null=$Document.Package.Capabilities.AppendChild($element)
    }
}

function Assert-ReleaseIntegrityManifest($Document) {
    $namespaces=New-Object Xml.XmlNamespaceManager($Document.NameTable)
    $namespaces.AddNamespace('p','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $namespaces.AddNamespace('u10','http://schemas.microsoft.com/appx/manifest/uap/windows10/10')
    $policy=$Document.SelectNodes('/p:Package/p:Properties/u10:PackageIntegrity',$namespaces)
    $content=$Document.SelectNodes('/p:Package/p:Properties/u10:PackageIntegrity/u10:Content',$namespaces)
    if($policy.Count -ne 1 -or $content.Count -ne 1 -or $content[0].GetAttribute('Enforcement') -cne 'on'){throw 'MSIX runtime package integrity enforcement must be on; rebuild this bundle.'}
}

function Get-MsixIdentity([string]$Path) {
    $null = Assert-ReleaseFile $Path
    Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($Path))
    try {
        $entry = $zip.GetEntry('AppxManifest.xml')
        if (-not $entry -or $entry.Length -gt 1048576) { throw 'MSIX manifest is missing or too large.' }
        $reader = New-Object IO.StreamReader($entry.Open(), [Text.Encoding]::UTF8)
        try { $document = Read-ReleaseXml $reader.ReadToEnd() } finally { $reader.Dispose() }
        Assert-ReleaseIntegrityManifest $document
        $identity = $document.Package.Identity
        if (-not $identity -or $identity.ProcessorArchitecture -cne 'x64') { throw 'An x64 MSIX identity is required.' }
        Assert-PackageVersion ([string]$identity.Version)
        return [pscustomobject]@{ Name=[string]$identity.Name; Publisher=[string]$identity.Publisher; Version=[string]$identity.Version; Architecture='x64' }
    } finally { $zip.Dispose() }
}

function Wait-ReleaseProbeResult([string]$Path,[string]$ExpectedName,[string]$ExpectedVersion,
    [ValidateRange(1,60)][int]$TimeoutSeconds=30) {
    if([IO.Path]::GetFileName($Path) -cnotmatch '^qualification-[a-f0-9]{32}\.json$'){throw 'Reserved probe result name required.'}
    $watch=[Diagnostics.Stopwatch]::StartNew()
    while($watch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        if(Test-Path -LiteralPath $Path -PathType Leaf) {
            try {
                $file=Get-Item -LiteralPath $Path
                if($file.Length -gt 16384){throw 'Probe result too large.'}
                $raw=[IO.File]::ReadAllText($Path)
            } catch [IO.IOException] { Start-Sleep -Milliseconds 100; continue }
            $result=$raw|ConvertFrom-Json -ErrorAction Stop
            $fields=@('schema_version','ok','package_name','package_version','error_category')
            Assert-ReleaseProperties $result $fields $fields 'installed probe'
            if(($result.schema_version -isnot [int] -and $result.schema_version -isnot [long]) -or $result.schema_version -ne 1 -or
                $result.ok -isnot [bool] -or -not $result.ok -or $result.package_name -cne $ExpectedName -or
                $result.package_version -cne $ExpectedVersion -or $null -ne $result.error_category){throw 'Installed Core/data probe result rejected.'}
            return $result
        }
        Start-Sleep -Milliseconds 100
    }
    throw 'Installed Core/data probe result timed out.'
}

function Test-MsixSignature([string]$Path, [string]$ExpectedPublisher) {
    $signTool = Get-ReleaseSdkTool 'signtool'
    # Do not copy raw verifier output into diagnostics: it includes local paths.
    $null = & $signTool verify /pa /all /v $Path 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'SignTool signature/trust verification failed.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $signature.SignerCertificate) {
        throw 'Windows did not validate the package signature.'
    }
    $cert = $signature.SignerCertificate
    if ($cert.Subject -cne $ExpectedPublisher) { throw 'Signing certificate subject does not match MSIX publisher.' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $identity = ([BitConverter]::ToString($sha.ComputeHash($cert.RawData))).Replace('-','').ToLowerInvariant() }
    finally { $sha.Dispose() }
    return [ordered]@{ publisher=$ExpectedPublisher; identity=('sha256:' + $identity); signature_verified=$true;
        verification_result='verified'; verified_at=[DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ');
        verification_tool='SignTool verify /pa /all /v' }
}

function Assert-ReleaseProperties($Object, [string[]]$Allowed, [string[]]$Required, [string]$Label) {
    if ($null -eq $Object -or $Object -isnot [pscustomobject]) { throw "$Label must be an object." }
    $names = @($Object.PSObject.Properties.Name)
    foreach ($name in $names) { if ($Allowed -cnotcontains $name) { throw "Unknown $Label field." } }
    foreach ($name in $Required) { if ($names -cnotcontains $name) { throw "Missing $Label field." } }
}

function Assert-ReleaseText($Value, [int]$Maximum, [bool]$Required = $true) {
    if ($Value -isnot [string] -or $Value.Length -gt $Maximum -or ($Required -and [string]::IsNullOrWhiteSpace($Value))) {
        throw 'Invalid release text field.'
    }
}

function Test-AppInstallerDocument([string]$Path, $Identity, [string]$PackageFileName, [switch]$LocalTest) {
    $file = Assert-ReleaseFile $Path
    if ($file.Name -cne 'WordSuggestor.Beta.appinstaller' -or $file.Length -gt 131072) { throw 'Unexpected App Installer file.' }
    $xml = Read-ReleaseXml ([IO.File]::ReadAllText($file.FullName))
    $root = $xml.DocumentElement
    if ($root.LocalName -cne 'AppInstaller' -or $root.NamespaceURI -cne 'http://schemas.microsoft.com/appx/appinstaller/2018') {
        throw 'Unexpected App Installer schema.'
    }
    $children = @($root.ChildNodes | Where-Object NodeType -eq ([Xml.XmlNodeType]::Element))
    if ($children.Count -ne 2 -or @($children | Where-Object { $_.NamespaceURI -cne $root.NamespaceURI -or @('MainPackage','UpdateSettings') -cnotcontains $_.LocalName }).Count -or
        @($children | Group-Object LocalName | Where-Object Count -ne 1).Count) { throw 'Unexpected or duplicate App Installer elements.' }
    $package = $root.MainPackage
    if (@($root.MainBundle).Count -gt 0 -and $root.MainBundle) { throw 'Unexpected bundle feed.' }
    if (-not $package -or @($package).Count -ne 1 -or $root.Version -cne $Identity.Version -or
        $package.Name -cne $Identity.Name -or $package.Publisher -cne $Identity.Publisher -or
        $package.Version -cne $Identity.Version -or $package.ProcessorArchitecture -cne 'x64') {
        throw 'App Installer identity, publisher or version mismatch.'
    }
    if ($LocalTest) {
        if ($Identity.Name -cnotmatch '^WordSuggestor\.Windows\.LocalTest\.[a-f0-9]{12}$' -or $Identity.Publisher -cne ('CN=WordSuggestor Local Test ' + $Identity.Name.Split('.')[-1])) { throw 'Local feed requires an isolated test identity.' }
        $feedUri = [uri]$root.Uri
        $packageUri = [uri]$package.Uri
        if ($feedUri.Scheme -ne 'file' -or $packageUri.Scheme -ne 'file' -or $feedUri.IsUnc -or $packageUri.IsUnc -or
            [IO.Path]::GetFileName($packageUri.LocalPath) -cne $PackageFileName -or
            [IO.Path]::GetDirectoryName($feedUri.LocalPath) -cne [IO.Path]::GetDirectoryName($packageUri.LocalPath)) {
            throw 'Local test feed must use sibling files on the local machine.'
        }
    } else {
        if ($root.Uri -cne ($script:CanonicalBetaBase + 'WordSuggestor.Beta.appinstaller') -or
            $package.Uri -cne ($script:CanonicalBetaBase + $PackageFileName)) { throw 'App Installer must use the canonical HTTPS beta URLs.' }
    }
    $updates = $root.UpdateSettings
    $updateChildren = @($updates.ChildNodes | Where-Object NodeType -eq ([Xml.XmlNodeType]::Element))
    if ($updateChildren.Count -ne 3 -or @($updateChildren | Where-Object { $_.NamespaceURI -cne $root.NamespaceURI -or @('OnLaunch','AutomaticBackgroundTask','ForceUpdateFromAnyVersion') -cnotcontains $_.LocalName }).Count -or
        @($updateChildren | Group-Object LocalName | Where-Object Count -ne 1).Count) { throw 'Unexpected or duplicate update policy elements.' }
    if (-not $updates.OnLaunch -or [int]$updates.OnLaunch.HoursBetweenUpdateChecks -lt 0 -or
        [int]$updates.OnLaunch.HoursBetweenUpdateChecks -gt 24 -or
        $updates.OnLaunch.HoursBetweenUpdateChecks -cnotmatch '^(0|[1-9][0-9]?)$' -or $updates.OnLaunch.ShowPrompt -cne 'true' -or
        $updates.OnLaunch.UpdateBlocksActivation -cne 'false' -or
        $null -eq $updates.SelectSingleNode("*[local-name()='AutomaticBackgroundTask']") -or
        $updates.ForceUpdateFromAnyVersion -cne 'false') {
        throw 'App Installer must enable nonblocking checks and prevent automatic downgrades.'
    }
    return $xml
}

function Test-ReleaseBundle([string]$BundleDirectory, [switch]$LocalTest, [switch]$VerifySignature) {
    $bundle = Get-Item -LiteralPath $BundleDirectory
    if (-not $bundle.PSIsContainer -or ($bundle.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Bundle must be a regular directory.' }
    $manifestPath = Join-Path $bundle.FullName 'release-manifest.json'
    $manifestFile = Assert-ReleaseFile $manifestPath
    if ($manifestFile.Length -gt 131072) { throw 'Release manifest exceeds 128 KiB.' }
    $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    Assert-ReleaseProperties $manifest @('schema_version','release') @('schema_version','release') 'manifest'
    if ($manifest.schema_version -isnot [int] -or $manifest.schema_version -ne 1) { throw 'Unsupported release manifest schema.' }
    $r = $manifest.release
    $fields = @('platform','architecture','channel','version','build_number','package_version','package_type','status','minimum_os','status_message','release_notes','known_issues','artifact','update_feed','signing')
    Assert-ReleaseProperties $r $fields $fields 'release'
    if ($r.platform -cne 'windows' -or $r.architecture -cne 'x64' -or $r.channel -cne 'beta' -or $r.package_type -cne 'msix' -or
        @('pending','active') -cnotcontains $r.status) { throw 'Only pending/active Windows x64 beta uploads are supported.' }
    Assert-PackageVersion $r.package_version
    Assert-ReleaseText $r.version 128
    Assert-ReleaseText $r.build_number 128
    Assert-ReleaseText $r.minimum_os 512
    Assert-ReleaseText $r.status_message 4096 $false
    foreach ($notes in @('release_notes','known_issues')) {
        if ($r.$notes -isnot [array] -or $r.$notes.Count -gt 30) { throw 'Release notes/issues must be bounded arrays.' }
        foreach ($note in $r.$notes) { Assert-ReleaseText $note 4096 }
    }
    $files = @('release-manifest.json')
    foreach ($pair in @(@('artifact','.msix'), @('update_feed','.appinstaller'))) {
        $metadata = $r.($pair[0])
        Assert-ReleaseProperties $metadata @('filename','size_bytes','sha256') @('filename','size_bytes','sha256') 'file'
        Assert-ReleaseText $metadata.filename 192
        if ($metadata.filename -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$' -or $metadata.filename.Contains('..') -or
            -not $metadata.filename.EndsWith($pair[1],[StringComparison]::Ordinal) -or
            $metadata.sha256 -isnot [string] -or $metadata.sha256 -cnotmatch '^[a-f0-9]{64}$') { throw 'Unsafe filename or digest.' }
        $contract = Get-ReleaseFileContract (Join-Path $bundle.FullName $metadata.filename)
        if ($metadata.size_bytes -isnot [int] -and $metadata.size_bytes -isnot [long]) { throw 'File size must be an integer.' }
        if ($metadata.size_bytes -ne $contract.size_bytes -or $metadata.sha256 -cne $contract.sha256) { throw 'Release size/checksum mismatch.' }
        $files += $metadata.filename
    }
    $actual = @(Get-ChildItem -LiteralPath $bundle.FullName -Force | ForEach-Object { $_.Name })
    if ($actual.Count -ne 3 -or @(Compare-Object $files $actual -CaseSensitive).Count -ne 0) { throw 'Bundle must contain exactly manifest, MSIX and App Installer.' }
    $identity = Get-MsixIdentity (Join-Path $bundle.FullName $r.artifact.filename)
    if ($identity.Version -cne $r.package_version -or (-not $LocalTest -and $identity.Name -cne $script:ProductionPackageName)) {
        throw 'Unexpected MSIX package identity/version.'
    }
    if (-not $LocalTest -and $identity.Publisher -match 'Local.?Test|Development|Test Only') { throw 'A local test publisher cannot be uploaded.' }
    $null = Test-AppInstallerDocument (Join-Path $bundle.FullName $r.update_feed.filename) $identity $r.artifact.filename -LocalTest:$LocalTest
    $signingFields = @('publisher','identity','signature_verified','verification_result','verified_at','verification_tool')
    Assert-ReleaseProperties $r.signing $signingFields $signingFields 'signing'
    if ($r.signing.publisher -cne $identity.Publisher -or $r.signing.signature_verified -isnot [bool] -or
        @('verified','failed','not_checked') -cnotcontains $r.signing.verification_result -or
        $r.signing.signature_verified -ne ($r.signing.verification_result -ceq 'verified')) { throw 'Inconsistent signing attestation.' }
    if ($r.status -ceq 'active' -and -not $r.signing.signature_verified) { throw 'Active releases require verified signing.' }
    if ($r.signing.signature_verified) {
        if ($r.signing.identity -cnotmatch '^sha256:[a-f0-9]{64}$' -or
            $r.signing.verification_tool -cne 'SignTool verify /pa /all /v') { throw 'Invalid signing identity or verifier.' }
        $date = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse($r.signing.verified_at,[ref]$date) -or $date -gt [DateTimeOffset]::UtcNow.AddMinutes(5)) {
            throw 'Invalid verification time.'
        }
    }
    if ($VerifySignature) {
        $verified = Test-MsixSignature (Join-Path $bundle.FullName $r.artifact.filename) $identity.Publisher
        if (-not $r.signing.signature_verified -or $verified.identity -cne $r.signing.identity) { throw 'Manifest does not attest the actual verified signer.' }
    }
    return [pscustomobject]@{ Manifest=$manifest; Identity=$identity; Directory=$bundle.FullName }
}
