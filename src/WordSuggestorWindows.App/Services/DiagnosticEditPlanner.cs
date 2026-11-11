using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed record AnalysisEditTransaction(string Original, string Result, int Caret, IReadOnlyList<CoreDiagnosticEdit> Edits)
{
    public override string ToString() => $"AnalysisEditTransaction edits={Edits.Count}";
}
public static class DiagnosticEditPlanner
{
    public static AnalysisEditTransaction Create(string original, int caret, CoreDiagnostic diagnostic, int alternative = 0)
    {
        var edits = diagnostic.Edits.Length > 0 ? diagnostic.Edits :
            alternative >= 0 && alternative < diagnostic.Replacements.Length ? [new CoreDiagnosticEdit(diagnostic.Start, diagnostic.Length, diagnostic.Replacements[alternative])] : Array.Empty<CoreDiagnosticEdit>();
        if (edits.Length == 0) throw new InvalidOperationException("No authorized correction.");
        var sorted = edits.OrderBy(e => e.Start).ToArray(); var end = -1; var previousStart = -1;
        foreach (var edit in sorted)
        {
            if (edit.Start == previousStart || edit.Start <= end || edit.Start < 0 || edit.Length < 0 || edit.Start > original.Length || edit.Length > original.Length - edit.Start ||
                !Boundary(original, edit.Start) || !Boundary(original, edit.Start + edit.Length) || edit.Replacement is null || edit.Replacement.Length > 512)
                throw new InvalidOperationException("Invalid or overlapping correction.");
            end = edit.Start + edit.Length - 1;
            previousStart = edit.Start;
        }
        var text = original; var nextCaret = Math.Clamp(caret, 0, original.Length);
        foreach (var edit in sorted.Reverse())
        {
            text = text[..edit.Start] + edit.Replacement + text[(edit.Start + edit.Length)..];
            if (nextCaret >= edit.Start + edit.Length) nextCaret += edit.Replacement.Length - edit.Length;
            else if (nextCaret >= edit.Start) nextCaret = edit.Start + edit.Replacement.Length;
        }
        return new(original, text, nextCaret, sorted.Reverse().ToArray());
    }
    private static bool Boundary(string text, int index) => index == 0 || index == text.Length || !(char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]));
}
