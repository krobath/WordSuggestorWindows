using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

// Explicit synthetic LocalTest profile seeding/readback for real installed updates.
// No arbitrary root, existing document overwrite, network or credential storage.
internal static class RetentionDataSmoke
{
    private const string Learned="betainstalleretsolsikke";
    public static async Task Run(string mode,string root,string appDirectory)
    {
        root=Path.GetFullPath(root);
        var parent=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WordSuggestor","LocalTests");
        if(!string.Equals(Path.GetDirectoryName(root),parent,StringComparison.OrdinalIgnoreCase)||!Regex.IsMatch(Path.GetFileName(root),"^[a-f0-9]{12}$")||mode is not("seed" or "dirty" or "check" or "check-dirty"))throw new InvalidOperationException("Retention fixture root/mode rejected.");
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);
        var workspaceStore=new EditorWorkspaceStore();var choices=new WindowsWordChoiceStore();var settings=new WindowsAppSettingsService();var learned=new LearnedWordStore(root);
        var cli=Path.Combine(appDirectory,"Core","WordSuggestorSuggestCLI.exe");
        var core=new WordSuggestorCoreCliSuggestionProvider(baseDirectory:appDirectory,localDataRoot:root,cliPath:cli);
        var scope=new CoreRequestContext(Guid.NewGuid(),1);
        if(mode=="seed")
        {
            if(File.Exists(workspaceStore.StorePath)||File.Exists(choices.StorePath)||File.Exists(learned.StorePath)||File.Exists(Path.Combine(root,"UnknownWords","sharing-v1.json")))throw new InvalidOperationException("Existing fixture data is preserved.");
            var s=settings.Load();s.ReadingSpeedDelta=.25;s.IsGlobalCaptureEnabled=false;s.IsUnknownWordLookupEnabled=false;s.IsAutomaticUnknownWordLookupEnabled=false;s.IsUnknownWordCrowdHandoffEnabled=false;
            s.AdditionalProperties["retentionSentinel"]=JsonSerializer.SerializeToElement(false);settings.Save(s);
            var workspace=new EditorWorkspace("Synthetic saved document one");Directory.CreateDirectory(Path.Combine(root,"fixtures"));
            workspace.Save(workspace.Active,Path.Combine(root,"fixtures","one.txt"));workspace.New("Synthetic saved document two");workspace.Save(workspace.Active,Path.Combine(root,"fixtures","two.txt"));workspace.Active.SetSelection(2,5,5);workspaceStore.Save(workspace.Snapshot(),1);
            var pack=Path.Combine(root,"Packs","da_lexicon.sqlite");using var packFile=File.OpenRead(pack);var sha=Convert.ToHexString(SHA256.HashData(packFile));
            choices.Append(new(Guid.NewGuid(),DateTimeOffset.UtcNow,"da-DK","syntetisk","analysis_v1:fixture","noun","sb",sha));
            var proposal=JsonSerializer.SerializeToElement(new{termNorm=Learned,display=Learned,gram="sb",lemmaNorm=Learned,freq=10,source="qualification",sourceRecordID="qualification:1"});
            var prepared=await core.PrepareLearnedWordAsync(Learned,proposal,scope,1);
            if(prepared.Value.Status!="prepared")throw new InvalidOperationException("Retention learned fixture unavailable.");
            using(var lease=await learned.AcquireAsync(default))await learned.CommitAsync(System.Text.Encoding.UTF8.GetBytes(prepared.Value.Snapshot!),()=>true,default);
            var handoff=await core.PrepareUnknownWordHandoffAsync(scope,1,"local-retention-fixture");
            if(handoff.Value.Status!="prepared")throw new InvalidOperationException("Retention handoff fixture unavailable.");
            var queue=new{schemaVersion=1,userId=1L,origin="https://fixture.invalid",sourceFingerprint=learned.Fingerprint(),body=handoff.Value.Body,created=DateTimeOffset.UtcNow,attempts=0,retryAfter=DateTimeOffset.UtcNow,status="pending"};
            using var file=new FileStream(Path.Combine(root,"UnknownWords","sharing-v1.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None);JsonSerializer.Serialize(file,queue);file.Flush(true);
        }
        else if(mode=="dirty")
        {
            var workspace=workspaceStore.Load();if(workspace.Tabs.Count!=2||workspace.Tabs.Any(x=>x.IsDirty))throw new InvalidOperationException("Only owned clean retention documents may be changed.");
            workspace.Active.UpdateText(workspace.Active.Text+" synthetic unsaved recovery");workspaceStore.Save(workspace.Snapshot(),2);
        }
        else
        {
            var s=settings.Load();var workspace=workspaceStore.Load();
            if(s.ReadingSpeedDelta!=.25||s.IsGlobalCaptureEnabled||s.IsUnknownWordLookupEnabled||s.IsAutomaticUnknownWordLookupEnabled||s.IsUnknownWordCrowdHandoffEnabled||s.AdditionalProperties["retentionSentinel"].GetBoolean()||workspace.Tabs.Count!=2||workspace.Active.IsDirty!=(mode=="check-dirty")||choices.Load().Count!=1)throw new InvalidOperationException("Retention state mismatch.");
            if(!(await core.SuggestWithOptionsAsync("betainstalleret",scope,new())).Value.Any(x=>x.Term==Learned))throw new InvalidOperationException("Installed learned word unavailable.");
            using var queue=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"UnknownWords","sharing-v1.json")));_=UnknownWordHandoffPacket.Parse(queue.RootElement.GetProperty("body").GetString()!);
        }
        Console.WriteLine("PASS RETENTION "+mode+": settings, tabs, choices, learned words and pending handoff; no upload.");
    }
}
