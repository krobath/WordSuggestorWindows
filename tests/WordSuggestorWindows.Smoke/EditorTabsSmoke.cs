using System.IO;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class EditorTabsSmoke
{
    public static int Run(string testRoot)
    {
        var passed = 0;
        void Check(string name, bool result)
        { if (!result) throw new Exception("Editor tabs: " + name); passed++; Console.WriteLine("PASS TABS " + name); }
        void Reject(string name, Action action)
        { var failed = false; try { action(); } catch (Exception ex) when (ex is IOException or InvalidOperationException) { failed = true; } Check(name, failed); }
        var root = Path.Combine(testRoot, "tabs"); Directory.CreateDirectory(root);
        var originalDataRoot = Environment.GetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", Path.Combine(root, "profile-a"));
            using var profileA = new SingleInstance();
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", Path.Combine(root, "profile-b"));
            using var profileB = new SingleInstance();
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", Path.Combine(root, "profile-a"));
            using var duplicateA = new SingleInstance();
            Check("test profiles isolate running user app and reuse own instance", profileA.IsPrimary && profileB.IsPrimary && !duplicateA.IsPrimary);
        }
        finally { Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", originalDataRoot); }
        var workspace = new EditorWorkspace();
        var first = workspace.Active;
        Check("one empty initial document", workspace.Tabs.Count == 1 && !first.IsDirty && first.Title == "Dokument 1");
        first.UpdateText("Første fane æøå 😀"); first.SetSelection(0, 6, 6);
        var second = workspace.New("Anden fane"); second.SetSelection(2, 2, 2);
        workspace.Activate(first);
        Check("independent text and selection", first.Text == "Første fane æøå 😀" && first.SelectionEnd == 6 && second.CaretIndex == 2);
        Check("dirty titles distinguish documents", first.IsDirty && second.IsDirty && first.Header.EndsWith(" *") && first.Title != second.Title);
        var count = workspace.Tabs.Count;
        Check("cancelled close preserves active document", !workspace.Close(first, _ => false) && workspace.Active == first && workspace.Tabs.Count == count);
        var firstPath = Path.Combine(root, "first.txt");
        workspace.Save(first, firstPath);
        Check("save affects only requested tab", File.ReadAllText(firstPath) == first.Text && !first.IsDirty && second.IsDirty && first.Title == "first.txt");
        first.UpdateText(first.Text + "!");
        var opened = workspace.Open(firstPath.ToUpperInvariant());
        Check("reopening file activates existing unsaved buffer", opened == first && workspace.Tabs.Count == count && first.Text.EndsWith('!'));
        var secondPath = Path.Combine(root, "second.txt");
        File.WriteAllText(secondPath, "Åbnet fil 😀");
        var third = workspace.Open(secondPath);
        Check("open preserves other documents", workspace.Tabs.Count == count + 1 && third.Text == "Åbnet fil 😀" && !third.IsDirty && second.Text == "Anden fane");
        Reject("save cannot overwrite another open tab", () => workspace.Save(second, firstPath));
        Check("rejected save preserves file and unsaved buffer", File.ReadAllText(firstPath) == "Første fane æøå 😀" && second.FilePath is null && second.IsDirty);
        Reject("failed save does not mark clean", () => workspace.Save(second, Path.Combine(root, "missing", "cannot-save.txt")));
        Check("failed save preserves dirty flag", second.IsDirty && second.FilePath is null);
        var blank = workspace.New();
        Check("open reuses clean blank tab", workspace.Open(secondPath) == third && workspace.Tabs.Contains(blank));
        var anotherPath = Path.Combine(root, "third.txt"); File.WriteAllText(anotherPath, "Ny fil");
        workspace.Activate(blank);
        Check("new file reuses active empty document", workspace.Open(anotherPath) == blank && blank.FilePath == anotherPath);
        workspace.Activate(first);
        Check("keyboard cycling wraps", workspace.Adjacent(-1) == workspace.Tabs.Last() && workspace.Adjacent(1) == second);
        var storePath = Path.Combine(root, "workspace.json"); var store = new EditorWorkspaceStore(storePath);
        store.Save(workspace.Snapshot(), 2);
        var restored = new EditorWorkspaceStore(storePath).Load("startup text must not replace a draft");
        Check("restart restores active tab and Danish Unicode draft", restored.Active.Id == first.Id && restored.Active.Text == first.Text && restored.Active.IsDirty && restored.Active.SelectionEnd == 6);
        Check("restart retains file path and independent documents", restored.Tabs.Single(t => t.Id == third.Id).FilePath == secondPath && restored.Tabs.Single(t => t.Id == second.Id).Text == "Anden fane");
        second.UpdateText("Stale snapshot");
        store.Save(workspace.Snapshot(), 1);
        Check("older queued autosave cannot overwrite newer snapshot", new EditorWorkspaceStore(storePath).Load().Tabs.Single(t => t.Id == second.Id).Text == "Anden fane");
        var discarded = EditorWorkspace.Restore(workspace.Snapshot(new HashSet<Guid> { first.Id, second.Id }));
        Check("quit discard snapshot is transactional", first.IsDirty && workspace.Tabs.Contains(second) && discarded.Tabs.All(t => t.Id != second.Id) && discarded.Tabs.Single(t => t.Id == first.Id).Text == first.SavedText);
        var last = new EditorWorkspace("draft");
        last.Close(last.Active, _ => true);
        Check("closing final tab leaves a new empty document", last.Tabs.Count == 1 && last.Active.Text == "" && last.Active.Number == 2);
        File.WriteAllText(storePath, "{\"schemaVersion\":99,\"futureDraft\":true}");
        var futureStore = new EditorWorkspaceStore(storePath); var future = futureStore.Load();
        Reject("newer workspace cannot be overwritten", () => futureStore.Save(future.Snapshot(), 3));
        Check("future draft data preserved with calm warning", futureStore.LoadWarning is not null && File.ReadAllText(storePath).Contains("futureDraft"));
        File.WriteAllText(storePath, "{invalid");
        var corruptStore = new EditorWorkspaceStore(storePath); var fallback = corruptStore.Load();
        Reject("corrupt workspace preserved", () => corruptStore.Save(fallback.Snapshot(), 1));
        var changedStore = new EditorWorkspaceStore(storePath);
        File.WriteAllText(storePath, "{\"schemaVersion\":1,\"tabs\":[]}");
        var invalidBytes = File.ReadAllBytes(storePath);
        Reject("malformed same-schema workspace cannot be overwritten", () => changedStore.Save(workspace.Snapshot(), 4));
        Check("malformed same-schema source bytes preserved", File.ReadAllBytes(storePath).SequenceEqual(invalidBytes));
        return passed;
    }
}
