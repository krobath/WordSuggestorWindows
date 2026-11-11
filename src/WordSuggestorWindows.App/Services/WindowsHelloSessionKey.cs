using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface IHelloSessionKey
{
    Task<bool> IsAvailableAsync(CancellationToken cancellation);
    Task<byte[]> DeriveAsync(string name, byte[] challenge, bool create, CancellationToken cancellation);
    Task DeleteAsync(string name, CancellationToken cancellation) => Task.CompletedTask;
}
public sealed class WindowsHelloSessionKey : IHelloSessionKey
{
    public async Task DeleteAsync(string name, CancellationToken cancellation)
    { _ = await RunAsync(new { operation = "delete", name }, cancellation); }
    public async Task<bool> IsAvailableAsync(CancellationToken cancellation)
    { try { return (await RunAsync(new { operation = "capability" }, cancellation)).Available; } catch (AccountFailure) { return false; } }
    public async Task<byte[]> DeriveAsync(string name, byte[] challenge, bool create, CancellationToken cancellation)
    {
        var result = await RunAsync(new { operation = create ? "create" : "open", name, challenge = Convert.ToBase64String(challenge) }, cancellation);
        if (!result.Available || result.Signature is null || result.PublicKey is null) throw new AccountFailure("hello_unavailable");
        byte[] signature = Convert.FromBase64String(result.Signature);
        try
        {
            using var rsa = RSA.Create(); rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(result.PublicKey), out _);
            if (rsa.KeySize < 2048 || !rsa.VerifyData(challenge, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new AccountFailure("hello_unavailable");
            // Fixed per-vault challenge + deterministic RSA/PKCS1 signature. The
            // signature is secret KDF input here: never persist or transmit it.
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, signature, 32, challenge, Encoding.UTF8.GetBytes("WordSuggestor/session-key/v1/" + name));
        }
        finally { CryptographicOperations.ZeroMemory(signature); }
    }
    private static async Task<Result> RunAsync(object request, CancellationToken cancellation)
    {
        using var stream = typeof(WindowsHelloSessionKey).Assembly.GetManifestResourceStream("WordSuggestorWindows.HelloWorker")!;
        var script = await new StreamReader(stream).ReadToEndAsync(cancellation);
        var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) }) info.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = info };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromSeconds(60));
        try
        {
            if (!process.Start()) throw new AccountFailure("hello_unavailable");
            var output = ReadBounded(process.StandardOutput, timeout.Token); var errors = ReadBounded(process.StandardError, timeout.Token);
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token); process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token); var data = await output; _ = await errors;
            if (process.ExitCode != 0) throw new AccountFailure("hello_unavailable");
            return JsonSerializer.Deserialize<Result>(data, AccountBackendClient.Web) ?? throw new AccountFailure("hello_unavailable");
        }
        catch (Exception ex) when (ex is IOException or JsonException or System.ComponentModel.Win32Exception or OperationCanceledException or InvalidOperationException)
        { if (cancellation.IsCancellationRequested) throw new OperationCanceledException(cancellation); throw new AccountFailure("hello_unavailable"); }
        finally { try { if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(2000); } } catch (InvalidOperationException) { } }
    }
    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken token)
    {
        var buffer = new char[1024]; var text = new StringBuilder(); int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) > 0) { if (text.Length + count > 16384) throw new IOException(); text.Append(buffer, 0, count); }
        return text.ToString();
    }
    private sealed record Result(bool Available, string? Signature, string? PublicKey);
}
