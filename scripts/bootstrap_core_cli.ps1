param(
    [string]$WindowsSdkVersion = "10.0.22621.0",
    [switch]$SkipCliBuild
)

$ErrorActionPreference = "Stop"

function Merge-EnvPath {
    param(
        [string]$Name,
        [string[]]$Required
    )

    $current = [Environment]::GetEnvironmentVariable($Name, "Process")
    $existing = @()
    if ($current) {
        $existing = $current.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)
    }

    $ordered = @()
    foreach ($item in ($Required + $existing)) {
        if (-not $item) { continue }
        if (-not (Test-Path $item)) { continue }
        if ($ordered -contains $item) { continue }
        $ordered += $item
    }

    [Environment]::SetEnvironmentVariable($Name, ($ordered -join ';'), "Process")
}

function Normalize-WindowsSdkPathVar {
    param(
        [string]$Name,
        [string]$PinnedVersion
    )

    $current = [Environment]::GetEnvironmentVariable($Name, "Process")
    if (-not $current) {
        return
    }

    $entries = $current.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)
    $filtered = New-Object System.Collections.Generic.List[string]

    foreach ($entry in $entries) {
        $normalized = $entry.Trim()
        if (-not $normalized) { continue }

        if ($normalized -match 'Windows Kits\\10\\(Include|Lib)\\') {
            if ($normalized -match [regex]::Escape($PinnedVersion)) {
                if (-not $filtered.Contains($normalized)) {
                    [void]$filtered.Add($normalized)
                }
            }
            continue
        }

        if (-not $filtered.Contains($normalized)) {
            [void]$filtered.Add($normalized)
        }
    }

    [Environment]::SetEnvironmentVariable($Name, ($filtered -join ';'), "Process")
}

function Resolve-ModulemapSource {
    param(
        [string[]]$Names,
        [string[]]$Roots
    )

    foreach ($root in $Roots) {
        foreach ($name in $Names) {
            $candidate = Join-Path $root $name
            if (Test-Path $candidate) {
                return $candidate
            }
        }
    }

    return $null
}

