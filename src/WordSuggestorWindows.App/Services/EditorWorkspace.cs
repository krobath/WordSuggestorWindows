using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Document ownership is separate from the shared suggestion view model. Each
// document's RichTextBox/undo stack is retained by the native editor window.
public sealed class EditorWorkspace
{
    private int _nextNumber = 1;
    public ObservableCollection<EditorDocumentTab> Tabs { get; } = [];
    public EditorDocumentTab Active { get; private set; } = null!;

    public EditorWorkspace(string initialText = "") => Active = New(initialText);
    private EditorWorkspace(bool restoring) { }

    public EditorDocumentTab New(string text = "")
    {
        if (Tabs.Count >= 128) throw new InvalidOperationException("For mange åbne faner.");
        var document = new EditorDocumentTab(_nextNumber++, text);
        Tabs.Add(document);
        Active = document;
        return document;
    }

    public void Activate(EditorDocumentTab document)
    {
        if (!Tabs.Contains(document)) throw new ArgumentException("Unknown editor document.");
        Active = document;
    }

    public EditorDocumentTab Open(string path)
    {
        path = Path.GetFullPath(path);
        var existing = Tabs.FirstOrDefault(t => string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) { Active = existing; return existing; }
        if (new FileInfo(path).Length > 2_000_000) throw new IOException("Dokumentet er for stort til denne beta.");
        var text = File.ReadAllText(path);
        if (Active.FilePath is null && Active.Text.Length == 0 && !Active.IsDirty)
        {
            Active.UpdateText(text); Active.MarkSaved(path); Active.SetSelection(text.Length, text.Length, text.Length);
            return Active;
        }
        var document = new EditorDocumentTab(_nextNumber++, text, path);
        if (Tabs.Count >= 128) throw new InvalidOperationException("For mange åbne faner.");
        Tabs.Add(document); Active = document;
        return document;
    }

    public void Save(EditorDocumentTab document, string path)
    {
        if (!Tabs.Contains(document)) throw new ArgumentException("Unknown editor document.");
        path = Path.GetFullPath(path);
        if (Tabs.Any(t => t != document && string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Filen er allerede åben i en anden fane.");
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, document.Text, new UTF8Encoding(false)); File.Move(temp, path, overwrite: true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        document.MarkSaved(path);
    }

    public bool Close(EditorDocumentTab document, Func<EditorDocumentTab, bool> confirm)
    {
        if (!Tabs.Contains(document) || !confirm(document)) return false;
        var index = Tabs.IndexOf(document);
        var wasActive = Active == document;
        Tabs.Remove(document);
        if (Tabs.Count == 0) New();
        else if (wasActive) Active = Tabs[Math.Min(index, Tabs.Count - 1)];
        return true;
    }

    public EditorDocumentTab Adjacent(int direction) => Tabs[(Tabs.IndexOf(Active) + direction + Tabs.Count) % Tabs.Count];

    public EditorWorkspaceSnapshot Snapshot(ISet<Guid>? discardChanges = null)
    {
        var tabs = Tabs.Where(t => discardChanges?.Contains(t.Id) != true || t.FilePath is not null)
            .Select(t =>
            {
                var text = discardChanges?.Contains(t.Id) == true ? t.SavedText : t.Text;
                return new EditorTabSnapshot(t.Id, t.Number, text, t.SavedText, t.FilePath,
                    Math.Clamp(t.SelectionStart, 0, text.Length), Math.Clamp(t.SelectionEnd, 0, text.Length), Math.Clamp(t.CaretIndex, 0, text.Length));
            }).ToArray();
        if (tabs.Length == 0) tabs = [new(Guid.NewGuid(), _nextNumber, "", "", null, 0, 0, 0)];
        return new(1, Math.Max(_nextNumber, tabs.Max(t => t.Number) + 1),
            tabs.Any(t => t.Id == Active.Id) ? Active.Id : tabs[0].Id, tabs);
    }

    public static EditorWorkspace Restore(EditorWorkspaceSnapshot snapshot)
    {
        if (snapshot.SchemaVersion != 1 || snapshot.Tabs is not { Length: > 0 and <= 128 } ||
            snapshot.Tabs.Any(t => t is null || t.Id == Guid.Empty || t.Number <= 0 || t.Text is null || t.SavedText is null ||
                t.Text.Length > 2_000_000 || t.SavedText.Length > 2_000_000 || t.FilePath is not null && !Path.IsPathFullyQualified(t.FilePath)) ||
            snapshot.Tabs.Select(t => t.Id).Distinct().Count() != snapshot.Tabs.Length ||
            snapshot.Tabs.Where(t => t.FilePath is not null).Select(t => t.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != snapshot.Tabs.Count(t => t.FilePath is not null) ||
            !snapshot.Tabs.Any(t => t.Id == snapshot.ActiveId) || snapshot.NextNumber >= int.MaxValue - 128 || snapshot.NextNumber <= snapshot.Tabs.Max(t => t.Number))
            throw new InvalidOperationException("Editor workspace is incompatible.");
        var workspace = new EditorWorkspace(true) { _nextNumber = snapshot.NextNumber };
        foreach (var tab in snapshot.Tabs)
        {
            var document = new EditorDocumentTab(tab.Number, tab.Text, tab.FilePath, tab.SavedText, tab.Id);
            document.SetSelection(tab.SelectionStart, tab.SelectionEnd, tab.CaretIndex);
            workspace.Tabs.Add(document);
        }
        workspace.Active = workspace.Tabs.Single(t => t.Id == snapshot.ActiveId);
        return workspace;
    }
}
