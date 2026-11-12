$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'app_process.ps1')
$root = Join-Path (Split-Path $PSScriptRoot -Parent) ('.artifacts\tests\run-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force | Out-Null
$fixture = Join-Path $root 'WordSuggestorRunFixture.exe'
$source = @'
using System;
using System.IO;
using System.Windows.Forms;
public class RunFixture : Form {
    private readonly string root = Environment.GetEnvironmentVariable("WORDSUGGESTOR_RUN_TEST_ROOT");
    public RunFixture() { Text = "WordSuggestor Windows"; ShowInTaskbar = false; Opacity = 0; }
    protected override void OnShown(EventArgs e) { base.OnShown(e); File.WriteAllText(Path.Combine(root,"sample.txt"),Environment.GetEnvironmentVariable("WORDSUGGESTOR_WINDOWS_STARTUP_TEXT")); File.WriteAllText(Path.Combine(root,"ready"),"ready"); }
    protected override void OnFormClosing(FormClosingEventArgs e) {
        File.WriteAllText(Path.Combine(root,"close-requested"),"requested");
        e.Cancel = File.Exists(Path.Combine(root,"cancel-close"));
        base.OnFormClosing(e);
    }
    [STAThread] public static void Main() { Application.Run(new RunFixture()); }
}
'@
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Windows.Forms,System.Drawing -OutputAssembly $fixture -OutputType WindowsApplication
$launch = New-Object Diagnostics.ProcessStartInfo
$launch.FileName = $fixture
$launch.UseShellExecute = $false
$launch.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$launch.EnvironmentVariables['WORDSUGGESTOR_RUN_TEST_ROOT'] = $root
$app = [Diagnostics.Process]::Start($launch)
$checks = 0
function Assert([string]$Name, [bool]$Condition) {
    if (-not $Condition) { throw "FAIL run script: $Name" }
    $script:checks++; Write-Output "PASS run script: $Name"
}
try {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not (Test-Path -LiteralPath (Join-Path $root 'ready'))) {
        if ($app.HasExited -or [DateTime]::UtcNow -ge $deadline) { throw 'Fixture did not start.' }
        Start-Sleep -Milliseconds 50
    }
    $found = @(Get-WordSuggestorAppProcess $fixture)
    Assert 'find only the exact executable' ($found.Count -eq 1 -and $found[0].Id -eq $app.Id)
    Assert 'different executable is not selected' (@(Get-WordSuggestorAppProcess (Join-Path $root 'other\WordSuggestorRunFixture.exe')).Count -eq 0)
    Push-Location $root
    try {
        Assert 'relative paths use the PowerShell working directory' ((Resolve-WordSuggestorDevPath '.\WordSuggestorRunFixture.exe') -eq $fixture)
        Assert 'relative process lookup selects the same exact target' (@(Get-WordSuggestorAppProcess '.\WordSuggestorRunFixture.exe').Count -eq 1)
    } finally { Pop-Location }
    $rejected = $false
    try { Close-WordSuggestorAppGracefully $app (Join-Path $root 'wrong.exe') 1 } catch { $rejected = $_.Exception.Message -match 'identity changed' }
    Assert 'wrong target rejected without window message' ($rejected -and -not (Test-Path -LiteralPath (Join-Path $root 'close-requested')) -and -not $app.HasExited)
    New-Item -ItemType File -Path (Join-Path $root 'cancel-close') | Out-Null
    $cancelled = $false
    try { Close-WordSuggestorAppGracefully $app $fixture 1 } catch { $cancelled = $_.Exception.Message -match 'still running' }
    Assert 'cancelled close never kills the process' ($cancelled -and -not $app.HasExited -and (Test-Path -LiteralPath (Join-Path $root 'close-requested')))
    Remove-Item -LiteralPath (Join-Path $root 'cancel-close')
    Close-WordSuggestorAppGracefully $app $fixture 5
    Assert 'normal window close waits for process exit' ($app.HasExited)
    Assert 'closed process no longer locks the executable' (@(Get-WordSuggestorAppProcess $fixture).Count -eq 0)
    $previousRoot = [Environment]::GetEnvironmentVariable('WORDSUGGESTOR_RUN_TEST_ROOT','Process')
    $previousSample = [Environment]::GetEnvironmentVariable('WORDSUGGESTOR_WINDOWS_STARTUP_TEXT','Process')
    try {
        $env:WORDSUGGESTOR_RUN_TEST_ROOT = $root
        $env:WORDSUGGESTOR_WINDOWS_STARTUP_TEXT = 'PARENT_SAMPLE_CANARY'
        Remove-Item -LiteralPath (Join-Path $root 'ready')
        $launchOutput = @(& (Join-Path $PSScriptRoot 'run_app.ps1') -SkipBuild -SkipBootstrap -AppPath $fixture -SampleText 'PRIVATE_SAMPLE_CANARY' 6>&1)
        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        while (-not (Test-Path -LiteralPath (Join-Path $root 'ready'))) { if ([DateTime]::UtcNow -ge $deadline) { throw 'Launcher fixture did not start.' }; Start-Sleep -Milliseconds 50 }
        Assert 'startup text reaches only the owned child' ([IO.File]::ReadAllText((Join-Path $root 'sample.txt')) -ceq 'PRIVATE_SAMPLE_CANARY')
        Assert 'parent startup environment restored' ($env:WORDSUGGESTOR_WINDOWS_STARTUP_TEXT -ceq 'PARENT_SAMPLE_CANARY')
        Assert 'launch output does not echo sample content' (-not (($launchOutput | Out-String).Contains('PRIVATE_SAMPLE_CANARY')))
    } finally {
        foreach ($owned in @(Get-WordSuggestorAppProcess $fixture)) { Close-WordSuggestorAppGracefully $owned $fixture 5 }
        [Environment]::SetEnvironmentVariable('WORDSUGGESTOR_RUN_TEST_ROOT',$previousRoot,'Process')
        [Environment]::SetEnvironmentVariable('WORDSUGGESTOR_WINDOWS_STARTUP_TEXT',$previousSample,'Process')
    }
    foreach ($name in @('app_process.ps1','run_app.ps1','build_app.ps1')) {
        $tokens = $null; $errors = $null
        $null = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $name), [ref]$tokens, [ref]$errors)
        Assert "$name parses" ($errors.Count -eq 0)
    }
    Write-Output "PASS run scripts: $checks checks; only a synthetic fixture process was closed."
} finally {
    if (-not $app.HasExited) {
        Remove-Item -LiteralPath (Join-Path $root 'cancel-close') -Force -ErrorAction SilentlyContinue
        Close-WordSuggestorAppGracefully $app $fixture 5
    }
    $app.Dispose()
}
