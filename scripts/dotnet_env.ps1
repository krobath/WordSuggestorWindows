$ErrorActionPreference = 'Stop'
$windowsRepo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localSdk = Join-Path $windowsRepo '.artifacts\dotnet\dotnet.exe'
if (Test-Path -LiteralPath $localSdk) {
    $env:DOTNET_ROOT = Split-Path $localSdk -Parent
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
}
$env:DOTNET_CLI_HOME = Join-Path $windowsRepo '.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
