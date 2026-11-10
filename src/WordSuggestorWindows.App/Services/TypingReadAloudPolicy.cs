using System.Text.RegularExpressions;

namespace WordSuggestorWindows.App.Services;

public sealed record ReadAloudSegment(int Start, int Length, string Text);

// UI speech units and explicit trigger policy from macOS ba18b45. This does not
// normalize, infer grammatical readings, correct spelling or rank suggestions.
public static partial class TypingReadAloudPolicy
{
    public static string NormalizeStrategy(string? value) => value switch
    { "word" or "sentence" or "paragraph" => value, "page" or "all" => "paragraph", _ => "none" };
    public static bool IsBoundary(string value) => value is " " or "\t" or "\n" or "\r" or "\r\n" or "." or "!" or "?" or "," or ";" or ":";

    public static ReadAloudSegment? Segment(string strategy, string prefix, string inserted)
    {
        if (!IsBoundary(inserted) || !prefix.EndsWith(inserted, StringComparison.Ordinal)) return null;
        if (strategy == "word")
        {
            var end = prefix.Length - inserted.Length;
            if (end == 0 || !(char.IsLetterOrDigit(prefix[end - 1]) || char.GetUnicodeCategory(prefix[end - 1]) == System.Globalization.UnicodeCategory.NonSpacingMark || prefix[end - 1] is '-' or '\'' or '*')) return null;
            var match = Words().Matches(prefix[..end]).LastOrDefault();
            return match is null ? null : new(match.Index, match.Length, match.Value);
        }
        if (strategy == "sentence" && prefix.EndsWith(". ", StringComparison.Ordinal))
            return SentenceAt(prefix[..^1], prefix.Length - 2);
        if (strategy == "paragraph")
        {
            var newline = prefix.EndsWith(".\r\n", StringComparison.Ordinal) ? 2 : prefix.EndsWith(".\n", StringComparison.Ordinal) || prefix.EndsWith(".\r", StringComparison.Ordinal) ? 1 : 0;
            if (newline == 0) return null;
            var end = prefix.Length - newline;
            var start = prefix.LastIndexOfAny(['\r', '\n'], Math.Max(0, end - 1));
            return Trim(prefix, start + 1, end);
        }
        return null;
    }

    public static ReadAloudSegment? SentenceAt(string text, int position) => Sentences().Matches(text)
        .Where(m => position >= m.Index && position < m.Index + m.Length).Select(m => Trim(text, m.Index, m.Index + m.Length)).FirstOrDefault();
    private static ReadAloudSegment? Trim(string text, int start, int end)
    {
        while (start < end && char.IsWhiteSpace(text[start])) start++;
        while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
        return end > start ? new(start, end - start, text[start..end]) : null;
    }
    [GeneratedRegex(@"[\p{L}\p{M}\p{Nd}][\p{L}\p{M}\p{Nd}'*-]*", RegexOptions.CultureInvariant)]
    private static partial Regex Words();
    [GeneratedRegex(@"[^.!?\r\n]+[.!?]*", RegexOptions.CultureInvariant)]
    private static partial Regex Sentences();
}
