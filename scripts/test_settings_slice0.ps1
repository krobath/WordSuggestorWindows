param([switch]$Desktop, [switch]$CleanSource, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'settings_qualification.ps1')
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $repo ('.artifacts\settings-qualification\' + [guid]::NewGuid().ToString('N')) }
$out=[IO.Path]::GetFullPath($OutputDirectory)
if (-not $out.StartsWith(($repo+'\.artifacts\'),[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $out)) {
    throw 'Use a new, repository-local .artifacts output directory.'
}
New-Item -ItemType Directory -Path $out | Out-Null
$source=Get-SettingsSourceSnapshot $repo
function SourceDigest($Snapshot,[string]$Prefix) {
    $rows=@($Snapshot.files | Where-Object { $_.path.StartsWith($Prefix,[StringComparison]::Ordinal) } | ForEach-Object { $_.path+' '+$_.sha256 })
    $hash=[Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes(($rows -join "`n"))))).Replace('-','').ToLowerInvariant() }
    finally { $hash.Dispose() }
}
$lock=Get-Content -LiteralPath (Join-Path $repo 'dependencies.lock.json') -Raw | ConvertFrom-Json
$core=Join-Path (Split-Path $repo -Parent) 'WordSuggestorCore'
$coreHead=(& git -C $core rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $coreHead -cne $lock.coreRevision -or @(& git -C $core status --porcelain).Count) { throw 'Use the clean, pinned Core revision.' }
$packHash=(Get-FileHash -LiteralPath (Join-Path $core 'Ressources\da_lexicon.sqlite') -Algorithm SHA256).Hash
if ($packHash -cne $lock.danishPackSha256) { throw 'Danish pack digest differs from qualified lock.' }
$source | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $out 'source.json') -Encoding UTF8
$record=[ordered]@{
    schema_version=1; sprint='WSA-UX-029'; slice=0; started_at=[DateTimeOffset]::UtcNow.ToString('o')
    windows_base_commit=$source.head; working_tree_dirty=$source.working_tree_dirty
    app_source_sha256=(SourceDigest $source 'src/'); tests_source_sha256=(SourceDigest $source 'tests/')
    mac_reference='ba18b451372dd2e3f125e557c5db45894f4e1ae3'; core_commit=$coreHead
    danish_pack_sha256=$packHash.ToLowerInvariant(); dotnet_sdk=$lock.dotnetSdk
    gates=[ordered]@{}; completed=$false
}
function RunGate([string]$Name,[string]$Script,[string[]]$Arguments=@()) {
    $clock=[Diagnostics.Stopwatch]::StartNew()
    $exitCode=Invoke-SettingsGateProcess (Join-Path $PSScriptRoot $Script) $Arguments (Join-Path $out ($Name+'.log'))
    $record.gates[$Name]=[ordered]@{passed=($exitCode -eq 0); exit_code=$exitCode; seconds=[Math]::Round($clock.Elapsed.TotalSeconds,2)}
    if ($exitCode -ne 0) { throw "Slice-0 gate failed: $Name ($exitCode)." }
}
try {
    RunGate 'qualification-contract' 'test_settings_qualification.ps1'
    RunGate 'build' 'build_app.ps1'
    RunGate 'core' 'test_core_cli.ps1' @('-SkipBootstrap')
    RunGate 'app' 'test_app.ps1' @('-SkipBuild')
    RunGate 'run-scripts' 'test_run_scripts.ps1'
    RunGate 'release-scripts' 'test_release_scripts.ps1'
    RunGate 'platform' 'test_settings_platform.ps1' @('-OutputDirectory',(Join-Path $out 'platform'))
    if ($Desktop) {
        RunGate 'desktop' 'test_gui.ps1' @('-SkipBuild')
        RunGate 'media' 'test_media.ps1' @('-SkipBuild')
    }
    if ($CleanSource) { RunGate 'clean-source' 'test_clean_checkout.ps1' @('-IncludeWorkingTree') }
    $after=Get-SettingsSourceSnapshot $repo
    if ((SourceDigest $after 'src/') -cne $record.app_source_sha256 -or (SourceDigest $after 'tests/') -cne $record.tests_source_sha256) {
        throw 'Application/test source changed during qualification; results are not a frozen baseline.'
    }
    $record.completed=$true
} finally {
    $record['finished_at']=[DateTimeOffset]::UtcNow.ToString('o')
    $record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $out 'baseline.json') -Encoding UTF8
}
Write-Output ('PASS slice-0 baseline of the recorded working tree: '+$out)
Write-Output 'Hello protected-secret and packaged-app update authority are separate qualification gates, not implied by baseline PASS.'
