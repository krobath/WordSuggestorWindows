param([string]$CoreRevision, [switch]$IncludeWorkingTree, [switch]$FullRegression)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $CoreRevision) {
    $CoreRevision = (Get-Content -LiteralPath (Join-Path $windowsRepo 'dependencies.lock.json') -Raw | ConvertFrom-Json).coreRevision
}
$coreRepo = Join-Path (Split-Path $windowsRepo -Parent) 'WordSuggestorCore'
$root = Join-Path $windowsRepo ('.artifacts\clean-' + [guid]::NewGuid().ToString('N').Substring(0,12))
New-Item -ItemType Directory -Path $root -Force | Out-Null
foreach ($entry in @(@($windowsRepo,'WordSuggestorWindows','HEAD'), @($coreRepo,'WordSuggestorCore',$CoreRevision))) {
    if ($IncludeWorkingTree -and $entry[1] -eq 'WordSuggestorWindows') {
        $destination = Join-Path $root 'WordSuggestorWindows'
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Push-Location $windowsRepo
        try {
            $tracked = (& git -c core.quotepath=false ls-files -z --cached --others --exclude-standard) -join "`n"
            if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate clean-source snapshot.' }
            foreach ($relative in ($tracked.Split([char]0) | Where-Object { $_ })) {
                $source = [IO.Path]::GetFullPath((Join-Path $windowsRepo $relative))
                if (-not $source.StartsWith(($windowsRepo + '\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Snapshot path escapes repository.' }
                if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue } # tracked deletion
                $target = Join-Path $destination $relative
                New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
                Copy-Item -LiteralPath $source -Destination $target
            }
        } finally { Pop-Location }
        continue
    }
    $archive = Join-Path $root ($entry[1] + '.zip')
    & git -C $entry[0] archive --format=zip -o $archive $entry[2]
    if ($LASTEXITCODE -ne 0) { throw "Cannot export $($entry[1]) at $($entry[2])." }
    Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $root $entry[1])
}
$cleanWindows = Join-Path $root 'WordSuggestorWindows'
# Existing installed toolchains remain prerequisites. No source build output or
# local repository caches are copied into either exported checkout.
. (Join-Path $PSScriptRoot 'settings_qualification.ps1')
$receipt=[ordered]@{schema_version=1;windows_commit=(& git -C $windowsRepo rev-parse HEAD).Trim();
    scope=$(if($FullRegression){'full_software_regression'}else{'build_core_app'});
    source_kind=$(if($IncludeWorkingTree){'working_tree_snapshot'}else{'committed_export'});
    core_revision=$CoreRevision;started_at=[DateTimeOffset]::UtcNow.ToString('o');gates=[ordered]@{};
    software_qualified=$false;release_qualified=$false;hello_native='deferred_by_user';installed_update='not_run';
    human_accessibility='not_run';physical_mixed_dpi='not_run';live_sharing_receiver='not_qualified'}
$receiptPath=Join-Path $root 'regression.json'
# Canonical source fingerprint excludes output/caches and normalizes text newlines,
# allowing a reviewer to compare the exact exported software to the eventual commit.
$sourceFiles=@(Get-ChildItem -LiteralPath $cleanWindows -Recurse -File | Where-Object Extension -ne '.md' | Sort-Object FullName)
$fingerprintLines=foreach($file in $sourceFiles){
    $relative=$file.FullName.Substring($cleanWindows.Length+1).Replace('\','/')
    $bytes=[IO.File]::ReadAllBytes($file.FullName)
    if($file.Extension -in @('.cs','.xaml','.ps1','.py','.json','.xml','.config','.csproj','.sln','.manifest','.txt','.yml','.yaml') -or $file.Name -in @('.gitignore','.gitattributes')){
        $bytes=[Text.Encoding]::UTF8.GetBytes(([Text.Encoding]::UTF8.GetString($bytes)).Replace("`r`n","`n"))
    }
    $hasher=[Security.Cryptography.SHA256]::Create()
    try{$digest=[BitConverter]::ToString($hasher.ComputeHash($bytes)).Replace('-','').ToLowerInvariant()}finally{$hasher.Dispose()}
    $relative+' '+$digest
}
[IO.File]::WriteAllLines((Join-Path $root 'software-source.sha256'),[string[]]$fingerprintLines,[Text.UTF8Encoding]::new($false))
function Gate([string]$Name,[string]$Script,[string[]]$GateArguments=@()) {
    $log=Join-Path $root ($Name+'.log')
    $code=Invoke-SettingsGateProcess (Join-Path $cleanWindows ('scripts\'+$Script)) $GateArguments $log 6>$null
    $summary=@(Get-Content -LiteralPath $log | Where-Object {$_ -match '^PASS .*([0-9]+ checks|[0-9]+ contract|Core CLI|Core fixture|GUI [0-9]+|MEDIA 3|run scripts:)'} | ForEach-Object {[string]$_})
    $receipt.gates[$Name]=[ordered]@{exit_code=$code;status=$(if($code -eq 0){'pass'}elseif($code -eq 3){'blocked'}else{'fail'});summary=$summary}
    [IO.File]::WriteAllText($receiptPath,($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    Write-Output ('Clean source '+$Name+': '+$receipt.gates[$Name].status)
    if($code -ne 0){throw ('Clean source gate did not pass: '+$Name+'; see '+$log)}
}
Push-Location $cleanWindows
try {
    Gate 'build' 'build_app.ps1'
    $coreArguments=if($FullRegression){@('-SettingsContract')}else{@()}
    Gate 'core' 'test_core_cli.ps1' $coreArguments
    Gate 'app' 'test_app.ps1' @('-SkipBuild')
    if($FullRegression){
        Gate 'settings' 'test_settings_app.ps1' @('-SkipBuild')
        # All intentionally excludes native Hello and mutating installed-update tests.
        foreach($mode in @('SuggestionState','AnalysisState','AccountState','UpdateState','AdvancedState')){
            Gate ('settings-'+$mode) 'test_settings_app.ps1' @('-SkipBuild','-Mode',$mode)
        }
        Gate 'gui' 'test_gui.ps1' @('-SkipBuild')
        Gate 'media' 'test_media.ps1' @('-SkipBuild')
        Gate 'qualification' 'test_settings_qualification.ps1'
        Gate 'restart' 'test_run_scripts.ps1'
        $backend=Join-Path $windowsRepo '.artifacts\backend-reference\2f67ddc\scripts\beta_release_publish.py'
        $target=Join-Path $cleanWindows '.artifacts\backend-reference\2f67ddc\scripts\beta_release_publish.py'
        # The unchanged verifier independently checks this blob's pinned SHA-256.
        if(Test-Path -LiteralPath $backend){New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force|Out-Null;Copy-Item -LiteralPath $backend -Destination $target}
        Gate 'release-tools' 'test_release_scripts.ps1'
    }
    $receipt.software_qualified=[bool]$FullRegression
} finally {
    Pop-Location
    $receipt.finished_at=[DateTimeOffset]::UtcNow.ToString('o')
    [IO.File]::WriteAllText($receiptPath,($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    Write-Output ('Clean source evidence: '+$receiptPath)
}
if ($IncludeWorkingTree) { Write-Output "PASS clean source snapshot of working tree (no commit/index change): $root" }
else { Write-Output "PASS clean committed exports: $root" }
