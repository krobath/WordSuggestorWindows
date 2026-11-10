using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WordSuggestorWindows.App.Controls;

// Paint over the spoken span without changing text, caret, selection or undo.
internal sealed class SpeechRangeAdorner : Adorner
{
    private TextPointer? _start, _end;
    private readonly Brush _brush = new SolidColorBrush(Color.FromArgb(90, 50, 140, 240));
    public SpeechRangeAdorner(RichTextBox editor) : base(editor) { IsHitTestVisible = false; ClipToBounds = true; }
    public void SetRange(TextPointer? start, TextPointer? end) { _start = start; _end = end; InvalidateVisual(); }
    protected override void OnRender(DrawingContext context)
    {
        if (_start is null || _end is null) return;
        var cursor = _start;
        for (var i = 0; i < 2000 && cursor is not null && cursor.CompareTo(_end) < 0; i++)
        {
            var next = cursor.GetNextInsertionPosition(LogicalDirection.Forward);
            if (next is null) break;
            var a = cursor.GetCharacterRect(LogicalDirection.Forward);
            var b = next.GetCharacterRect(LogicalDirection.Backward);
            if (!a.IsEmpty && !b.IsEmpty && Math.Abs(a.Top - b.Top) < 1 && a.Height > 0)
                context.DrawRectangle(_brush, null, new Rect(a.Left, a.Top, Math.Max(2, b.Right - a.Left), a.Height));
            cursor = next;
        }
    }
}
