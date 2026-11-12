using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace WordSuggestorWindows.App.Models;

public sealed class EditorDocumentTab : INotifyPropertyChanged
{
    private string _text;
    private string _savedText;
    private string? _filePath;
    public Guid Id { get; }
    public int Number { get; }
    public string Text => _text;
    public string SavedText => _savedText;
    public string? FilePath => _filePath;
    public int SelectionStart { get; private set; }
    public int SelectionEnd { get; private set; }
    public int CaretIndex { get; private set; }
    public string Title => FilePath is null ? $"Dokument {Number}" : Path.GetFileName(FilePath);
    public bool IsDirty => !string.Equals(Text, SavedText, StringComparison.Ordinal);
    public string Header => Title + (IsDirty ? " *" : "");
    public string AccessibleName => Title + (IsDirty ? ", ikke gemt" : "");
    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorDocumentTab(int number, string text = "", string? path = null, string? savedText = null, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid(); Number = number;
        _text = text; _savedText = savedText ?? (path is null ? "" : text); _filePath = path;
        SetSelection(text.Length, text.Length, text.Length);
    }

    public void UpdateText(string text)
    {
        if (_text == text) return;
        _text = text;
        SetSelection(SelectionStart, SelectionEnd, CaretIndex);
        Changed(nameof(Text)); Changed(nameof(IsDirty)); Changed(nameof(Header)); Changed(nameof(AccessibleName));
    }

    public void SetSelection(int start, int end, int caret)
    {
        SelectionStart = Math.Clamp(start, 0, Text.Length);
        SelectionEnd = Math.Clamp(end, SelectionStart, Text.Length);
        CaretIndex = Math.Clamp(caret, SelectionStart, SelectionEnd);
    }

    public void MarkSaved(string path)
    {
        _filePath = path; _savedText = Text;
        Changed(nameof(FilePath)); Changed(nameof(Title)); Changed(nameof(IsDirty)); Changed(nameof(Header)); Changed(nameof(AccessibleName));
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record EditorTabSnapshot(Guid Id, int Number, string Text, string SavedText, string? FilePath,
    int SelectionStart, int SelectionEnd, int CaretIndex);
public sealed record EditorWorkspaceSnapshot(int SchemaVersion, int NextNumber, Guid ActiveId, EditorTabSnapshot[] Tabs);
