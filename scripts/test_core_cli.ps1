$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$workspaceRoot = Split-Path $repoRoot -Parent
$coreRepoPath = Join-Path $workspaceRoot "WordSuggestorCore"
$packPath = Join-Path $coreRepoPath "Ressources\da_lexicon.sqlite"
$sampleInputPath = Join-Path $repoRoot "sample_input.txt"

if (-not (Test-Path $coreRepoPath)) {
    throw "WordSuggestorCore repo not found at '$coreRepoPath'."
}

if (-not (Test-Path $packPath)) {
    throw "Pack file not found at '$packPath'."
}

Set-Content $sampleInputPath "skri"

try {
    swift run --package-path $coreRepoPath WordSuggestorSuggestCLI --lang da-DK --pack $packPath --inputs $sampleInputPath --k 5
}
finally {
    Remove-Item $sampleInputPath -ErrorAction SilentlyContinue
}
