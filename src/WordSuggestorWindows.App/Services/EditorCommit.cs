using System.Globalization;

namespace WordSuggestorWindows.App.Services;

// UTF-16 offsets at the Windows editing boundary, not language/ranking logic.
public static class EditorCommit
{
    public static (string Text, int Caret) Replace(string text, int start, int end, string replacement, bool exactInsertion = false)
    {
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        if (start == end)
        {
            while (start > 0 && IsTokenCharacter(text[start - 1])) start--;
            while (end < text.Length && IsTokenCharacter(text[end])) end++;
        }
        // Never split a surrogate pair at a selection boundary.
        if (start > 0 && start < text.Length && char.IsLowSurrogate(text[start]) && char.IsHighSurrogate(text[start - 1])) start--;
        if (end > 0 && end < text.Length && char.IsHighSurrogate(text[end - 1]) && char.IsLowSurrogate(text[end])) end++;
        var space = end < text.Length && char.IsWhiteSpace(text[end]) ? "" : " ";
        var inserted = replacement + (exactInsertion ? "" : space);
        return (text[..start] + inserted + text[end..], start + inserted.Length);
    }

    public static string Suffix(string text, int start, int end)
    {
        start = Math.Clamp(start, 0, text.Length); end = Math.Clamp(end, start, text.Length);
        if (start == end) while (end < text.Length && IsTokenCharacter(text[end])) end++;
        return text[end..];
    }

    public static string PrefixWindow(string text, int maximum)
    {
        var end = Math.Min(maximum, text.Length);
        if (end > 0 && end < text.Length && char.IsHighSurrogate(text[end - 1]) && char.IsLowSurrogate(text[end])) end--;
        return text[..end];
    }
    public static string TailWindow(string text, int maximum)
    {
        var start = Math.Max(0, text.Length - maximum);
        if (start > 0 && start < text.Length && char.IsLowSurrogate(text[start]) && char.IsHighSurrogate(text[start - 1])) start++;
        return text[start..];
    }

    public static bool IsTokenCharacter(char value) => char.IsLetterOrDigit(value) ||
        char.GetUnicodeCategory(value) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark ||
        value is '\'' or '-';

    public static string Prefix(string text, int caret)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var start = caret;
        while (start > 0 && IsTokenCharacter(text[start - 1])) start--;
        return text[start..caret];
    }
}
