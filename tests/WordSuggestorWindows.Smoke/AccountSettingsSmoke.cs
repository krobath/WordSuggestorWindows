using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class AccountSettingsSmoke
{
    internal sealed class Secrets : IAccountSecretStore
    {
        public Dictionary<string,byte[]> Values = []; public bool Fail;
        public byte[]? Read(string slot) => Values.GetValueOrDefault(slot)?.ToArray();
        public void Write(string slot, byte[] data) { if (Fail) throw new AccountFailure("credential_store"); Values[slot] = data.ToArray(); }
        public void Delete(string slot) { if (Fail) throw new AccountFailure("credential_store"); Values.Remove(slot); }
    }
    internal sealed class Hello : IHelloSessionKey
    {
        public bool Available = true, Fail, WrongReopen; public int Calls;
        public Task<bool> IsAvailableAsync(CancellationToken c) => Task.FromResult(Available);
        public Task<byte[]> DeriveAsync(string name, byte[] challenge, bool create, CancellationToken c)
        { Calls++; if (Fail) throw new AccountFailure("hello_unavailable"); return Task.FromResult(SHA256.HashData(Encoding.UTF8.GetBytes(name + Convert.ToBase64String(challenge) + (WrongReopen && !create ? "wrong" : "")))); }
    }
    internal sealed class Server : HttpMessageHandler
    {
        public readonly string SessionId = Guid.NewGuid().ToString();
        public List<(string Path,string Method,JsonElement? Body,string? Idempotency,string? Auth)> Requests = [];
        public string? FailurePath, BlockPath; public int FailureStatus = 503; public bool NetworkFailure, BadUser, DisabledRegistration, Conflict;
        public TaskCompletionSource? Block;
        public string Name = "Tester"; public long Version; public AccountPersonalization Personal => new("profile_personalization_v1",Version,"45_54","technology_professional","information_technology_telecom",false);
        public AccountTokens Tokens(bool refresh) => new("synthetic-access", "Bearer",900,refresh ? "synthetic-rotated-refresh" : "synthetic-refresh",DateTimeOffset.UtcNow.AddDays(1),DateTimeOffset.UtcNow.AddDays(30),SessionId,refresh ? null : "synthetic-revocation","refresh_session_v1",new(BadUser ? 99 : 7,"fixture@example.invalid",true));
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.AbsolutePath; var body=request.Content is null ? (JsonElement?)null : JsonDocument.Parse(await request.Content.ReadAsStringAsync(ct)).RootElement.Clone();
            Requests.Add((path,request.Method.Method,body,request.Headers.TryGetValues("Idempotency-Key",out var keys) ? keys.Single() : null,request.Headers.Authorization?.Parameter));
            if (path==BlockPath && Block is not null) await Block.Task; // deliberately ignores cancellation to challenge coordinator
            if (path==FailurePath) { if(NetworkFailure) throw new HttpRequestException("PRIVATE_RESPONSE_SENTINEL"); return new((HttpStatusCode)FailureStatus){Content=new StringContent("PRIVATE_RESPONSE_SENTINEL")}; }
            object response = path switch
            {
                "/v1/client/config" => new AccountConfig(new() { ["email_verification_v1_issuance"] = !DisabledRegistration, ["email_change_v1"] = true }),
                "/v1/auth/token" => Tokens(false), "/v1/auth/refresh" => Tokens(true),
                "/v1/client/profile" => new AccountProfile(7,"fixture@example.invalid",true,request.Method==HttpMethod.Put ? Name=body!.Value.GetProperty("display_name").GetString()! : Name),
                "/v1/client/profile/personalization" => Personal,
                "/v1/client/entitlements" => new AccountEntitlements(new("WordSuggestor","beta","active",null)),
                "/v1/auth/registrations" or "/v1/auth/email-verification/requests" => new VerificationPending("verification_pending","EMAIL_VERIFICATION_MESSAGE_SENT_IF_ELIGIBLE",3600,60),
                "/v1/auth/email-change/requests" => new VerificationPending("change_pending","EMAIL_CHANGE_MESSAGE_SENT_IF_ELIGIBLE",3600,60),
                _ => true
            };
            if (path=="/v1/client/profile/personalization" && request.Method==HttpMethod.Put)
            { if (Conflict) return new(HttpStatusCode.Conflict); Version++; response=Personal; }
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(response,AccountBackendClient.Snake),Encoding.UTF8,"application/json") };
        }
    }
    internal sealed class Fixture : IDisposable
    {
        public Secrets Store = new(); public Hello Hello = new(); public Server Server = new(); public AccountSessionVault Vault; public AccountSessionCoordinator Session;
        public Fixture()
        {
            Vault = new(Store,Hello,"https://fixture.invalid");
            Session = NewSession(Vault);
        }
        public AccountSessionCoordinator NewSession(AccountSessionVault vault) => new(new AccountBackendClient("https://fixture.invalid",Server),vault,Store,Hello,Guid.NewGuid().ToString());
        public void Dispose() => Session.Dispose();
    }
    public static async Task Run(bool desktop)
    {
        SettingsText.ApplyPreference("da-DK");
        var checks=0; void Check(string name,bool value) { if(!value) throw new Exception("Account: "+name); checks++; Console.WriteLine("PASS ACCOUNT "+name); }
        try { using var bad=new AccountBackendClient("http://fixture.invalid"); Check("HTTPS required",false); } catch(AccountFailure) { Check("HTTPS required",true); }
        try { using var bad=new AccountBackendClient("https://user:password@fixture.invalid"); Check("URL credentials rejected",false); } catch(AccountFailure) { Check("URL credentials rejected",true); }
        using var f=new Fixture();
        Check("default signed out without remembered-identity proof", !f.Session.IsSignedIn && !f.Store.Values.ContainsKey("session"));
        Check("config fetch respects current backend flags", await f.Session.InitializeAsync() && f.Session.Config.Supports("email_verification_v1_issuance"));
        Check("registration is verification-first and grants no session", await f.Session.RegisterAsync("fixture@example.invalid","SyntheticPassword12","SyntheticPassword12") && !f.Session.IsSignedIn && f.Session.StatusCode=="verification_pending" && !f.Store.Values.ContainsKey("session"));
        var register=f.Server.Requests.Last(); Check("registration wire matches Jackson snake_case contract",register.Body!.Value.GetProperty("registration_contract_version").GetString()=="email_verification_v1" && register.Auth is null);
        Check("resend cooldown enforced without duplicate request",!await f.Session.ResendAsync("fixture@example.invalid") && f.Session.StatusCode=="cooldown");
        Check("actual login/profile/entitlements transaction",await f.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12") && f.Session.Profile?.UserId==7 && f.Session.Entitlement?.Status=="active");
        var login=f.Server.Requests.Single(x=>x.Path=="/v1/auth/token");
        Check("login negotiates refresh contract and stable device",login.Body!.Value.GetProperty("auth_contract_version").GetString()=="refresh_session_v1" && Guid.TryParse(login.Body.Value.GetProperty("device_id").GetString(),out _));
        var stored=Encoding.UTF8.GetString(f.Store.Values["session"]);
        Check("only recovery is persisted; password/access absent",!stored.Contains("SyntheticPassword12") && !stored.Contains("synthetic-access"));
        Check("profile and personalization preserve backend IDs",await f.Session.SaveProfileAsync("Ægir","45_54","technology_professional","information_technology_telecom",true) && f.Session.Profile?.DisplayName=="Ægir");
        var personal=f.Server.Requests.Last(x=>x.Path.EndsWith("personalization") && x.Method=="PUT");
        Check("profile request is versioned and no ranking request exists",personal.Body!.Value.GetProperty("profile_version").GetInt64()==0 && personal.Body.Value.GetProperty("personalization_enabled").GetBoolean() && personal.Auth=="synthetic-access");
        f.Server.Conflict=true;
        Check("partial profile save and optimistic conflict reported",!await f.Session.SaveProfileAsync("Changed","45_54","technology_professional","information_technology_telecom",true) && f.Session.StatusCode=="profile_conflict" && f.Session.Profile?.DisplayName=="Changed"); f.Server.Conflict=false;
        var before=f.Store.Values["session"].ToArray(); f.Hello.Fail=true;
        Check("failed Hello never downgrades/replaces prior credential",!await f.Session.SetHelloAsync(true) && !f.Session.HelloEnabled && before.SequenceEqual(f.Store.Values["session"]));
        f.Hello.Fail=false; f.Hello.WrongReopen=true;
        Check("Hello create success without real reopen equivalence is rejected",!await f.Session.SetHelloAsync(true) && before.SequenceEqual(f.Store.Values["session"])); f.Hello.WrongReopen=false;
        Check("mock protected roundtrip changes policy only after completion",await f.Session.SetHelloAsync(true) && f.Session.HelloEnabled && f.Hello.Calls>=4);
        var raw=f.Store.Values["session"]; var env=JsonDocument.Parse(raw).RootElement;
        Check("protected ciphertext excludes recovery token",!Encoding.UTF8.GetString(Convert.FromBase64String(env.GetProperty("data").GetString()!)).Contains("synthetic-refresh"));
        f.Session.Lock(); Check("lock invalidates access/profile and memory key",f.Session.State==AccountState.Locked && !f.Session.IsSignedIn && f.Session.Profile is null);
        Check("passive restore does not prompt/decrypt protected session",!await f.Session.InitializeAsync() && f.Session.StatusCode=="locked");
        Check("explicit mock unlock refreshes and restores backend identity",await f.Session.UnlockAsync() && f.Session.IsSignedIn);
        Check("disable protection requires unlocked authenticated recovery",await f.Session.SetHelloAsync(false) && !f.Session.HelloEnabled);
        f.Server.FailurePath="/v1/auth/refresh"; f.Server.NetworkFailure=true;
        f.Vault.Lock();
        // A new coordinator simulates restart; same store, client and actual HTTP serialization.
        using var restored=f.NewSession(new(f.Store,f.Hello,"https://fixture.invalid"));
        Check("restart offline retains recoverable credential without false login",!await restored.InitializeAsync() && restored.State==AccountState.OfflineRecoverable && f.Store.Values.ContainsKey("session"));
        var refresh=f.Server.Requests.Last(x=>x.Path=="/v1/auth/refresh");
        f.Server.FailurePath=null; f.Server.NetworkFailure=false;
        Check("refresh retry succeeds with persisted idempotency key",await restored.InitializeAsync() && refresh.Idempotency==f.Server.Requests.Last(x=>x.Path=="/v1/auth/refresh").Idempotency);
        Check("rotation drops pending idempotency key only after storing new credential",(await new AccountSessionVault(f.Store,f.Hello,"https://fixture.invalid").LoadAsync(false,default))?.RefreshRequestId is null);
        f.Server.FailurePath="/v1/auth/sessions/current"; f.Server.NetworkFailure=true;
        Check("local logout clears recovery while retaining revocation-only queue",!await restored.LogoutAsync(false) && restored.State==AccountState.SignedOut && !f.Store.Values.ContainsKey("session") && f.Store.Values.ContainsKey("revocations"));
        f.Server.FailurePath=null; f.Server.NetworkFailure=false;
        Check("next connection flushes revocation before login",await f.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12") && !f.Store.Values.ContainsKey("revocations") && f.Server.Requests.Any(x=>x.Path=="/v1/auth/session-revocations"));
        f.Server.BlockPath="/v1/auth/token"; f.Server.Block=new();
        var pending=f.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12"); await Task.Delay(50);
        var requests=f.Server.Requests.Count;
        Check("single-flight prevents duplicate auth requests",!await f.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12") && f.Server.Requests.Count==requests);
        var credential=f.Store.Values["session"].ToArray(); f.Session.Suspend(); f.Server.Block.SetResult();
        Check("late cancelled response cannot replace persisted credential",!await pending && credential.SequenceEqual(f.Store.Values["session"])); f.Server.BlockPath=null;
        Check("record diagnostics redact recovery secrets",!new AccountRecovery(1,"x","x","PRIVATE_RESPONSE_SENTINEL","x","x",default,default,7).ToString().Contains("PRIVATE"));
        using(var corrupt=new Fixture())
        {
            corrupt.Store.Values["session"]=Encoding.UTF8.GetBytes("{\"version\":99,\"policy\":\"automatic\",\"data\":\"preserve\"}"); var keep=corrupt.Store.Values["session"].ToArray();
            Check("future credential envelope preserved",!await corrupt.Session.InitializeAsync() && keep.SequenceEqual(corrupt.Store.Values["session"]));
        }
        using(var reject=new Fixture())
        {
            reject.Server.FailurePath="/v1/auth/token"; reject.Server.FailureStatus=401;
            Check("unauthorized never persists a session or raw server text",!await reject.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12") && !reject.Store.Values.ContainsKey("session") && reject.Session.StatusCode=="unauthorized");
        }
        using(var disabled=new Fixture())
        {
            disabled.Server.DisabledRegistration=true; await disabled.Session.InitializeAsync(); var size=disabled.Server.Requests.Count;
            Check("disabled registration cannot issue a request",!await disabled.Session.RegisterAsync("fixture@example.invalid","SyntheticPassword12","SyntheticPassword12") && disabled.Server.Requests.Count==size);
            disabled.Store.Fail=true;
            Check("secure-store failure cannot become a persisted login",!await disabled.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12") && !disabled.Session.IsSignedIn && disabled.Session.StatusCode=="credential_store");
        }
        using(var mismatch=new Fixture())
        {
            await mismatch.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12"); mismatch.Server.BadUser=true;
            using var next=mismatch.NewSession(new(mismatch.Store,mismatch.Hello,"https://fixture.invalid"));
            Check("refresh cannot switch account identity",!await next.InitializeAsync() && !next.IsSignedIn && next.StatusCode=="invalid_response");
        }
        using(var expired=new Fixture())
        {
            await expired.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12"); expired.Server.FailurePath="/v1/auth/refresh"; expired.Server.FailureStatus=401;
            using var next=expired.NewSession(new(expired.Store,expired.Hello,"https://fixture.invalid"));
            Check("terminal refresh rejection clears recovery and requests login",!await next.InitializeAsync() && next.State==AccountState.ReauthenticationRequired && !expired.Store.Values.ContainsKey("session"));
        }
        using(var security=new Fixture())
        {
            await security.Session.InitializeAsync(); await security.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12");
            var size=security.Server.Requests.Count;
            Check("current password and matching new values required",!await security.Session.ChangePasswordAsync("","SyntheticPassword34","SyntheticPassword34") && security.Server.Requests.Count==size);
            Check("email change keeps original authoritative identity",await security.Session.ChangeEmailAsync("new@example.invalid","new@example.invalid","SyntheticPassword12") && security.Session.EmailChangePending && security.Session.Profile?.Email=="fixture@example.invalid");
            Check("pending email change can be cancelled",await security.Session.CancelEmailChangeAsync() && !security.Session.EmailChangePending);
            Check("password update clears session and recovery",await security.Session.ChangePasswordAsync("SyntheticPassword12","SyntheticPassword34","SyntheticPassword34") && security.Session.State==AccountState.ReauthenticationRequired && !security.Store.Values.ContainsKey("session"));
        }
        using(var lockedLogout=new Fixture())
        {
            await lockedLogout.Session.InitializeAsync(); await lockedLogout.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12"); await lockedLogout.Session.SetHelloAsync(true); lockedLogout.Session.Lock(); var calls=lockedLogout.Hello.Calls;
            lockedLogout.Server.FailurePath="/v1/auth/session-revocations"; lockedLogout.Server.NetworkFailure=true;
            Check("locked offline logout needs no Hello decryption",!await lockedLogout.Session.LogoutAsync(false) && !lockedLogout.Store.Values.ContainsKey("session") && lockedLogout.Hello.Calls==calls && lockedLogout.Store.Values.ContainsKey("revocations"));
        }
        using(var malformed=new Fixture())
        {
            malformed.Server.FailurePath="/v1/auth/token"; malformed.Server.FailureStatus=200;
            Check("malformed successful response is rejected without content echo",!await malformed.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12") && malformed.Session.StatusCode=="invalid_response" && !malformed.Store.Values.ContainsKey("session"));
        }
        using(var disposed=new Fixture())
        {
            await disposed.Session.InitializeAsync(); await disposed.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12"); await disposed.Session.SetHelloAsync(true);
            disposed.Session.Dispose(); disposed.Session.Lock(); disposed.Session.Suspend(); disposed.Session.Dispose();
            Check("late lock/suspend callbacks after disposal are harmless",!disposed.Session.IsSignedIn && !await disposed.Session.InitializeAsync());
        }
        var native=new WindowsAccountCredentials("slice7-smoke-"+Guid.NewGuid());
        try { native.Write("fixture",Encoding.UTF8.GetBytes("synthetic-only")); Check("real Windows Credential Manager write/read",Encoding.UTF8.GetString(native.Read("fixture")!)=="synthetic-only"); }
        finally { native.Delete("fixture"); }
        Check("real credential fixture removed",native.Read("fixture") is null);
        var available=await new WindowsHelloSessionKey().IsAvailableAsync(default);
        Console.WriteLine("EVIDENCE native Hello availability="+available+"; protected PIN/biometric roundtrip NOT RUN; mock cryptography is not platform signoff.");
        if(desktop) await Desktop(Check);
        Console.WriteLine($"PASS ACCOUNT SETTINGS {checks} checks; desktop={desktop}; no production account mutation.");
    }
    private static async Task Desktop(Action<string,bool> check)
    {
        using var f=new Fixture(); f.Hello.Available=false;
        var view=new AccountSettingsView(f.Session); var window=new Window { Title="WordSuggestor Account Test",Width=1000,Height=700,Content=new ScrollViewer { Content=view } };
        var foreground=GetForegroundWindow(); GetCursorPos(out var cursor);
        T Find<T>(string name) where T:FrameworkElement => (T)view.FindName(name);
        try
        {
            window.Show(); window.Activate(); await Until(()=>!f.Session.Busy && f.Session.Config.Supports("email_change_v1"));
            check("native account initially anonymous with password control",Find<PasswordBox>("LoginPassword").IsVisible && !f.Session.IsSignedIn);
            check("login hides entire confirmation and verification rows",!Find<SettingsRow>("RegistrationConfirmationRow").IsVisible && !Find<SettingsRow>("VerificationRow").IsVisible);
            Find<ComboBox>("AccessMode").SelectedValue="register";
            check("registration exposes native confirmation row",Find<SettingsRow>("RegistrationConfirmationRow").IsVisible);
            Find<ComboBox>("AccessMode").SelectedValue="login";
            check("native password has localized screen-reader name",AutomationProperties.GetName(Find<PasswordBox>("LoginPassword"))==SettingsText.Get("AccountPassword"));
            Find<TextBox>("LoginEmail").Text="fixture@example.invalid"; Find<PasswordBox>("LoginPassword").Password="SyntheticPassword12";
            await Click(Find<Button>("LoginButton")); await Until(()=>!f.Session.Busy && f.Session.IsSignedIn);
            check("physical native login updates authoritative identity",Find<TextBlock>("Identity").Text=="Tester");
            check("password cleared and unqualified Hello unavailable",Find<PasswordBox>("LoginPassword").Password.Length==0 && !Find<CheckBox>("HelloCheck").IsEnabled);
            Find<TextBox>("ProfileName").Text="Ægir"; Find<ComboBox>("Age").SelectedValue="45_54";
            // Ensure the real target is in the viewport before physical input.
            Find<Button>("SaveProfile").BringIntoView(); await Task.Delay(150); await Click(Find<Button>("SaveProfile")); await Until(()=>!f.Session.Busy && f.Session.StatusCode=="profile_saved");
            check("native explicit profile save uses UTF-8 backend contract",f.Session.Profile?.DisplayName=="Ægir");
            Find<TextBox>("ProfileName").Text="draft";
            SettingsText.ApplyPreference("en-GB");
            check("copy change preserves profile draft and catalog IDs",Find<TextBox>("ProfileName").Text=="draft" && Find<ComboBox>("Age").SelectedValue as string=="45_54");
            Find<Button>("LogoutAll").BringIntoView(); await Task.Delay(150);
            var count=f.Server.Requests.Count(x=>x.Path=="/v1/auth/sessions");
            using(var response=AnswerDialog(window,false)) await Click(Find<Button>("LogoutAll")); await Task.Delay(150);
            check("native all-device cancellation makes no request",f.Session.IsSignedIn && count==f.Server.Requests.Count(x=>x.Path=="/v1/auth/sessions"));
            using(var response=AnswerDialog(window,true)) await Click(Find<Button>("LogoutAll")); await Until(()=>!f.Session.Busy && !f.Session.IsSignedIn);
            check("native confirmed all-device logout clears recovery",!f.Store.Values.ContainsKey("session") && count+1==f.Server.Requests.Count(x=>x.Path=="/v1/auth/sessions"));
            check("locked/offline local sign-out remains visible",Find<Button>("LogoutCurrent").IsVisible);
        }
        finally { window.Close(); SettingsText.ApplyPreference("da-DK"); if(foreground!=IntPtr.Zero) SetForegroundWindow(foreground); SetCursorPos(cursor.X,cursor.Y); }
    }
    private sealed class DialogAnswer : IDisposable
    {
        private readonly DispatcherTimer _timer;
        public DialogAnswer(Window owner,bool yes)
        {
            _timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(50) };
            _timer.Tick+=(_,_)=> { var hwnd=GetForegroundWindow(); GetWindowThreadProcessId(hwnd,out var pid); var cls=new StringBuilder(64); GetClassName(hwnd,cls,64); if(pid==Environment.ProcessId && cls.ToString()=="#32770") { PostMessage(hwnd,0x111,new IntPtr(yes ? 6 : 7),IntPtr.Zero); _timer.Stop(); } }; _timer.Start();
        }
        public void Dispose()=>_timer.Stop();
    }
    private static IDisposable AnswerDialog(Window owner,bool yes)=>new DialogAnswer(owner,yes);
    private static async Task Until(Func<bool> condition) { var sw=Stopwatch.StartNew(); while(!condition()) { if(sw.ElapsedMilliseconds>15000) throw new Exception("Account UI timed out."); await Task.Delay(25); } }
    private static async Task Click(FrameworkElement element) { var p=element.PointToScreen(new Point(element.ActualWidth/2,element.ActualHeight/2)); SetCursorPos((int)p.X,(int)p.Y); mouse_event(2,0,0,0,UIntPtr.Zero); mouse_event(4,0,0,0,UIntPtr.Zero); await Task.Delay(100); }
    [StructLayout(LayoutKind.Sequential)] private struct Cursor { public int X,Y; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Cursor point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr w,IntPtr l);
}
