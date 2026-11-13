using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class AccountSessionCoordinator : INotifyPropertyChanged, IDisposable
{
    private readonly IAccountBackend _backend;
    private readonly IAccountSessionVault _vault;
    private readonly IAccountSecretStore _secrets;
    private readonly IHelloSessionKey _hello;
    private readonly string _device;
    private CancellationTokenSource _lifetime = new();
    private long _generation;
    private CancellationTokenSource _sharingLifetime = new();
    public string Origin => _backend.Origin;
    private AccountTokens? _tokens;
    private AccountRecovery? _recovery;
    private DateTimeOffset _expires;
    private bool _disposed;
    private Microsoft.Win32.SessionSwitchEventHandler? _sessionSwitch;
    public event PropertyChangedEventHandler? PropertyChanged;
    public AccountState State { get; private set; } = AccountState.SignedOut;
    public bool Busy { get; private set; }
    public string StatusCode { get; private set; } = "signed_out";
    public bool HelloAvailable { get; private set; }
    public bool HelloEnabled { get; private set; }
    public bool HasRecovery => _recovery is not null;
    public bool IsSignedIn => State == AccountState.SignedIn && _tokens is not null && DateTimeOffset.UtcNow < _expires;
    public AccountConfig Config { get; private set; } = new([]);
    public AccountProfile? Profile { get; private set; }
    public AccountPersonalization? Personalization { get; private set; }
    public AccountEntitlement? Entitlement { get; private set; }
    public DateTimeOffset ResendAfter { get; private set; }
    public bool EmailChangePending { get; private set; }
    public AccountSessionCoordinator(IAccountBackend backend, IAccountSessionVault vault, IAccountSecretStore secrets, IHelloSessionKey hello, string device)
    { _backend = backend; _vault = vault; _secrets = secrets; _hello = hello; _device = device; }
    public static AccountSessionCoordinator CreateDefault()
    {
        var backend = new AccountBackendClient(); var secrets = new WindowsAccountCredentials(backend.Origin + "|" + WindowsDataPaths.Root);
        var hello = new WindowsHelloSessionKey();
        string device;
        try
        {
            var directory = Path.Combine(WindowsDataPaths.Root, "account"); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "device-id-v1.txt");
            if (!File.Exists(path))
            {
                try { using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read); using var writer = new StreamWriter(file); writer.Write(Guid.NewGuid().ToString()); }
                catch (IOException) when (File.Exists(path)) { }
            }
            device = File.ReadAllText(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { device = ""; }
        var coordinator = new AccountSessionCoordinator(backend, new AccountSessionVault(secrets, hello, backend.Origin), secrets, hello, device);
        coordinator._sessionSwitch = (_, e) =>
        {
            if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionLock)
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(coordinator.Suspend);
        };
        Microsoft.Win32.SystemEvents.SessionSwitch += coordinator._sessionSwitch;
        return coordinator;
    }
    public Task<bool> InitializeAsync() => Run(async token =>
    {
        if (!Guid.TryParse(_device, out _)) throw new AccountFailure("credential_store");
        HelloEnabled = _vault.Policy == "hello";
        if (HelloEnabled && !IsSignedIn) State = AccountState.Locked;
        HelloAvailable = await _hello.IsAvailableAsync(token);
        Config = await Send<AccountConfig>(HttpMethod.Get, "/v1/client/config", null, token);
        if (Config.Flags is null) throw new AccountFailure("invalid_response");
        await FlushRevocations(token);
        if (IsSignedIn) return;
        try { _recovery = await _vault.LoadAsync(false, token); }
        catch (AccountFailure e) when (e.Code == "locked") { State = AccountState.Locked; throw; }
        if (_recovery is not null) await Refresh(token);
    });
    public Task<bool> LoginAsync(string email, string password) => Run(async token =>
    {
        if (!Guid.TryParse(_device, out _)) throw new AccountFailure("credential_store");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320 || !email.Contains('@') || string.IsNullOrEmpty(password)) throw new AccountFailure("invalid_input");
        await FlushRevocations(token);
        var result = await Send<AccountTokens>(HttpMethod.Post, "/v1/auth/token", new { email = email.Trim(), password, device_id = _device, client_version = WindowsUpdateService.DisplayVersion, auth_contract_version = "refresh_session_v1" }, token);
        if (_vault.Policy == "hello") _ = await _vault.LoadAsync(true, token);
        await AcceptTokens(result, null, token); await LoadProfile(token);
    });
    public Task<bool> RegisterAsync(string email, string password, string confirm) => Run(async token =>
    {
        if (IsSignedIn || !Config.Supports("email_verification_v1_issuance")) throw new AccountFailure("unsupported");
        ValidatePassword(password, confirm); if (!email.Contains('@') || email.Length > 320) throw new AccountFailure("invalid_input");
        var result = await Send<VerificationPending>(HttpMethod.Post, "/v1/auth/registrations", new { email, password, registrationContractVersion = "email_verification_v1", locale = SettingsText.ResolvedLocale, deviceId = _device }, token);
        SetPending(result); State = AccountState.SignedOut;
    }, "verification_pending");
    public Task<bool> ResendAsync(string email) => Run(async token =>
    {
        if (!Config.Supports("email_verification_v1_issuance") || DateTimeOffset.UtcNow < ResendAfter) throw new AccountFailure("cooldown");
        if (!email.Contains('@') || email.Length > 320) throw new AccountFailure("invalid_input");
        SetPending(await Send<VerificationPending>(HttpMethod.Post, "/v1/auth/email-verification/requests", new { email, locale = SettingsText.ResolvedLocale, deviceId = _device }, token));
    }, "verification_pending");
    public Task<bool> UnlockAsync() => Run(async token =>
    { _recovery = await _vault.LoadAsync(true, token) ?? throw new AccountFailure("login_required"); await Refresh(token); });
    public Task<bool> ReloadProfileAsync() => Run(async token => { await EnsureAccess(token); await LoadProfile(token); });
    public Task<bool> SaveProfileAsync(string name, string? age, string? profession, string? industry, bool consent) => Run(async token =>
    {
        static bool Allowed(string[] ids, string? value, string? current) => ids.Contains(value ?? "") || value == current;
        if (name.Length > 255 || !Allowed(AccountCatalogs.Ages, age, Personalization?.AgeBand) || !Allowed(AccountCatalogs.Professions, profession, Personalization?.ProfessionCode) || !Allowed(AccountCatalogs.Industries, industry, Personalization?.IndustryCode)) throw new AccountFailure("invalid_input");
        await EnsureAccess(token); var previous = Personalization ?? throw new AccountFailure("unavailable");
        var saved = await Send<AccountProfile>(HttpMethod.Put, "/v1/client/profile", new { displayName = name.Trim() }, token, true);
        ValidateProfile(saved);
        // Two existing backend transactions: report partial success honestly if
        // personalization fails; keep the edited draft and reload before retry.
        Profile = saved;
        try
        {
            var result = await Send<AccountPersonalization>(HttpMethod.Put, "/v1/client/profile/personalization", new { profileVersion = previous.ProfileVersion, ageBand = Empty(age), professionCode = Empty(profession), industryCode = Empty(industry), personalizationEnabled = consent }, token, true);
            ValidatePersonalization(result); Personalization = result;
        }
        catch (AccountFailure e) { throw new AccountFailure(e.Code == "conflict" ? "profile_conflict" : "profile_partial", e.Status); }
    }, "profile_saved");
    public Task<bool> ChangePasswordAsync(string current, string password, string confirm) => Run(async token =>
    {
        ValidatePassword(password, confirm); if (current.Length == 0) throw new AccountFailure("invalid_input");
        await EnsureAccess(token); await Send<bool>(HttpMethod.Put, "/v1/auth/password", new { currentPassword = current, newPassword = password }, token, true);
        _vault.Clear(); ClearMemory(); State = AccountState.ReauthenticationRequired;
    }, "password_changed");
    public Task<bool> ChangeEmailAsync(string email, string confirm, string password) => Run(async token =>
    {
        if (!Config.Supports("email_change_v1")) throw new AccountFailure("unsupported");
        if (email != confirm || !email.Contains('@') || email.Length > 320 || password.Length == 0) throw new AccountFailure("invalid_input");
        await EnsureAccess(token);
        var result = await Send<VerificationPending>(HttpMethod.Post, "/v1/auth/email-change/requests", new { new_email = email, current_password = password, locale = SettingsText.ResolvedLocale, device_id = _device }, token, true);
        if (result.Status != "change_pending") throw new AccountFailure("invalid_response"); EmailChangePending = true;
    }, "email_change_pending");
    public Task<bool> CancelEmailChangeAsync() => Run(async token =>
    { await EnsureAccess(token); await Send<bool>(HttpMethod.Delete, "/v1/auth/email-change", null, token, true); EmailChangePending = false; });
    public Task<bool> SetHelloAsync(bool enabled) => Run(async token =>
    {
        await EnsureAccess(token);
        if (_recovery is null || enabled && !HelloAvailable) throw new AccountFailure("hello_unavailable");
        await _vault.SetHelloAsync(enabled, _recovery, token); HelloEnabled = _vault.Policy == "hello";
    }, "protection_saved");
    public Task<bool> LogoutAsync(bool allDevices) => Run(async token =>
    {
        if (allDevices)
        {
            await EnsureAccess(token); await Send<bool>(HttpMethod.Delete, "/v1/auth/sessions", null, token, true);
            _vault.Clear(); ClearMemory(); State = AccountState.SignedOut; return;
        }
        // Clear locally even offline. The revocation-only secret cannot restore
        // the session; keep it in Credential Manager until server acknowledgement.
        AccountFailure? failure = null;
        var revocation = _vault.Revocation();
        try
        {
            if (_tokens is not null) await Send<bool>(HttpMethod.Delete, "/v1/auth/sessions/current", null, token, true);
            else if (revocation is { } r) await Send<bool>(HttpMethod.Post, "/v1/auth/session-revocations", new { session_id = r.SessionId, session_revocation_secret = r.Secret }, token);
        }
        catch (AccountFailure e) { failure = e; }
        try { if (failure is not null && revocation is { } r) QueueRevocation(r.SessionId, r.Secret); }
        finally { try { _vault.Clear(); } finally { ClearMemory(); State = AccountState.SignedOut; } }
        if (failure is not null) throw new AccountFailure("logout_pending");
    }, "signed_out");
    public void Lock()
    {
        if (_disposed || !HelloEnabled) return;
        _generation++; _lifetime.Cancel(); _lifetime.Dispose(); _lifetime = new();
        ClearMemory(); _vault.Lock(); State = AccountState.Locked; StatusCode = "locked"; Notify();
    }
    public void Suspend()
    {
        if (_disposed) return;
        if (HelloEnabled) { Lock(); return; }
        _generation++; _lifetime.Cancel(); _lifetime.Dispose(); _lifetime = new();
    }
    private async Task EnsureAccess(CancellationToken token)
    {
        if (_tokens is null || DateTimeOffset.UtcNow >= _expires - TimeSpan.FromSeconds(30))
        { if (_recovery is null) throw new AccountFailure("login_required"); await Refresh(token, false); }
        if (!IsSignedIn) throw new AccountFailure("login_required");
    }
    private async Task Refresh(CancellationToken token, bool loadProfile = true)
    {
        var recovery = _recovery ?? throw new AccountFailure("login_required");
        if (recovery.IdleExpiry <= DateTimeOffset.UtcNow || recovery.AbsoluteExpiry <= DateTimeOffset.UtcNow) { _vault.Clear(); ClearMemory(); throw new AccountFailure("login_required"); }
        recovery = recovery with { RefreshRequestId = recovery.RefreshRequestId ?? Guid.NewGuid().ToString() };
        await _vault.SaveAsync(recovery, token); _recovery = recovery; // durable idempotency before network
        try
        {
            var result = await Send<AccountTokens>(HttpMethod.Post, "/v1/auth/refresh", new { refresh_token = recovery.RefreshToken, session_id = recovery.SessionId, device_id = recovery.DeviceId }, token, idempotency: recovery.RefreshRequestId);
            if (result.SessionId != recovery.SessionId || result.User is null || result.User.Id != recovery.UserId) throw new AccountFailure("invalid_response");
            await AcceptTokens(result, recovery, token);
            if (loadProfile) await LoadProfile(token);
        }
        catch (AccountFailure e) when (e.Status is 401 or 403)
        { _vault.Clear(); ClearMemory(); throw new AccountFailure("login_required"); }
    }
    private async Task AcceptTokens(AccountTokens result, AccountRecovery? previous, CancellationToken token)
    {
        if (result.User is null || result.User.Id <= 0 || result.User.Email is null || !result.User.Email.Contains('@') || result.TokenType != "Bearer" ||
            string.IsNullOrWhiteSpace(result.AccessToken) || result.AccessToken.Any(c => c < 33 || c > 126) || result.AccessToken.Length > 16384 || result.ExpiresIn is < 1 or > 86400) throw new AccountFailure("invalid_response");
        AccountRecovery? recovery = null;
        if (result.AuthContractVersion == "refresh_session_v1")
        {
            if (!Guid.TryParse(result.SessionId, out _) || string.IsNullOrWhiteSpace(result.RefreshToken) || result.RefreshToken.Length > 1024 ||
                result.RefreshIdleExpiresAt is not { } idle || result.RefreshAbsoluteExpiresAt is not { } absolute || idle <= DateTimeOffset.UtcNow || absolute < idle ||
                string.IsNullOrWhiteSpace(result.SessionRevocationSecret ?? previous?.RevocationSecret)) throw new AccountFailure("invalid_response");
            recovery = new(1, _backend.Origin, previous?.DeviceId ?? _device, result.RefreshToken, result.SessionId!, result.SessionRevocationSecret ?? previous!.RevocationSecret, idle, absolute, result.User.Id);
            await _vault.SaveAsync(recovery, token);
        }
        else if (result.AuthContractVersion is not null || result.RefreshToken is not null) throw new AccountFailure("invalid_response");
        else _vault.Clear(); // Legacy access-only session is never silently persisted.
        _recovery = recovery; _tokens = result; _expires = DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn); State = AccountState.SignedIn;
    }
    private async Task LoadProfile(CancellationToken token)
    {
        var profile = await Send<AccountProfile>(HttpMethod.Get, "/v1/client/profile", null, token, true); ValidateProfile(profile); Profile = profile;
        try { var value = await Send<AccountPersonalization>(HttpMethod.Get, "/v1/client/profile/personalization", null, token, true); ValidatePersonalization(value); Personalization = value; }
        catch (AccountFailure e) when (e.Status is 403 or 404) { Personalization = null; }
        try { Entitlement = (await Send<AccountEntitlements>(HttpMethod.Get, "/v1/client/entitlements", null, token, true)).Entitlement; }
        catch (AccountFailure e) when (e.Status is 403 or 404) { Entitlement = null; }
    }
    private void ValidateProfile(AccountProfile profile)
    { if (_tokens is null || profile.UserId != _tokens.User.Id || profile.Email != _tokens.User.Email || profile.DisplayName?.Length > 255) throw new AccountFailure("invalid_response"); }
    private static void ValidatePersonalization(AccountPersonalization p)
    { if (p.SchemaName != "profile_personalization_v1" || p.ProfileVersion < 0) throw new AccountFailure("invalid_response"); }
    public async Task SubmitUnknownWordHandoffAsync(JsonElement payload,string idempotencyKey,CancellationToken token)
    {
        if(!UnknownWordSharingDeployment.Qualified||UnknownWordSharingDeployment.EndpointPath is not {} path||!Config.Supports(UnknownWordSharingDeployment.Capability))throw new AccountFailure("unsupported");
        if(!IsSignedIn||Busy)throw new AccountFailure("login_required");
        var identity=_tokens;
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,_lifetime.Token,_sharingLifetime.Token);
        await Send<bool>(HttpMethod.Post,path,payload,linked.Token,true,idempotencyKey);
        if(!ReferenceEquals(identity,_tokens)||!IsSignedIn)throw new OperationCanceledException();
    }
    private async Task<T> Send<T>(HttpMethod method, string path, object? body, CancellationToken token, bool authenticated = false, string? idempotency = null)
    {
        var result = await _backend.SendAsync<T>(method, path, body, authenticated ? _tokens?.AccessToken ?? throw new AccountFailure("login_required") : null, token, idempotency);
        token.ThrowIfCancellationRequested(); return result;
    }
    private void SetPending(VerificationPending result)
    { if (result.Status != "verification_pending" || result.ResendAfterSeconds is < 0 or > 86400) throw new AccountFailure("invalid_response"); ResendAfter = DateTimeOffset.UtcNow.AddSeconds(result.ResendAfterSeconds); }
    private static string? Empty(string? s) => string.IsNullOrEmpty(s) ? null : s;
    private static void ValidatePassword(string password, string confirm)
    { if (password != confirm || password.Length is < 12 or > 1024 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)) throw new AccountFailure("invalid_input"); }
    private void ClearMemory() { _tokens = null; _recovery = null; Profile = null; Personalization = null; Entitlement = null; EmailChangePending = false; }
    private sealed record Revocation(string SessionId, string RevocationSecret);
    private void QueueRevocation(string sessionId, string secret)
    {
        var current = ReadRevocations().Where(x => x.SessionId != sessionId).Append(new(sessionId, secret)).ToArray();
        if (current.Length > 4) throw new AccountFailure("credential_store");
        _secrets.Write("revocations", JsonSerializer.SerializeToUtf8Bytes(current, AccountBackendClient.Web));
    }
    private Revocation[] ReadRevocations()
    {
        var data = _secrets.Read("revocations"); if (data is null) return [];
        try
        {
            var pending = JsonSerializer.Deserialize<Revocation[]>(data, AccountBackendClient.Web);
            if (pending is null || pending.Length > 4 || pending.Any(r => r is null || !Guid.TryParse(r.SessionId, out _) || string.IsNullOrWhiteSpace(r.RevocationSecret) || r.RevocationSecret.Length > 1024)) throw new AccountFailure("credential_store");
            return pending;
        }
        finally { CryptographicOperations.ZeroMemory(data); }
    }
    private async Task FlushRevocations(CancellationToken token)
    {
        foreach (var r in ReadRevocations()) await Send<bool>(HttpMethod.Post, "/v1/auth/session-revocations", new { session_id = r.SessionId, session_revocation_secret = r.RevocationSecret }, token);
        _secrets.Delete("revocations");
    }
    private async Task<bool> Run(Func<CancellationToken, Task> action, string success = "ready")
    {
        if (_disposed || Busy) return false;
        _sharingLifetime.Cancel();_sharingLifetime.Dispose();_sharingLifetime=new();
        Busy = true; var generation = _generation; var token = _lifetime.Token; StatusCode = "connecting"; Notify();
        try { await action(token); token.ThrowIfCancellationRequested(); if (generation != _generation) return false; StatusCode = success; return true; }
        catch (OperationCanceledException) { if (generation == _generation) StatusCode = "cancelled"; return false; }
        catch (Exception e) when (e is AccountFailure or IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            if (generation != _generation) return false;
            StatusCode = e is AccountFailure failure ? failure.Code : "credential_store";
            if (e is AccountFailure { Status: 401 } || StatusCode == "invalid_response")
            {
                try { _vault.Clear(); } catch (AccountFailure) { StatusCode = "credential_store"; }
                finally { ClearMemory(); State = AccountState.ReauthenticationRequired; }
            }
            if (StatusCode == "offline" && _recovery is not null) { _tokens = null; State = AccountState.OfflineRecoverable; }
            if (StatusCode == "login_required") State = AccountState.ReauthenticationRequired;
            return false;
        }
        finally { Busy = false; Notify(); }
    }
    private void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    public void Dispose()
    { if (_disposed) return; _disposed = true; _sharingLifetime.Cancel();_sharingLifetime.Dispose(); _generation++; _lifetime.Cancel(); _lifetime.Dispose(); _vault.Dispose(); ClearMemory(); if (_sessionSwitch is not null) Microsoft.Win32.SystemEvents.SessionSwitch -= _sessionSwitch; if (_backend is IDisposable disposable) disposable.Dispose(); }
}
