namespace WordSuggestorWindows.App.Services;

// Contains only an ephemeral in-memory verified edit receipt, never diagnostics.
public sealed record SmartCommitReceipt(string Text, int Caret, long Created)
{
    public override string ToString() => $"SmartCommitReceipt length={Text.Length}, caret={Caret}";
    public string? MatchInput(string next, int caret, long now)
    {
        if (now - Created is < 0 or > 1500 || Caret < 1 || Caret > Text.Length || Text[Caret - 1] != ' ') return null;
        var length = next.Length - Text.Length;
        if (length is < 1 or > 4 || caret != Caret + length ||
            !next.StartsWith(Text[..Caret], StringComparison.Ordinal) || next[caret..] != Text[Caret..]) return null;
        return next.Substring(Caret, length);
    }
}
