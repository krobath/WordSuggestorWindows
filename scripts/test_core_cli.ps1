param([switch]$SkipBootstrap, [switch]$SettingsContract)
$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$coreRepo = Join-Path (Split-Path $repoRoot -Parent) "WordSuggestorCore"
$pack = Join-Path $coreRepo "Ressources\da_lexicon.sqlite"
if (-not $SkipBootstrap) { & (Join-Path $PSScriptRoot "bootstrap_core_cli.ps1") }
$cli = $env:WORDSUGGESTOR_SUGGEST_CLI_PATH
if (-not $cli) { $cli = Join-Path $coreRepo ".build\x86_64-unknown-windows-msvc\debug\WordSuggestorSuggestCLI.exe" }
if (-not (Test-Path -LiteralPath $cli)) { throw "Core CLI missing." }
$fixtures = @("skri", "l$([char]0x00e6)", "$([char]0x00f8)", "$([char]0x00e5)", "sm$([char]0x00f8)r", "bl$([char]0x00e5)")
$testDir = Join-Path $repoRoot (".artifacts\tests\cli-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$inputs = Join-Path $testDir "fixtures.txt"
[System.IO.File]::WriteAllLines($inputs, $fixtures, [System.Text.UTF8Encoding]::new($false))
# Decode native stdout explicitly; PowerShell's legacy console encoding may be ANSI.
$info = [System.Diagnostics.ProcessStartInfo]::new($cli)
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardInput = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$info.StandardOutputEncoding = [System.Text.Encoding]::UTF8
$info.StandardErrorEncoding = [System.Text.Encoding]::UTF8
$info.Arguments = '--lang da-DK --pack "' + $pack + '" --inputs "' + $inputs + '" --k 10'
$batchProcess = [System.Diagnostics.Process]::Start($info)
$batchOut = $batchProcess.StandardOutput.ReadToEndAsync()
$batchErr = $batchProcess.StandardError.ReadToEndAsync()
if (-not $batchProcess.WaitForExit(10000)) { $batchProcess.Kill(); throw "Legacy Core timeout." }
if ($batchProcess.ExitCode -ne 0) { throw "Legacy Core failed." }
$batch = $batchOut.Result | ConvertFrom-Json
$batchProcess.Dispose()
for ($i = 0; $i -lt $fixtures.Count; $i++) {
    $id = [guid]::NewGuid().ToString('N')
    $request = @{ version=1; requestId=$id; operation='suggest'; language='da-DK'; packPath=$pack; textBeforeCaret=$fixtures[$i]; maxSuggestions=10 }
    $info.Arguments = '--stdio-json-v1'
    $process = [System.Diagnostics.Process]::Start($info)
    try {
        $output = $process.StandardOutput.ReadToEndAsync()
        $errors = $process.StandardError.ReadToEndAsync()
        $inputWriter = [System.IO.StreamWriter]::new($process.StandardInput.BaseStream, [System.Text.UTF8Encoding]::new($false))
        $inputWriter.Write(($request | ConvertTo-Json -Compress))
        $inputWriter.Dispose()
        if (-not $process.WaitForExit(10000)) { $process.Kill(); throw "Core JSON timeout." }
        if ($process.ExitCode -ne 0) { throw "Core JSON failed ($($process.ExitCode))." }
        $response = $output.Result | ConvertFrom-Json
        if ($response.version -ne 1 -or $response.requestId -ne $id -or $response.errorCode) { throw "Core JSON contract failed." }
        if ($response.PSObject.Properties.Name -contains 'input') { throw "Core response must not echo document input." }
        if ($response.suggestions.Count -eq 0) { throw "Core returned no real suggestions." }
        $expected = $batch[$i].suggestions
        if ($response.suggestions.Count -ne $expected.Count) { throw "Core parity count differs for fixture $i." }
        for ($j=0; $j -lt $expected.Count; $j++) {
            if ($response.suggestions[$j].term -ne $expected[$j].term -or $response.suggestions[$j].kind -ne $expected[$j].kind -or [Math]::Abs($response.suggestions[$j].score - $expected[$j].score) -gt 0.000001) {
                throw "Core batch/JSON parity differs for fixture $i, candidate $j."
            }
        }
        Write-Output "PASS Core fixture $i : JSON v1, batch parity, $($response.suggestions.Count) candidates"
    } finally { if (-not $process.HasExited) { $process.Kill() }; $process.Dispose() }
}
Write-Output 'PASS Core CLI: 6 real Danish fixtures; no document echo.'
if ($SettingsContract) {
    $output = Join-Path $testDir 'settings-contract'
    & python (Join-Path $coreRepo 'Tools\SuggestCLI\test_settings_protocol.py') --cli $cli --output $output
    if ($LASTEXITCODE -ne 0) { throw "Core Settings contract failed ($LASTEXITCODE)." }
}