function Ensure-Modulemap {
    param(
        [string]$Label,
        [string]$Destination,
        [string[]]$SourceNames,
        [bool]$Required,
        [string[]]$Roots
    )

    if (Test-Path $Destination) {
        return
    }

    $source = Resolve-ModulemapSource -Names $SourceNames -Roots $Roots
    if ($source) {
        New-Item -ItemType Directory -Force -Path (Split-Path $Destination -Parent) | Out-Null
        Copy-Item $source -Destination $Destination -Force
        return
    }

    $existing = foreach ($root in $Roots) {
        if (Test-Path $root) {
            Get-ChildItem -Path $root -Filter "*.modulemap" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name
        }
    }

    $message = "Missing modulemap source for $Label. Expected one of [$($SourceNames -join ', ')]. Source roots: [$($Roots -join '; ')]. Existing modulemaps: [$($existing -join ', ')]."
    if ($Required) {
        throw $message
    }

    Write-Warning $message
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$workspaceRoot = Split-Path $repoRoot -Parent
$coreRepo = Join-Path $workspaceRoot "WordSuggestorCore"
$artifactsRoot = Join-Path $repoRoot ".artifacts"
$sqliteRoot = Join-Path $artifactsRoot "sqlite-dev"
$sqliteSrcRoot = Join-Path $sqliteRoot "src"
$sqliteBuildRoot = Join-Path $sqliteRoot "build"
$vsDevShell = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Launch-VsDevShell.ps1"

if (-not (Test-Path $coreRepo)) {
    throw "WordSuggestorCore repo not found at '$coreRepo'."
}

if (-not (Test-Path $vsDevShell)) {
    throw "Visual Studio developer shell not found at '$vsDevShell'."
}

New-Item -ItemType Directory -Force -Path $sqliteSrcRoot | Out-Null
New-Item -ItemType Directory -Force -Path $sqliteBuildRoot | Out-Null

& $vsDevShell -Arch amd64 -HostArch amd64 | Out-Null

$pinnedIncludeRoot = "C:\Program Files (x86)\Windows Kits\10\Include\$WindowsSdkVersion"
$pinnedLibRoot = "C:\Program Files (x86)\Windows Kits\10\Lib\$WindowsSdkVersion"
if (-not (Test-Path $pinnedIncludeRoot)) {
    throw "Pinned Windows SDK include root missing: $pinnedIncludeRoot"
}

$env:WindowsSDKVersion = "$WindowsSdkVersion\"
$env:WindowsSDKLibVersion = "$WindowsSdkVersion\"
$env:UCRTVersion = "$WindowsSdkVersion\"

foreach ($name in @("INCLUDE", "EXTERNAL_INCLUDE", "LIB", "EXTERNAL_LIB", "LIBPATH", "EXTERNAL_LIBPATH", "PATH")) {
    Normalize-WindowsSdkPathVar -Name $name -PinnedVersion $WindowsSdkVersion
}

$sqliteHeader = Join-Path $sqliteBuildRoot "sqlite3.h"
$sqliteLib = Join-Path $sqliteBuildRoot "sqlite3.lib"

if (-not ((Test-Path $sqliteHeader) -and (Test-Path $sqliteLib))) {
    $candidates = New-Object System.Collections.Generic.List[hashtable]
    try {
        $downloadPage = Invoke-WebRequest -Uri "https://www.sqlite.org/download.html"
        $match = [regex]::Match($downloadPage.Content, '(20[0-9]{2})/sqlite-amalgamation-([0-9]{7})\.zip')
        if ($match.Success) {
            $latestYear = [int]$match.Groups[1].Value
            $latestTag = $match.Groups[2].Value
            $candidates.Add(@{
                Label = "latest-$latestTag"
                Tag = $latestTag
                Years = @($latestYear, $latestYear - 1, $latestYear + 1)
            })
        }
    } catch {
        Write-Host "SQLite latest resolution failed: $($_.Exception.Message). Using fallback list."
    }

    $candidates.Add(@{ Label = "3.46.0"; Tag = "3460000"; Years = @(2024, 2025, 2026) })
    $candidates.Add(@{ Label = "3.45.3"; Tag = "3450300"; Years = @(2024, 2025, 2026) })
    $candidates.Add(@{ Label = "3.44.2"; Tag = "3440200"; Years = @(2023, 2024, 2025, 2026) })

    $zipPath = $null
    $selected = $null
    $triedKeys = New-Object System.Collections.Generic.HashSet[string]
    foreach ($candidate in $candidates) {
        foreach ($year in $candidate.Years) {
            $tryKey = "$year-$($candidate.Tag)"
            if ($triedKeys.Contains($tryKey)) { continue }
            [void]$triedKeys.Add($tryKey)
            $url = "https://www.sqlite.org/$year/sqlite-amalgamation-$($candidate.Tag).zip"
            $probeZip = Join-Path $sqliteRoot "sqlite-amalgamation-$($candidate.Tag)-$year.zip"
            try {
                Invoke-WebRequest -Uri $url -OutFile $probeZip
                $zipPath = $probeZip
                $selected = $candidate
                break
            } catch {
                Write-Host "SQLite download miss for $url"
            }
        }
        if ($selected) { break }
    }

    if (-not $selected -or -not $zipPath) {
        throw "Failed to download any SQLite amalgamation candidate."
    }

    Remove-Item $sqliteSrcRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $sqliteSrcRoot | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $sqliteSrcRoot -Force

    $sqliteSourceDir = Get-ChildItem -Path $sqliteSrcRoot -Directory | Where-Object { $_.Name -like "sqlite-amalgamation-*" } | Select-Object -First 1
    if (-not $sqliteSourceDir) {
        throw "Could not locate extracted sqlite-amalgamation directory in $sqliteSrcRoot"
    }

    $sqliteC = Join-Path $sqliteSourceDir.FullName "sqlite3.c"
    $sqliteH = Join-Path $sqliteSourceDir.FullName "sqlite3.h"
    if (-not (Test-Path $sqliteC) -or -not (Test-Path $sqliteH)) {
        throw "sqlite3.c/sqlite3.h missing after extraction. SourceDir=$($sqliteSourceDir.FullName)"
    }

    & cl /nologo /O2 /DSQLITE_THREADSAFE=1 /DSQLITE_OMIT_LOAD_EXTENSION /c $sqliteC "/Fo$sqliteBuildRoot\sqlite3.obj"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to compile sqlite3.c"
    }

    & lib /nologo "/OUT:$sqliteLib" "$sqliteBuildRoot\sqlite3.obj"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create sqlite3.lib"
    }

    Copy-Item -Path $sqliteH -Destination $sqliteHeader -Force
}

$env:WS_SQLITE_INCLUDE = $sqliteBuildRoot
$env:WS_SQLITE_LIB = $sqliteBuildRoot
$env:CPATH = $sqliteBuildRoot
$env:C_INCLUDE_PATH = $sqliteBuildRoot
$env:LIBRARY_PATH = $sqliteBuildRoot

