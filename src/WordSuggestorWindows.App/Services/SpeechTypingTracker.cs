namespace WordSuggestorWindows.App.Services;

// Observe confirmed document snapshots, never raw/global keyboard events.
public sealed class SpeechTypingTracker
{
    private object? _target;
    public string Text { get; private set; } = "";
    private int _caret;
    public void Reset(object? target = null, string text = "", int caret = 0)
    { _target = target; Text = text; _caret = Math.Clamp(caret, 0, text.Length); }
    public ReadAloudSegment? Observe(object target, string text, int caret, string strategy)
    {
        ReadAloudSegment? result = null;
        if (Equals(target, _target) && caret >= 0 && caret <= text.Length)
        {
            var added = text.Length - Text.Length;
            if (added is 1 or 2 && caret == _caret + added && text.AsSpan(0, _caret).SequenceEqual(Text.AsSpan(0, _caret)) &&
                text.AsSpan(caret).SequenceEqual(Text.AsSpan(_caret)))
            {
                var inserted = text.Substring(_caret, added);
                result = TypingReadAloudPolicy.Segment(strategy, text[..caret], inserted);
            }
        }
        Reset(target, text, caret);
        return result is { Length: <= 16000 } ? result : null;
    }
}
