param(
    [ValidateSet('State', 'Ui', 'Core', 'CommittedWord', 'UnknownWords', 'UnknownWordsUi', 'AutomaticWords', 'UnknownSharing', 'Speech', 'SpeechUi', 'Suggestions', 'SuggestionState', 'AnalysisState', 'AnalysisUi', 'AccountState', 'AccountUi', 'HelloNative', 'UpdateState', 'UpdateUi', 'UpdateNative', 'AdvancedState', 'AdvancedUi', 'All')][string]$Mode = 'All',
    [switch]$SkipBuild,
    [string]$RunnerPath
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'dotnet_env.ps1')
if (-not $SkipBuild) {
    dotnet build (Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\WordSuggestorWindows.Smoke.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'Settings app smoke build failed.' }
}
$runner = Join-Path $windowsRepo 'tests\WordSuggestorWindows.Smoke\bin\Debug\net10.0-windows\WordSuggestorWindows.Smoke.exe'
if ($RunnerPath) { $runner = [IO.Path]::GetFullPath($RunnerPath) }
if ($Mode -eq 'AdvancedState') {
    & $runner --advanced-state
    if ($LASTEXITCODE -ne 0) { throw "Advanced state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('AdvancedUi', 'All')) {
    & $runner --advanced-ui
    if ($LASTEXITCODE -ne 0) { throw "Advanced desktop smoke failed ($LASTEXITCODE). An active unlocked desktop is required." }
}
if ($Mode -eq 'UpdateNative') {
    & $runner --update-native
    if ($LASTEXITCODE -eq 3) { Write-Warning 'Packaged native update qualification needs a trusted isolated LocalTest MSIX; G05 remains read-only.'; exit 3 }
    if ($LASTEXITCODE -ne 0) { throw "Native update smoke failed ($LASTEXITCODE)." }
}
if ($Mode -eq 'UpdateState') {
    & $runner --update-state
    if ($LASTEXITCODE -ne 0) { throw "Update state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('UpdateUi', 'All')) {
    & $runner --update-ui
    if ($LASTEXITCODE -ne 0) { throw "Update desktop smoke failed ($LASTEXITCODE)." }
}
if ($Mode -eq 'HelloNative') {
    & $runner --hello-native
    if ($LASTEXITCODE -eq 3) { Write-Warning 'Native Hello qualification blocked; provision PIN/biometrics in a local unlocked session. Mock PASS does not enable protection.'; exit 3 }
    if ($LASTEXITCODE -ne 0) { throw "Native Hello smoke failed ($LASTEXITCODE)." }
}
if ($Mode -eq 'AccountState') {
    & $runner --account-state
    if ($LASTEXITCODE -ne 0) { throw "Account state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('AccountUi', 'All')) {
    & $runner --account-ui
    if ($LASTEXITCODE -ne 0) { throw "Account desktop smoke failed ($LASTEXITCODE)." }
}
if ($Mode -eq 'AnalysisState') {
    & $runner --analysis-state
    if ($LASTEXITCODE -ne 0) { throw "Analysis state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('AnalysisUi', 'All')) {
    & $runner --analysis-ui
    if ($LASTEXITCODE -ne 0) { throw "Analysis desktop smoke failed ($LASTEXITCODE)." }
}
if ($Mode -eq 'SuggestionState') {
    & $runner --suggestion-state
    if ($LASTEXITCODE -ne 0) { throw "Suggestion state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('Suggestions', 'All')) {
    & $runner --suggestion-settings
    if ($LASTEXITCODE -ne 0) { throw "Suggestion settings smoke failed ($LASTEXITCODE). An unlocked interactive Windows desktop is required." }
}
if ($Mode -in @('Speech', 'All')) {
    & $runner --speech-settings
    if ($LASTEXITCODE -ne 0) { throw "Settings speech adapter smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('Core', 'All')) {
    & $runner --core-settings
    if ($LASTEXITCODE -ne 0) { throw "Settings Core adapter smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('CommittedWord', 'All')) {
    & $runner --committed-word
    if ($LASTEXITCODE -ne 0) { throw "Committed-word assessment smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('State', 'All')) {
    & $runner --settings-state
    if ($LASTEXITCODE -ne 0) { throw "Settings state smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('Ui', 'All')) {
    & $runner --settings-ui
    if ($LASTEXITCODE -ne 0) { throw "Settings desktop smoke failed ($LASTEXITCODE). An unlocked interactive Windows desktop is required." }
}
if ($Mode -in @('SpeechUi', 'All')) {
    & $runner --speech-ui
    if ($LASTEXITCODE -ne 0) { throw "Speech desktop smoke failed ($LASTEXITCODE). An unlocked interactive Windows desktop is required." }
}

if ($Mode -in @('UnknownWords', 'All')) {
    & $runner --unknown-word
    if ($LASTEXITCODE -ne 0) { throw "Unknown-word import smoke failed ($LASTEXITCODE)." }
}
if ($Mode -in @('UnknownWordsUi', 'All')) {
    & $runner --unknown-word-ui
    if ($LASTEXITCODE -ne 0) { throw "Unknown-word desktop smoke failed ($LASTEXITCODE)." }
}

if ($Mode -in @('AutomaticWords', 'All')) {
 & $runner --automatic-word
 if ($LASTEXITCODE -ne 0) { throw "Automatic word smoke failed ($LASTEXITCODE)." }
}

if ($Mode -in @('UnknownSharing', 'All')) {
 & $runner --unknown-sharing
 if ($LASTEXITCODE -ne 0) { throw "Unknown sharing smoke failed ($LASTEXITCODE)." }
}
