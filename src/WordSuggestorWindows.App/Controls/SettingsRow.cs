using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.Controls;

public sealed class SettingsHelpEventArgs(Button anchor, string title, string help) : RoutedEventArgs(SettingsRow.HelpRequestedEvent)
{
    public Button Anchor { get; } = anchor;
    public string Title { get; } = title;
    public string Help { get; } = help;
}

public sealed class SettingsRow : HeaderedContentControl
{
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsRow), new PropertyMetadata("", OnCopyChanged));
    public static readonly DependencyProperty ControlIdProperty = DependencyProperty.Register(nameof(ControlId), typeof(string), typeof(SettingsRow), new PropertyMetadata(""));
    public static readonly RoutedEvent HelpRequestedEvent = EventManager.RegisterRoutedEvent("HelpRequested", RoutingStrategy.Bubble, typeof(EventHandler<SettingsHelpEventArgs>), typeof(SettingsRow));
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string ControlId { get => (string)GetValue(ControlIdProperty); set => SetValue(ControlIdProperty, value); }
    private Button? _help;

    static SettingsRow() => HeaderProperty.OverrideMetadata(typeof(SettingsRow), new FrameworkPropertyMetadata(null, OnCopyChanged));
    private static void OnCopyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((SettingsRow)sender).UpdateAccessibility();

    public override void OnApplyTemplate()
    {
        if (_help is not null) _help.Click -= OnHelpClick;
        base.OnApplyTemplate();
        _help = (Button)GetTemplateChild("PART_Help");
        _help.Click += OnHelpClick;
        UpdateAccessibility();
    }

    private void UpdateAccessibility()
    {
        if (_help is null) return;
        AutomationProperties.SetName(_help, SettingsText.Format("HelpFor", Header));
        AutomationProperties.SetAutomationId(_help, "SettingsHelp_" + ControlId);
        AutomationProperties.SetHelpText(_help, Description);
        if (Content is FrameworkElement editor)
        {
            // Read-only values must expose their current text (e.g. version and
            // status), rather than having their UIA name replaced by the row label.
            if (string.IsNullOrEmpty(AutomationProperties.GetAutomationId(editor))) AutomationProperties.SetAutomationId(editor, "SettingsControl_" + ControlId);
            AutomationProperties.SetHelpText(editor, Description);
            if (editor is not TextBlock && GetTemplateChild("PART_Label") is FrameworkElement label) AutomationProperties.SetLabeledBy(editor, label);
        }
    }

    private void OnHelpClick(object sender, RoutedEventArgs e) => RaiseEvent(new SettingsHelpEventArgs(_help!, Header?.ToString() ?? "", Description));
}
