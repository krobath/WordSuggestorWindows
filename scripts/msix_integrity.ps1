# Qualification helpers. Never edit installed files, execute altered code, change
# WindowsApps ACLs, or accept a changed signature/payload without verification.
function New-MsixSignatureCorruption([string]$Source,[string]$Destination) {
    $null=Assert-ReleaseFile $Source
    if(Test-Path -LiteralPath $Destination){throw 'Corruption fixture must be new.'}
    Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
    Copy-Item -LiteralPath $Source -Destination $Destination
    $zip=[IO.Compression.ZipFile]::Open($Destination,[IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry=$zip.GetEntry('AppxSignature.p7x')
        if(-not $entry -or $entry.Length -lt 8 -or $entry.Length -gt 1048576){throw 'Bounded signature footprint required.'}
        $memory=New-Object IO.MemoryStream;$input=$entry.Open()
        try {$input.CopyTo($memory);$bytes=$memory.ToArray()}finally{$input.Dispose();$memory.Dispose()}
        $bytes[$bytes.Length-1]=$bytes[$bytes.Length-1] -bxor 1
        $entry.Delete();$replacement=$zip.CreateEntry('AppxSignature.p7x',[IO.Compression.CompressionLevel]::NoCompression)
        $output=$replacement.Open();try{$output.Write($bytes,0,$bytes.Length)}finally{$output.Dispose()}
    }finally{$zip.Dispose()}
}

function New-MsixPayloadCorruption([string]$Source,[string]$Destination) {
    $null=Assert-ReleaseFile $Source
    if(Test-Path -LiteralPath $Destination){throw 'Corruption fixture must be new.'}
    Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
    # Inspect the first ZIP local header. MakeAppx's DLL payload fixture uses no
    # executable wrapper; the changed byte must be inside compressed DLL data.
    $stream=[IO.File]::OpenRead($Source)
    try {
        $header=New-Object byte[] 30
        if($stream.Read($header,0,30) -ne 30 -or [BitConverter]::ToUInt32($header,0) -ne 0x04034b50){throw 'ZIP local header required.'}
        $nameLength=[BitConverter]::ToUInt16($header,26);$extraLength=[BitConverter]::ToUInt16($header,28)
        if($nameLength -lt 1 -or $nameLength -gt 4096){throw 'Bounded payload name required.'}
        $nameBytes=New-Object byte[] $nameLength
        if($stream.Read($nameBytes,0,$nameLength) -ne $nameLength){throw 'Truncated payload name.'}
        $name=[Text.Encoding]::UTF8.GetString($nameBytes)
        if(-not $name.EndsWith('.dll',[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture first entry must be a DLL payload, not a footprint.'}
        $dataOffset=30+$nameLength+$extraLength
    }finally{$stream.Dispose()}
    $zip=[IO.Compression.ZipFile]::OpenRead($Source)
    try {
        $entry=$zip.GetEntry($name)
        if(-not $entry -or $entry.CompressedLength -lt 64){throw 'Nonempty compressed payload required.'}
        $offset=$dataOffset+53
    }finally{$zip.Dispose()}
    Copy-Item -LiteralPath $Source -Destination $Destination
    $stream=[IO.File]::Open($Destination,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try {$stream.Position=$offset;$value=$stream.ReadByte();if($value -lt 0){throw 'Payload offset outside archive.'};$stream.Position=$offset;$stream.WriteByte([byte]($value -bxor 1))}finally{$stream.Dispose()}
    return [pscustomobject]@{entry=$name;offset=$offset}
}

function Assert-InstalledMsixPayload([string]$SignedPackage,[string]$InstalledDirectory) {
    # Caller must first establish trust in SignedPackage with Test-ReleaseBundle
    # -VerifySignature. Hashes here compare installed bytes with that original.
    $null=Assert-ReleaseFile $SignedPackage
    $root=[IO.Path]::GetFullPath($InstalledDirectory).TrimEnd('\')
    if(-not (Test-Path -LiteralPath $root -PathType Container)){throw 'Installed payload directory required.'}
    Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
    $zip=[IO.Compression.ZipFile]::OpenRead($SignedPackage)
    try {
        $mapEntry=$zip.GetEntry('AppxBlockMap.xml')
        if(-not $mapEntry -or $mapEntry.Length -gt 8388608){throw 'Bounded block map required.'}
        $reader=New-Object IO.StreamReader($mapEntry.Open(),[Text.Encoding]::UTF8)
        try{$map=Read-ReleaseXml $reader.ReadToEnd()}finally{$reader.Dispose()}
        if($map.DocumentElement.LocalName -cne 'BlockMap' -or $map.DocumentElement.NamespaceURI -cne 'http://schemas.microsoft.com/appx/2010/blockmap'){throw 'Block-map schema rejected.'}
        $files=$map.GetElementsByTagName('File','http://schemas.microsoft.com/appx/2010/blockmap')
        if($files.Count -lt 1 -or $files.Count -gt 10000){throw 'Bounded payload file list required.'}
        $count=0;$seen=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach($file in $files){
            $name=$file.GetAttribute('Name').Replace('\','/')
            if(-not $seen.Add($name) -or [IO.Path]::IsPathRooted($name) -or $name.Contains(':')){throw 'Payload path rejected.'}
            $path=[IO.Path]::GetFullPath((Join-Path $root $name.Replace('/','\')))
            if(-not $path.StartsWith(($root+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Payload path escapes installed directory.'}
            $entry=$zip.GetEntry($name);$actual=Assert-ReleaseFile $path
            if(-not $entry -or $actual.Length -ne $entry.Length){throw 'Installed payload size differs from signed original.'}
            $sha=[Security.Cryptography.SHA256]::Create();$expectedStream=$entry.Open();$actualStream=[IO.File]::OpenRead($path)
            try {
                $expected=[Convert]::ToBase64String($sha.ComputeHash($expectedStream));$got=[Convert]::ToBase64String($sha.ComputeHash($actualStream))
                if($expected -cne $got){throw 'Installed payload differs from approved signed original.'}
            }finally{$expectedStream.Dispose();$actualStream.Dispose();$sha.Dispose()}
            $count++
        }
        return $count
    }finally{$zip.Dispose()}
}
