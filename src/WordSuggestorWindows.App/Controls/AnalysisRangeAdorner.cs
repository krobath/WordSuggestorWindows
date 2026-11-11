using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WordSuggestorWindows.App.Controls;

internal sealed record AnalysisPaintSpan(TextPointer Start, TextPointer End, Brush Brush, bool Underline);
internal sealed class AnalysisRangeAdorner(RichTextBox editor) : Adorner(editor)
{
    public IReadOnlyList<AnalysisPaintSpan> Spans { get; set; } = [];
    protected override void OnRender(DrawingContext drawing)
    {
        var budget = 18000;
        foreach (var span in Spans)
        {
            var position = span.Start;
            if (span.Underline && position.CompareTo(span.End) == 0)
            {
                var rectangle = position.GetCharacterRect(LogicalDirection.Forward);
                if (!rectangle.IsEmpty) drawing.DrawLine(new Pen(SystemParameters.HighContrast ? SystemColors.HotTrackBrush : span.Brush, 1.5), new(rectangle.Left, rectangle.Bottom - 1), new(rectangle.Left + 6, rectangle.Bottom - 1));
            }
            while (position.CompareTo(span.End) < 0 && budget-- > 0)
            {
                var next = position.GetNextInsertionPosition(LogicalDirection.Forward); if (next is null) break;
                var a = position.GetCharacterRect(LogicalDirection.Forward); var b = next.GetCharacterRect(LogicalDirection.Backward);
                if (!a.IsEmpty && !b.IsEmpty && Math.Abs(a.Top - b.Top) < 1 && a.Height > 0)
                {
                    var rectangle = new Rect(a.Left, a.Top, Math.Max(2, b.Right - a.Left), a.Height);
                    if (span.Underline) drawing.DrawLine(new Pen(SystemParameters.HighContrast ? SystemColors.HotTrackBrush : span.Brush, 1.5), new(rectangle.Left, rectangle.Bottom - 1), new(rectangle.Right, rectangle.Bottom - 1));
                    else if (SystemParameters.HighContrast) drawing.DrawRectangle(null, new Pen(SystemColors.HotTrackBrush, .7), rectangle);
                    else drawing.DrawRectangle(span.Brush, null, rectangle);
                }
                position = next;
            }
        }
    }
}
