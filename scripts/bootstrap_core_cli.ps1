param(
    [string]$WindowsSdkVersion = "10.0.22621.0",
    [ValidateSet("debug", "release")][string]$Configuration = "debug",
    [string]$ScratchPath,
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

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$workspaceRoot = Split-Path $repoRoot -Parent
$coreRepo = Join-Path $workspaceRoot "WordSuggestorCore"
$artifactsRoot = Join-Path $repoRoot ".artifacts"
$sqliteRoot = Join-Path $artifactsRoot "sqlite-dev"
$sqliteSrcRoot = Join-Path $sqliteRoot "src"
$sqliteBuildRoot = Join-Path $sqliteRoot "build"
$vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vsRoot = $env:VSINSTALLDIR
if (Test-Path -LiteralPath $vsWhere) {
    $vsRoot = & $vsWhere -latest -version '[17.0,18.0)' -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if ($LASTEXITCODE -ne 0) { throw 'Visual Studio toolchain discovery failed.' }
}
if (-not $vsRoot) { throw 'Visual Studio 2022 C++ tools were not found. Install the Desktop development with C++ workload.' }
$vsDevShell = Join-Path $vsRoot 'Common7\Tools\Launch-VsDevShell.ps1'

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
    $zipPath = Join-Path $sqliteRoot "sqlite-amalgamation-3460000-2024.zip"
    if (-not (Test-Path -LiteralPath $zipPath)) {
        Invoke-WebRequest -UseBasicParsing -Uri "https://www.sqlite.org/2024/sqlite-amalgamation-3460000.zip" -OutFile $zipPath
    }
    $expectedHash = "712A7D09D2A22652FB06A49AF516E051979A3984ADB067DA86760E60ED51A7F5"
    if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash -ne $expectedHash) {
        throw "Pinned SQLite 3.46.0 archive checksum mismatch."
    }
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

# Swift's SDK owns module maps. Never patch protected Windows SDK files here.
$null = Get-Command swift -ErrorAction Stop
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

$swiftBuildArgs = @('--package-path', [string]$coreRepo, '--configuration', $Configuration)
$overlayPath = & (Join-Path $PSScriptRoot 'swift_sdk_overlay.ps1') -PinnedIncludeRoot $pinnedIncludeRoot -OutputDirectory (Join-Path $artifactsRoot 'swift-sdk-overlay')
$compilerArgs = @('-Xswiftc','-windows-sdk-version','-Xswiftc',$WindowsSdkVersion,
    '-Xswiftc','-vfsoverlay','-Xswiftc',$overlayPath,'-Xcc','-ivfsoverlay','-Xcc',$overlayPath)
foreach ($folder in @('ucrt','shared','um','winrt')) { $compilerArgs += @('-Xcc',('-I' + (Join-Path $pinnedIncludeRoot $folder))) }
if ($ScratchPath) {
    $resolvedScratch = [System.IO.Path]::GetFullPath($ScratchPath)
    if (-not $resolvedScratch.StartsWith(([string]$repoRoot + [System.IO.Path]::DirectorySeparatorChar), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'ScratchPath must stay inside the Windows repository.'
    }
    $swiftBuildArgs += @('--scratch-path', $resolvedScratch)
}
Push-Location $coreRepo
try {
    if (-not $SkipCliBuild) {
        & swift build @swiftBuildArgs --product WordSuggestorSuggestCLI @compilerArgs
        if ($LASTEXITCODE -ne 0) { throw "Core CLI build failed ($LASTEXITCODE); stale executables are not accepted." }
    }
    $binPath = & swift build @swiftBuildArgs --show-bin-path
    if ($LASTEXITCODE -ne 0) { throw 'Swift output directory resolution failed.' }
} finally { Pop-Location }
$resolvedCli = Join-Path ($binPath | Select-Object -Last 1) 'WordSuggestorSuggestCLI.exe'
if (-not (Test-Path -LiteralPath $resolvedCli)) { throw 'Core CLI executable was not produced.' }
$env:WORDSUGGESTOR_SUGGEST_CLI_PATH = $resolvedCli

Write-Host "Windows core CLI bootstrap complete."
Write-Host "WS_SQLITE_INCLUDE=$env:WS_SQLITE_INCLUDE"
Write-Host "WS_SQLITE_LIB=$env:WS_SQLITE_LIB"
if ($resolvedCli) {
    Write-Host "WORDSUGGESTOR_SUGGEST_CLI_PATH=$resolvedCli"
}
