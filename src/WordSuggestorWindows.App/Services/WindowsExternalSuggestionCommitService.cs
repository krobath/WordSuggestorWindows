namespace WordSuggestorWindows.App.Services;
public sealed class WindowsExternalSuggestionCommitService
{
    private readonly WindowsNativeTextAdapter _adapter = new();
    private bool _committing;
    public bool TryCommitSuggestion(ExternalTextSnapshot expected, string typedToken, string replacement, out string diagnostic, bool exactInsertion = false)
    {
        diagnostic = "NativeEdit.DuplicateRejected";
        if (_committing) return false;
        _committing = true;
        try { return _adapter.TryCommit(expected, typedToken, replacement, out diagnostic, exactInsertion); }
        finally { _committing = false; }
    }
}
