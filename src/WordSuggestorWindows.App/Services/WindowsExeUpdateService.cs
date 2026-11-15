using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed record ExeUpdateOffer(Version PackageVersion, string DisplayVersion, string FileName, long Size, string Sha256);
public sealed record ExeUpdateCheck(string Code, ExeUpdateOffer? Offer = null);

// Download a complete signed installer and hand it to the established installer.
// No app-file patching, silent installation or arbitrary URLs/commands.
public sealed class WindowsExeUpdateService : IDisposable
{
    public const string ArtifactBase = "https://wordsuggestor.com/beta-artifacts/windows/x64/beta/";
    public const string FeedUrl = ArtifactBase + "WordSuggestor.Beta.update.json";
    private readonly HttpClient _http;
    private readonly Func<string, string, CancellationToken, Task<bool>> _trust;
    private readonly Action<string> _launch;
    private readonly Func<string, Version, bool> _versionMatches;
    private readonly string _cache;
    public DirectInstallation Installation { get; }
    public WindowsExeUpdateService(DirectInstallation installation, HttpMessageHandler? handler = null,
        Func<string, string, CancellationToken, Task<bool>>? trust = null, Action<string>? launch = null, string? cache = null,
        Func<string, Version, bool>? versionMatches = null)
    {
        Installation = installation;
        _http = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = TimeSpan.FromSeconds(15) };
        _trust = trust ?? VerifyTrustAsync;
        _launch = launch ?? (path => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }));
        _versionMatches = versionMatches ?? VerifyInstallerVersion;
        _cache = cache ?? Path.Combine(WindowsDataPaths.Root, "updates");
    }
    public async Task<ExeUpdateCheck> CheckAsync(CancellationToken token)
    {
        if (!Installation.UpdatesEnabled || Installation.IsPreview) return new("local_preview");
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await _http.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode == HttpStatusCode.NotFound) return new("unpublished");
            if (!response.IsSuccessStatusCode) return new("unavailable");
            if (response.Content.Headers.ContentLength is > 32768) return new("invalid_feed");
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream(); var bytes = new byte[4096]; int n;
            while ((n = await stream.ReadAsync(bytes, deadline.Token)) != 0) { if (buffer.Length + n > 32768) return new("invalid_feed"); buffer.Write(bytes, 0, n); }
            var offer = ValidateFeed(buffer.ToArray(), Installation);
            return offer.PackageVersion > Version.Parse(Installation.PackageVersion) ? new("available", offer) : new("current");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return new("offline"); }
        catch (HttpRequestException) { return new("offline"); }
        catch (IOException) { return new("offline"); }
        catch (Exception e) when (e is JsonException or InvalidDataException or InvalidOperationException or FormatException or KeyNotFoundException or OverflowException) { return new("invalid_feed"); }
    }
    public static ExeUpdateOffer ValidateFeed(byte[] bytes, DirectInstallation installation)
    {
        if (bytes.Length > 32768) throw new InvalidDataException();
        using var json = JsonDocument.Parse(bytes);
        var r = json.RootElement;
        var fields = new[] { "schema_version", "platform", "architecture", "channel", "package_version", "version", "filename", "size_bytes", "sha256", "publisher" };
        if (r.ValueKind != JsonValueKind.Object || r.EnumerateObject().Count() != fields.Length || r.EnumerateObject().Any(p => !fields.Contains(p.Name)) || r.EnumerateObject().Select(p => p.Name).Distinct().Count() != fields.Length ||
            r.GetProperty("schema_version").GetInt32() != 1 || r.GetProperty("platform").GetString() != "windows" || r.GetProperty("architecture").GetString() != "x64" || r.GetProperty("channel").GetString() != "beta" || r.GetProperty("publisher").GetString() != installation.Publisher) throw new InvalidDataException();
        var numeric = r.GetProperty("package_version").GetString() ?? "";
        var display = r.GetProperty("version").GetString() ?? "";
        var file = r.GetProperty("filename").GetString() ?? "";
        var sha = r.GetProperty("sha256").GetString() ?? "";
        var size = r.GetProperty("size_bytes").GetInt64();
        if (!Regex.IsMatch(numeric, @"^(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})$") || !Version.TryParse(numeric, out var version) ||
            new[] { version.Major, version.Minor, version.Build, version.Revision }.Any(v => v > 65535) ||
            file != "WordSuggestor.Beta_" + numeric + "_x64.exe" || !Regex.IsMatch(sha, "^[a-f0-9]{64}$") || size is <= 0 or > 1073741824 || display.Length is 0 or > 128 || display.Any(char.IsControl)) throw new InvalidDataException();
        return new(version, display, file, size, sha);
    }
    public async Task<IWindowsInstallerLauncher> DownloadVerifiedAsync(ExeUpdateOffer expected, CancellationToken token)
    {
        var fresh = await CheckAsync(token);
        if (fresh.Code != "available" || fresh.Offer != expected) throw new InvalidDataException("Update offer changed; check again.");
        Directory.CreateDirectory(_cache);
        var path = Path.Combine(_cache, Guid.NewGuid().ToString("N") + ".exe");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromMinutes(5));
            using var response = await _http.GetAsync(ArtifactBase + expected.FileName, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentLength is { } length && length != expected.Size) throw new InvalidDataException("Installer response rejected.");
            await using (var source = await response.Content.ReadAsStreamAsync(timeout.Token))
            await using (var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
            {
                var buffer = new byte[65536]; long count = 0; int n;
                while ((n = await source.ReadAsync(buffer, timeout.Token)) != 0) { count += n; if (count > expected.Size) throw new InvalidDataException("Installer size exceeded."); await target.WriteAsync(buffer.AsMemory(0, n), timeout.Token); }
                if (count != expected.Size) throw new InvalidDataException("Installer size mismatch.");
            }
            VerifyHash(path, expected.Sha256);
            if (!_versionMatches(path, expected.PackageVersion)) throw new InvalidDataException("Installer numeric version mismatch.");
            if (!await _trust(path, Installation.Publisher, timeout.Token)) throw new InvalidDataException("Installer publisher/signature is not trusted.");
            return new ExecutableLauncher(path, expected.Sha256, Installation.Publisher, _trust, _launch);
        }
        catch (OperationCanceledException error) when (!token.IsCancellationRequested)
        { if (File.Exists(path)) File.Delete(path); throw new IOException("Installer download timed out.", error); }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }
    private static void VerifyHash(string path, string expected)
    { using var file = File.OpenRead(path); if (Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant() != expected) throw new InvalidDataException("Installer checksum mismatch."); }
    public static bool VerifyInstallerVersion(string path, Version expected)
    {
        var info = FileVersionInfo.GetVersionInfo(path);
        return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart) == expected;
    }
    public static async Task<bool> VerifyTrustAsync(string path, string publisher, CancellationToken token)
    {
        try { return await VerifyTrustCoreAsync(path, publisher, token); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return false; }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException) { return false; }
    }
    private static async Task<bool> VerifyTrustCoreAsync(string path, string publisher, CancellationToken token)
    {
        var encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(Path.GetFullPath(path)));
        var script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';try{$p=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encodedPath + "'));$s=Get-AuthenticodeSignature -LiteralPath $p;@{valid=($s.Status -eq 'Valid');publisher=$(if($s.SignerCertificate){$s.SignerCertificate.Subject}else{''})}|ConvertTo-Json -Compress}catch{exit 1}";
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-EncodedCommand"); start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
        using var process = Process.Start(start) ?? throw new InvalidOperationException();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            async Task<string> Read(StreamReader reader)
            { var buffer = new char[1024]; var result = new StringBuilder(); int n; while ((n = await reader.ReadAsync(buffer, deadline.Token)) > 0) { if (result.Length + n > 8192) throw new InvalidDataException(); result.Append(buffer, 0, n); } return result.ToString(); }
            var stdout = Read(process.StandardOutput); var stderr = Read(process.StandardError);
            await process.WaitForExitAsync(deadline.Token);
            if (process.ExitCode != 0 || (await stderr).Length != 0) return false;
            using var json = JsonDocument.Parse(await stdout);
            return json.RootElement.GetProperty("valid").GetBoolean() && json.RootElement.GetProperty("publisher").GetString() == publisher;
        }
        finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
    }
    private sealed class ExecutableLauncher(string path, string hash, string publisher, Func<string, string, CancellationToken, Task<bool>> trust, Action<string> launch) : IWindowsInstallerLauncher
    {
        public void Open()
        {
            try
            {
                // Hold a no-write/no-delete handle across verification and launch.
                using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (Convert.ToHexString(SHA256.HashData(locked)).ToLowerInvariant() != hash || !trust(path, publisher, CancellationToken.None).GetAwaiter().GetResult()) throw new InvalidOperationException("Installer changed or signature is no longer trusted.");
                launch(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or OperationCanceledException or JsonException) { throw new InvalidOperationException("Installer verification is unavailable.", e); }
        }
    }
    public void Dispose() => _http.Dispose();
}

internal static class ExeUpdateInteraction
{
    private static int _active;
    public static bool TryEnter() => Interlocked.CompareExchange(ref _active, 1, 0) == 0;
    public static void Exit() => Interlocked.Exchange(ref _active, 0);
}
