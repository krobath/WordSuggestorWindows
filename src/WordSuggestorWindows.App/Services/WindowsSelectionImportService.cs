using System.Runtime.InteropServices;
using System.Windows.Automation;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsSelectionImportService
{
    public SelectionImportResult? TryReadSelectionFromForegroundWindow(IntPtr excludedWindowHandle)
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == excludedWindowHandle)
        {
            return null;
        }

        return TryReadFocusedSelection("Windows UI Automation focused element")
            ?? TryReadSelectionFromWindow(foreground);
    }

    private static SelectionImportResult? TryReadSelectionFromWindow(IntPtr windowHandle)
    {
        try
        {
            var root = AutomationElement.FromHandle(windowHandle);
            if (root is null)
            {
                return null;
            }

            var focused = root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.HasKeyboardFocusProperty, true));

            return focused is not null
                ? TryReadTextPatternSelection(focused, "Windows UI Automation focused descendant")
                : TryReadTextPatternSelection(root, "Windows UI Automation window");
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static SelectionImportResult? TryReadFocusedSelection(string source)
    {
        try
        {
            return TryReadTextPatternSelection(AutomationElement.FocusedElement, source);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static SelectionImportResult? TryReadTextPatternSelection(AutomationElement? element, string source)
    {
        if (element is null ||
            !element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) ||
            patternObject is not TextPattern textPattern)
        {
            return null;
        }

        var ranges = textPattern.GetSelection();
        if (ranges.Length == 0)
        {
            return null;
        }

        var text = string.Join(
            Environment.NewLine,
            ranges
                .Select(range => range.GetText(-1))
                .Where(value => !string.IsNullOrWhiteSpace(value)))
            .Trim();

        return string.IsNullOrWhiteSpace(text)
            ? null
            : new SelectionImportResult(text, source, DateTimeOffset.Now);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
