# Shared transport for the release publisher. Windows PowerShell 5.1 compatible;
# ProcessStartInfo executes binaries directly, with no cmd/PowerShell shell layer.
if (-not ('WordSuggestor.Release.ProcessRunner' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
namespace WordSuggestor.Release {
    public sealed class ProcessResult { public int ExitCode; public string Output; public string Error; }
    public static class ProcessRunner {
        public static string Quote(string value) {
            var b = new StringBuilder("\""); int slashes = 0;
            foreach (char c in value) {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') { b.Append('\\', slashes * 2 + 1); b.Append(c); }
                else { b.Append('\\', slashes); b.Append(c); }
                slashes = 0;
            }
            b.Append('\\', slashes * 2); b.Append('"'); return b.ToString();
        }
        static async Task<string> ReadBounded(StreamReader reader, int limit) {
            char[] block = new char[4096]; var text = new StringBuilder(); int n;
            while ((n = await reader.ReadAsync(block, 0, block.Length)) != 0) {
                if (text.Length + n > limit) throw new IOException("Release command output limit exceeded.");
                text.Append(block, 0, n);
            }
            return text.ToString();
        }
        static void Stop(Process process) {
            try {
                if (process.HasExited) return;
                var stop = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe"),
                    "/PID " + process.Id + " /T /F");
                stop.UseShellExecute = false; stop.CreateNoWindow = true;
                stop.RedirectStandardOutput = true; stop.RedirectStandardError = true;
                using (var child = Process.Start(stop)) {
                    child.StandardOutput.ReadToEnd(); child.StandardError.ReadToEnd(); child.WaitForExit(5000);
                }
                process.WaitForExit(5000);
            } catch { try { process.Kill(); } catch {} }
        }
        public static ProcessResult Run(string executable, string[] args, int timeoutSeconds) {
            var quoted = new string[args.Length]; for (int i = 0; i < args.Length; i++) quoted[i] = Quote(args[i]);
            var start = new ProcessStartInfo(executable, String.Join(" ", quoted));
            start.UseShellExecute = false; start.CreateNoWindow = true;
            start.RedirectStandardOutput = true; start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8; start.StandardErrorEncoding = Encoding.UTF8;
            using (var process = Process.Start(start)) {
                var output = ReadBounded(process.StandardOutput, 1048576);
                var error = ReadBounded(process.StandardError, 65536);
                var watch = Stopwatch.StartNew();
                try {
                    while (!process.WaitForExit(50) || !output.IsCompleted || !error.IsCompleted) {
                        if (output.IsFaulted || error.IsFaulted) throw new IOException("Release command output rejected.");
                        if (watch.Elapsed.TotalSeconds >= timeoutSeconds) throw new TimeoutException("Release command timed out.");
                        if (process.HasExited) System.Threading.Thread.Sleep(10);
                    }
                    return new ProcessResult { ExitCode = process.ExitCode, Output = output.GetAwaiter().GetResult(), Error = error.GetAwaiter().GetResult() };
                } catch {
                    Stop(process);
                    output.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    error.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    throw;
                }
            }
        }
    }
}
'@
}

