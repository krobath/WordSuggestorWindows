using System.Windows.Input;

namespace WordSuggestorWindows.App.Services;

public static class SuggestionShortcutPolicy
{
    public static ModifierKeys Modifiers(string scheme) => scheme switch
    { "ctrl" => ModifierKeys.Control, "ctrlShift" => ModifierKeys.Control | ModifierKeys.Shift, "alt" => ModifierKeys.Alt, _ => ModifierKeys.None };
    public static string Label(string scheme, int index) => (scheme switch
    { "ctrl" => "Ctrl", "ctrlShift" => "Ctrl+Shift", "alt" => "Alt", _ => "" }) + "+" + (index == 9 ? 0 : index + 1);
}