$swiftExe = (Get-Command swift -ErrorAction Stop).Source
$toolchainShare = Join-Path (Split-Path $swiftExe -Parent) "..\share"
$toolchainShare = [System.IO.Path]::GetFullPath($toolchainShare)
$sdkShare = if ($env:SDKROOT) { Join-Path $env:SDKROOT "usr\share" } else { $null }
$sourceRoots = @($sdkShare, $toolchainShare) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique
if (-not $sourceRoots -or $sourceRoots.Count -eq 0) {
    throw "No modulemap source roots found. SDKROOT=$env:SDKROOT toolchainShare=$toolchainShare"
}

try {
    Ensure-Modulemap -Label "ucrt" -Destination (Join-Path $pinnedIncludeRoot "ucrt\module.modulemap") -SourceNames @("ucrt.modulemap") -Required $true -Roots $sourceRoots
    Ensure-Modulemap -Label "winsdk_um" -Destination (Join-Path $pinnedIncludeRoot "um\module.modulemap") -SourceNames @("winsdk_um.modulemap") -Required $true -Roots $sourceRoots
    Ensure-Modulemap -Label "winsdk_shared" -Destination (Join-Path $pinnedIncludeRoot "shared\module.modulemap") -SourceNames @("winsdk_shared.modulemap") -Required $true -Roots $sourceRoots
    Ensure-Modulemap -Label "_guiddef" -Destination (Join-Path $pinnedIncludeRoot "shared\_guiddef.modulemap") -SourceNames @("_guiddef.modulemap", "guiddef.modulemap") -Required $false -Roots $sourceRoots
    Ensure-Modulemap -Label "_complex" -Destination (Join-Path $pinnedIncludeRoot "ucrt\_complex.modulemap") -SourceNames @("_complex.modulemap", "complex.modulemap") -Required $false -Roots $sourceRoots
    if ($env:VCToolsInstallDir) {
        Ensure-Modulemap -Label "visualc" -Destination (Join-Path $env:VCToolsInstallDir "include\module.modulemap") -SourceNames @("visualc.modulemap", "vcruntime.modulemap") -Required $false -Roots $sourceRoots
    }
} catch {
    Write-Warning "Modulemap bootstrap skipped: $($_.Exception.Message)"
}

$requiredInclude = @(
    (Join-Path $pinnedIncludeRoot "ucrt"),
    (Join-Path $pinnedIncludeRoot "um"),
    (Join-Path $pinnedIncludeRoot "shared"),
    (Join-Path $pinnedIncludeRoot "winrt"),
    (Join-Path $pinnedIncludeRoot "cppwinrt"),
    $sqliteBuildRoot
)
$requiredLib = @(
    (Join-Path $pinnedLibRoot "ucrt\x64"),
    (Join-Path $pinnedLibRoot "um\x64"),
    $sqliteBuildRoot
)

Merge-EnvPath -Name "INCLUDE" -Required $requiredInclude
Merge-EnvPath -Name "EXTERNAL_INCLUDE" -Required $requiredInclude
Merge-EnvPath -Name "LIB" -Required $requiredLib
Merge-EnvPath -Name "EXTERNAL_LIB" -Required $requiredLib

Push-Location $coreRepo
try {
    swift package describe --package-path $coreRepo | Out-Null
    if (-not $SkipCliBuild) {
        swift build --package-path $coreRepo --product WordSuggestorSuggestCLI -Xswiftc -windows-sdk-version -Xswiftc $WindowsSdkVersion
    }
}
finally {
    Pop-Location
}

$cliCandidates = @(
    (Join-Path $coreRepo ".build\debug\WordSuggestorSuggestCLI.exe"),
    (Join-Path $coreRepo ".build\debug\WordSuggestorSuggestCLI"),
    (Join-Path $coreRepo ".build\x86_64-unknown-windows-msvc\debug\WordSuggestorSuggestCLI.exe")
)
$resolvedCli = $cliCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($resolvedCli) {
    $env:WORDSUGGESTOR_SUGGEST_CLI_PATH = $resolvedCli
}

Write-Host "Windows core CLI bootstrap complete."
Write-Host "WS_SQLITE_INCLUDE=$env:WS_SQLITE_INCLUDE"
Write-Host "WS_SQLITE_LIB=$env:WS_SQLITE_LIB"
if ($resolvedCli) {
    Write-Host "WORDSUGGESTOR_SUGGEST_CLI_PATH=$resolvedCli"
}
