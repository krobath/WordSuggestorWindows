using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class AccountHelloNativeSmoke
{
    // Explicit interactive gate; never part of unattended All and never touches
    // an actual account or the production credential namespace.
    public static async Task<bool> Run()
    {
        var hello = new WindowsHelloSessionKey();
        if (!await hello.IsAvailableAsync(default))
        { Console.WriteLine("BLOCKED HELLO NATIVE: no provisioned/accessible key service in this session; product protection not qualified."); return false; }
        var credentials = new WindowsAccountCredentials("hello-native-fixture-" + Guid.NewGuid());
        var record = new AccountRecovery(1,"https://native-fixture.invalid",Guid.NewGuid().ToString(),"synthetic-refresh",Guid.NewGuid().ToString(),"synthetic-revocation",DateTimeOffset.UtcNow.AddDays(1),DateTimeOffset.UtcNow.AddDays(2),7);
        var window = new Window { Title="WordSuggestor — Windows Hello qualification",Width=560,Height=180,Content=new TextBlock { Text="Windows Hello-test med syntetiske sessionsdata. Bekræft PIN/biometri i Windows-dialogerne. Ingen backendkonto anvendes.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(20) } };
        string? keyName = null;
        try
        {
            window.Show(); window.Activate();
            using(var vault = new AccountSessionVault(credentials,hello,record.Origin))
            {
                await vault.SaveAsync(record,default); await vault.SetHelloAsync(true,record,default);
                keyName=JsonDocument.Parse(credentials.Read("session")!).RootElement.GetProperty("name").GetString();
                vault.Lock();
                try { _=await vault.LoadAsync(false,default); throw new Exception("Passive restore bypassed Hello."); } catch(AccountFailure e) when(e.Code=="locked") { }
            }
            using(var reopened = new AccountSessionVault(credentials,hello,record.Origin))
            {
                var loaded = await reopened.LoadAsync(true,default);
                if(loaded!=record) throw new Exception("Native protected roundtrip mismatch.");
                var rotated=record with { RefreshToken="synthetic-rotated-refresh" }; await reopened.SaveAsync(rotated,default); reopened.Lock();
                if(await reopened.LoadAsync(true,default)!=rotated) throw new Exception("Native protected rotation mismatch.");
                await reopened.SetHelloAsync(false,rotated,default);
                if(reopened.Policy!="automatic" || await reopened.LoadAsync(false,default)!=rotated) throw new Exception("Native protection-off migration mismatch.");
            }
            Console.WriteLine("PASS HELLO NATIVE: create/reopen, locked passive rejection, explicit recovery, protected rotation and protection-off; synthetic credentials only."); return true;
        }
        catch(AccountFailure e) { Console.WriteLine("BLOCKED HELLO NATIVE: "+e.Code+"; no positive native qualification."); return false; }
        finally
        {
            window.Close(); var remaining=credentials.Read("session"); credentials.Delete("session");
            if(keyName is not null && remaining is not null && JsonDocument.Parse(remaining).RootElement.GetProperty("policy").GetString()=="hello") await hello.DeleteAsync(keyName,default);
        }
    }
}
