$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$workspaceRoot = Split-Path $repoRoot -Parent
$coreRepoPath = Join-Path $workspaceRoot "WordSuggestorCore"
$packPath = Join-Path $coreRepoPath "Ressources\da_lexicon.sqlite"
$sampleInputPath = Join-Path $repoRoot "sample_input.txt"
$bootstrapScript = Join-Path $repoRoot "scripts\bootstrap_core_cli.ps1"

if (-not (Test-Path $coreRepoPath)) {
    throw "WordSuggestorCore repo not found at '$coreRepoPath'."
}

if (-not (Test-Path $packPath)) {
    throw "Pack file not found at '$packPath'."
}

& $bootstrapScript

$ae = [char]0x00E6
$oe = [char]0x00F8
$aa = [char]0x00E5

Set-Content -Path $sampleInputPath -Encoding utf8 -Value @(
    "skri",
    "l$ae",
    "$oe",
    "$aa",
    "sm${oe}r",
    "bl$aa"
)

try {
    if ($env:WORDSUGGESTOR_SUGGEST_CLI_PATH -and (Test-Path $env:WORDSUGGESTOR_SUGGEST_CLI_PATH)) {
        & $env:WORDSUGGESTOR_SUGGEST_CLI_PATH --lang da-DK --pack $packPath --inputs $sampleInputPath --k 5
    } else {
        swift run --package-path $coreRepoPath WordSuggestorSuggestCLI --lang da-DK --pack $packPath --inputs $sampleInputPath --k 5
    }
}
finally {
    Remove-Item $sampleInputPath -ErrorAction SilentlyContinue
}