function Get-PinnedSshOptions([string]$HostName, [int]$Port, [string]$UserName, [string]$KnownHostsFile,
    [string]$HostKeyFingerprint, [string]$IdentityFile) {
    if ($HostName -cnotmatch '^[A-Za-z0-9][A-Za-z0-9.-]{0,252}$' -or $UserName -cnotmatch '^[A-Za-z_][A-Za-z0-9_.-]{0,63}$' -or
        $Port -lt 1 -or $Port -gt 65535 -or $HostKeyFingerprint -cnotmatch '^SHA256:[A-Za-z0-9+/]{43}$') {
        throw 'Invalid SSH endpoint or pinned ED25519 fingerprint.'
    }
    $file = Assert-ReleaseFile $KnownHostsFile
    $lines = @([IO.File]::ReadAllLines($file.FullName) | Where-Object { $_.Trim() -and -not $_.Trim().StartsWith('#') })
    if ($lines.Count -ne 1) { throw 'Use a dedicated known_hosts file containing exactly one approved ED25519 key.' }
    $parts = $lines[0].Trim() -split '\s+'
    $lookup = if ($Port -eq 22) { $HostName } else { '[' + $HostName + ']:' + $Port }
    if ($parts.Count -lt 3 -or $parts[0] -cne $lookup -or $parts[1] -cne 'ssh-ed25519') { throw 'Pinned host entry does not match the selected SSH endpoint.' }
    try { $key = [Convert]::FromBase64String($parts[2]) } catch { throw 'Invalid ED25519 host key.' }
    # SSH wire encoding: length(11), "ssh-ed25519", length(32), 32 public bytes.
    if ($key.Length -ne 51 -or [BitConverter]::ToString($key[0..3]) -cne '00-00-00-0B' -or
        [Text.Encoding]::ASCII.GetString($key,4,11) -cne 'ssh-ed25519' -or
        [BitConverter]::ToString($key[15..18]) -cne '00-00-00-20') { throw 'Host key is not an ED25519 wire key.' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $fingerprint = 'SHA256:' + [Convert]::ToBase64String($sha.ComputeHash($key)).TrimEnd('=') }
    finally { $sha.Dispose() }
    if ($fingerprint -cne $HostKeyFingerprint) { throw 'Pinned ED25519 host fingerprint mismatch.' }
    $options = @('-F','NUL','-o','BatchMode=yes','-o','StrictHostKeyChecking=yes',
        '-o',('UserKnownHostsFile="' + $file.FullName.Replace('\','/') + '"'),'-o','GlobalKnownHostsFile=NUL',
        '-o','HostKeyAlgorithms=ssh-ed25519','-o','UpdateHostKeys=no',
        '-o','PasswordAuthentication=no','-o','KbdInteractiveAuthentication=no',
        '-o','ConnectTimeout=15','-o','ServerAliveInterval=15','-o','ServerAliveCountMax=2',
        '-o','ClearAllForwardings=yes','-o','RequestTTY=no')
    if ($IdentityFile) {
        $identity = Assert-ReleaseFile $IdentityFile
        # This is a filename, never private key content or a password.
        $options += @('-i',$identity.FullName,'-o','IdentitiesOnly=yes')
    }
    return $options
}

function Invoke-ReleaseProcess([string]$Executable, [string[]]$Arguments, [int]$TimeoutSeconds = 300) {
    if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) { throw 'Required release transport executable is missing.' }
    return [WordSuggestor.Release.ProcessRunner]::Run($Executable,$Arguments,$TimeoutSeconds)
}

function Read-PublisherResult($Result, [string]$Operation, [string]$ExpectedVersion) {
    if ($Result.ExitCode -eq 255) { throw 'SSH connection/authentication/host-key verification failed; staging is not reused.' }
    try { $json = $Result.Output.Trim() | ConvertFrom-Json } catch { throw 'Publisher returned invalid structured JSON.' }
    if ($null -eq $json -or $json -is [array] -or $json.ok -isnot [bool] -or $json.code -isnot [string]) {
        throw 'Publisher response is not a structured result.'
    }
    $codes = @{ 2='VALIDATION_ERROR'; 3='CONFLICT'; 4='INTEGRITY_ERROR'; 5='PUBLICATION_ERROR';
        6='HTTPS_VERIFICATION_ERROR'; 7='CONFIGURATION_ERROR'; 8='NOT_FOUND'; 9='INTERNAL_ERROR' }
    if ($Result.ExitCode -ne 0) {
        if (-not $codes.ContainsKey([int]$Result.ExitCode) -or $json.ok -or $json.code -cne $codes[[int]$Result.ExitCode]) {
            throw 'Publisher exit code and JSON result disagree.'
        }
        throw ('Publisher rejected operation: ' + $json.code + '. New attempts require a new staging ID.')
    }
    if (-not $json.ok -or $json.code -cne 'OK' -or $json.operation -cne $Operation -or
        $json.target.platform -cne 'windows' -or $json.target.architecture -cne 'x64' -or
        $json.target.channel -cne 'beta' -or $json.target.package_version -cne $ExpectedVersion) {
        throw 'Publisher success response does not match the requested operation/target.'
    }
    if ($Operation -eq 'validate' -and ($json.validated -isnot [bool] -or -not $json.validated)) {
        throw 'Publisher did not confirm staging validation.'
    }
    return $json
}

function Invoke-PrivateReleaseStaging($Candidate, $Endpoint, [string]$Operation, [string]$StagingId) {
    if (@('validate','register','publish') -cnotcontains $Operation -or
        $StagingId -cnotmatch '^windows-x64-beta-[0-9.]+-[a-f0-9-]{36}$') { throw 'Invalid staging operation or unique ID.' }
    $options = @(Get-PinnedSshOptions $Endpoint.HostName $Endpoint.Port $Endpoint.UserName $Endpoint.KnownHostsFile $Endpoint.HostKeyFingerprint $Endpoint.IdentityFile)
    $r = $Candidate.Manifest.release
    if ($Endpoint.BackendDirectory -cnotmatch '^/[A-Za-z0-9_./-]+$' -or
        @($Endpoint.BackendDirectory.Split('/') | Where-Object { $_ -eq '..' -or $_ -eq '.' }).Count) { throw 'Unsafe server command directory.' }
    if (($Operation -eq 'register' -and $r.status -cne 'pending') -or
        ($Operation -eq 'publish' -and ($r.status -cne 'active' -or -not $r.signing.signature_verified))) {
        throw 'Private staging operation does not match a truthful release state.'
    }
    $temporary = '/data/docker_data/volumes/wordsuggestorbackend_beta_staging/' + $StagingId + '.uploading'
    $ready = '/data/docker_data/volumes/wordsuggestorbackend_beta_staging/' + $StagingId
    $target = $Endpoint.UserName + '@' + $Endpoint.HostName
    function Remote([string]$Command) {
        Invoke-ReleaseProcess $Endpoint.SshPath ($options + @('-p',[string]$Endpoint.Port,'-T',$target,$Command)) $Endpoint.TimeoutSeconds
    }
    $created = Remote "test ! -e '$temporary' && test ! -e '$ready' && mkdir -m 700 -- '$temporary'"
    if ($created.ExitCode -ne 0) { throw 'Cannot create fresh private staging directory. No staging ID is reused.' }
    $batch = Join-Path ([IO.Path]::GetTempPath()) ('wordsuggestor-sftp-' + [guid]::NewGuid().ToString('N') + '.txt')
    try {
        $lines = foreach ($filename in @('release-manifest.json',$r.artifact.filename,$r.update_feed.filename)) {
            $local = (Join-Path $Candidate.Directory $filename).Replace('\','/')
            if ($local -match '["\r\n]') { throw 'SFTP input path contains unsupported control characters.' }
            'put "' + $local + '" "' + $temporary + '/' + $filename + '"'
        }
        [IO.File]::WriteAllLines($batch,$lines,[Text.UTF8Encoding]::new($false))
        $transfer = Invoke-ReleaseProcess $Endpoint.SftpPath ($options + @('-P',[string]$Endpoint.Port,'-b',$batch,$target)) $Endpoint.TimeoutSeconds
        if ($transfer.ExitCode -ne 0) { throw 'SFTP transfer failed; partial staging remains private and is never reused.' }
    } finally { if (Test-Path -LiteralPath $batch) { Remove-Item -LiteralPath $batch -Force } }
    $renamed = Remote "test ! -e '$ready' && mv -T -n -- '$temporary' '$ready' && test ! -e '$temporary' && test -d '$ready'"
    if ($renamed.ExitCode -ne 0) { throw 'Atomic staging completion failed; do not reuse this staging ID.' }
    $result = $null
    $operations = if ($Operation -eq 'validate') { @('validate') } else { @('validate',$Operation) }
    foreach ($name in $operations) {
        $command = "cd '$($Endpoint.BackendDirectory)' && ./scripts/publish_beta_release_on_docker01.sh $name --staging-id '$StagingId'"
        $result = Read-PublisherResult (Remote $command) $name $r.package_version
    }
    return [pscustomobject]@{ staging_id=$StagingId; result=$result }
}
