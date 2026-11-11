using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface IAccountSessionVault : IDisposable
{
    string Policy { get; }
    Task<AccountRecovery?> LoadAsync(bool interactive, CancellationToken cancellation);
    Task SaveAsync(AccountRecovery recovery, CancellationToken cancellation);
    Task SetHelloAsync(bool enabled, AccountRecovery recovery, CancellationToken cancellation);
    void Clear(); void Lock();
    (string SessionId, string Secret)? Revocation();
}
public sealed class AccountSessionVault(IAccountSecretStore secrets, IHelloSessionKey hello, string origin) : IAccountSessionVault
{
    private byte[]? _key;
    private sealed record Envelope(int Version, string Policy, string? Name, string? Challenge, string Data, string? Nonce, string? Tag, string? SessionId = null, string? RevocationSecret = null);
    private Envelope? Read()
    {
        var bytes = secrets.Read("session"); if (bytes is null) return null;
        try
        {
            var value = JsonSerializer.Deserialize<Envelope>(bytes, AccountBackendClient.Web);
            if (value is null || value.Version != 1 || value.Policy is not ("automatic" or "hello" or "loginAtLaunch") || string.IsNullOrWhiteSpace(value.Data)) throw new AccountFailure("credential_store");
            return value;
        }
        catch (JsonException) { throw new AccountFailure("credential_store"); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    public string Policy => Read()?.Policy ?? "automatic";
    public (string SessionId, string Secret)? Revocation()
    {
        var e = Read();
        return e is { SessionId: { } id, RevocationSecret: { } secret } && Guid.TryParse(id, out _) && secret.Length > 0 ? (id, secret) : null;
    }
    public async Task<AccountRecovery?> LoadAsync(bool interactive, CancellationToken cancellation)
    {
        var envelope = Read(); if (envelope is null) return null;
        if (envelope.Policy == "loginAtLaunch") throw new AccountFailure("login_required");
        try
        {
            if (envelope.Policy == "hello" && _key is null)
            {
                if (!interactive) throw new AccountFailure("locked");
                var key = await hello.DeriveAsync(envelope.Name!, Convert.FromBase64String(envelope.Challenge!), false, cancellation);
                if (cancellation.IsCancellationRequested) { CryptographicOperations.ZeroMemory(key); cancellation.ThrowIfCancellationRequested(); }
                _key = key;
            }
            var data = envelope.Policy == "hello" ? Decrypt(envelope, _key!) : Convert.FromBase64String(envelope.Data);
            try
            {
                var recovery = JsonSerializer.Deserialize<AccountRecovery>(data, AccountBackendClient.Web);
                if (recovery is null || recovery.Version != 1 || recovery.Origin != origin || !Guid.TryParse(recovery.DeviceId, out _) ||
                    !Guid.TryParse(recovery.SessionId, out _) || recovery.UserId <= 0 || string.IsNullOrWhiteSpace(recovery.RefreshToken) || string.IsNullOrWhiteSpace(recovery.RevocationSecret)) throw new AccountFailure("credential_store");
                return recovery;
            }
            finally { CryptographicOperations.ZeroMemory(data); }
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException or ArgumentException)
        { Lock(); throw new AccountFailure("credential_store"); }
    }
    public Task SaveAsync(AccountRecovery recovery, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested(); var envelope = Read();
        var data = JsonSerializer.SerializeToUtf8Bytes(recovery, AccountBackendClient.Web);
        try
        {
            var next = envelope?.Policy == "hello" ? Encrypt(envelope with { SessionId = recovery.SessionId, RevocationSecret = recovery.RevocationSecret }, data, _key ?? throw new AccountFailure("locked"))
                : new Envelope(1, envelope?.Policy ?? "automatic", null, null, Convert.ToBase64String(data), null, null, recovery.SessionId, recovery.RevocationSecret);
            Write(next); return Task.CompletedTask;
        }
        finally { CryptographicOperations.ZeroMemory(data); }
    }
    public async Task SetHelloAsync(bool enabled, AccountRecovery recovery, CancellationToken cancellation)
    {
        if (!enabled)
        {
            var oldName = Read()?.Name;
            // Caller must hold an already unlocked, backend-confirmed session.
            cancellation.ThrowIfCancellationRequested();
            var bytes = JsonSerializer.SerializeToUtf8Bytes(recovery, AccountBackendClient.Web);
            try { Write(new(1, "automatic", null, null, Convert.ToBase64String(bytes), null, null, recovery.SessionId, recovery.RevocationSecret)); Lock(); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
            if (oldName is not null) await DeleteKey(oldName);
            return;
        }
        var name = "WordSuggestor.Session." + Guid.NewGuid().ToString("N"); var challenge = RandomNumberGenerator.GetBytes(32);
        byte[] key = []; var saved = false;
        try
        {
            key = await hello.DeriveAsync(name, challenge, true, cancellation);
            // Actual create -> reopen -> protected operation. Probe/mock success
            // alone never changes persisted protection or reports an enabled state.
            var reopened = await hello.DeriveAsync(name, challenge, false, cancellation);
            try { if (!CryptographicOperations.FixedTimeEquals(key, reopened)) throw new AccountFailure("hello_unavailable"); }
            finally { CryptographicOperations.ZeroMemory(reopened); }
            cancellation.ThrowIfCancellationRequested();
            var plain = JsonSerializer.SerializeToUtf8Bytes(recovery, AccountBackendClient.Web);
            try
            {
                var envelope = Encrypt(new(1, "hello", name, Convert.ToBase64String(challenge), "", null, null, recovery.SessionId, recovery.RevocationSecret), plain, key);
                var roundtrip = Decrypt(envelope, key);
                try { if (!CryptographicOperations.FixedTimeEquals(plain, roundtrip)) throw new AccountFailure("hello_unavailable"); }
                finally { CryptographicOperations.ZeroMemory(roundtrip); }
                cancellation.ThrowIfCancellationRequested(); Write(envelope); Lock(); _key = key; key = []; saved = true;
            }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        finally { CryptographicOperations.ZeroMemory(key); if (!saved) await DeleteKey(name); }
    }
    private byte[] Aad(Envelope e) => Encoding.UTF8.GetBytes("WordSuggestor/session/v1|" + origin + "|" + e.Policy + "|" + e.Name + "|" + e.Challenge + "|" + e.SessionId + "|" + e.RevocationSecret);
    private Envelope Encrypt(Envelope envelope, byte[] plain, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var cipher = new byte[plain.Length];
        using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plain, cipher, tag, Aad(envelope));
        return envelope with { Data = Convert.ToBase64String(cipher), Nonce = Convert.ToBase64String(nonce), Tag = Convert.ToBase64String(tag) };
    }
    private byte[] Decrypt(Envelope envelope, byte[] key)
    {
        var cipher = Convert.FromBase64String(envelope.Data); var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, 16); aes.Decrypt(Convert.FromBase64String(envelope.Nonce!), cipher, Convert.FromBase64String(envelope.Tag!), plain, Aad(envelope)); return plain;
    }
    private void Write(Envelope envelope)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, AccountBackendClient.Web);
        try { secrets.Write("session", bytes); } finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private async Task DeleteKey(string name)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await hello.DeleteAsync(name, timeout.Token); } catch (Exception e) when (e is AccountFailure or OperationCanceledException) { }
    }
    public void Clear() { var name = Read()?.Name; secrets.Delete("session"); Lock(); if (name is not null) _ = DeleteKey(name); }
    public void Lock() { if (_key is { } key) CryptographicOperations.ZeroMemory(key); _key = null; }
    public void Dispose() => Lock();
}
