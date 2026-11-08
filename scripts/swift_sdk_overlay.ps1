param([Parameter(Mandatory=$true)][string]$PinnedIncludeRoot, [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$sdkShare = Join-Path $env:SDKROOT 'usr\share'
if (-not (Test-Path -LiteralPath $sdkShare)) { throw 'Swift SDKROOT/usr/share is unavailable.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$mapRoots = @{
    'ucrt.modulemap' = (Join-Path $PinnedIncludeRoot 'ucrt')
    'winsdk_shared.modulemap' = (Join-Path $PinnedIncludeRoot 'shared')
    'winsdk_um.modulemap' = (Join-Path $PinnedIncludeRoot 'um')
}
$overlays = @()
foreach ($name in $mapRoots.Keys) {
    $source = Join-Path $sdkShare $name
    $text = [System.IO.File]::ReadAllText($source)
    $includeRoot = $mapRoots[$name]
    # Swift 6.3 embeds SDK module maps with relative headers. Resolve those to
    # the pinned SDK and expose the maps virtually; do not patch Program Files.
    $regex = [regex]'(header\s+")([^"]+)(")'
    $resolved = $regex.Replace($text, [System.Text.RegularExpressions.MatchEvaluator]{param($match)
        $match.Groups[1].Value + [System.IO.Path]::GetFullPath((Join-Path $includeRoot $match.Groups[2].Value)).Replace('\','/') + $match.Groups[3].Value
    })
    $map = Join-Path $OutputDirectory $name
    [System.IO.File]::WriteAllText($map, $resolved, [System.Text.UTF8Encoding]::new($false))
    $overlays += @{type='file';name=$source.Replace('\','/');'external-contents'=$map.Replace('\','/')}
    $folder = Split-Path $includeRoot -Leaf
    foreach ($sdk in Get-ChildItem -LiteralPath (Split-Path $PinnedIncludeRoot -Parent) -Directory) {
        $virtualPath = Join-Path $sdk.FullName "$folder\module.modulemap"
        $overlays += @{type='file';name=$virtualPath.Replace('\','/');'external-contents'=$map.Replace('\','/')}
    }
}
$overlayPath = Join-Path $OutputDirectory 'sdk-overlay.json'
$json = @{version=0;'case-sensitive'=$false;roots=$overlays} | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText($overlayPath,$json,[System.Text.UTF8Encoding]::new($false))
Write-Output $overlayPath
