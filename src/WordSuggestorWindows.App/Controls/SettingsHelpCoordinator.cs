using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WordSuggestorWindows.App.Controls;

internal sealed class SettingsHelpCoordinator
{
    private readonly Window _owner;
    private readonly Popup _popup = new() { StaysOpen = true, Placement = PlacementMode.Right, AllowsTransparency = true };
    private readonly GroupBox _content;
    private readonly TextBlock _description = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 310 };
    private readonly Button _close = new() { Content = "Luk hjælp", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0), Padding = new Thickness(10, 5, 10, 5) };
    private Button? _origin;
    private bool _restoreFocus;
    public bool IsOpen => _popup.IsOpen;

    public SettingsHelpCoordinator(Window owner)
    {
        _owner = owner;
        var panel = new StackPanel(); panel.Children.Add(_description); panel.Children.Add(_close);
        _content = new GroupBox { Padding = new Thickness(14), Margin = new Thickness(2), Content = panel };
        _content.SetResourceReference(Control.BackgroundProperty, SystemColors.WindowBrushKey);
        _content.SetResourceReference(Control.ForegroundProperty, SystemColors.WindowTextBrushKey);
        KeyboardNavigation.SetTabNavigation(_content, KeyboardNavigationMode.Cycle);
        AutomationProperties.SetAutomationId(_content, "SettingsHelpContent");
        AutomationProperties.SetAutomationId(_close, "SettingsHelpClose");
        _popup.Child = _content;
        _close.Click += (_, _) => Close(true);
        _content.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Close(true); } };
        _popup.Opened += (_, _) => _close.Focus();
        _popup.Closed += (_, _) =>
        {
            var origin = _origin;
            if (_restoreFocus && _owner.IsActive && origin is { IsVisible: true, IsEnabled: true }) origin.Focus();
            _restoreFocus = false;
        };
        owner.Deactivated += (_, _) => Close(false);
        owner.Closing += (_, _) => Close(false);
        // Automatic Popup capture closes before the originating button receives
        // its next click, which would immediately reopen it. Handle owner clicks
        // explicitly so the same info button reliably toggles help off.
        owner.PreviewMouseDown += (_, _) =>
        {
            if (IsOpen && _origin?.IsMouseOver != true) Close(false);
        };
    }

    public void Toggle(SettingsHelpEventArgs request)
    {
        if (IsOpen && ReferenceEquals(_origin, request.Anchor)) { Close(true); return; }
        Close(false);
        _origin = request.Anchor;
        _description.Text = request.Help;
        _content.Header = request.Title;
        AutomationProperties.SetName(_content, "Hjælp: " + request.Title);
        _popup.PlacementTarget = request.Anchor;
        _popup.IsOpen = true;
    }

    public void Close(bool restoreFocus)
    {
        _restoreFocus = restoreFocus;
        _popup.IsOpen = false;
    }
}
