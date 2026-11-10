using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App;

public partial class MainWindow
{
    private readonly SpeechTypingTracker _editorSpeechTyping = new(), _externalSpeechTyping = new();
    private RichTextBox? _speechEditor;
    private SpeechRangeAdorner? _speechAdorner;
    private long _speechEditorGeneration, _speechUtteranceGeneration;
    private bool _externalAutomaticSpeech;
    private bool _automaticEditorSpeech;
    private string _speechCommittedPrefix = "", _externalSpeechCommittedPrefix = "";
    private object? _externalSpeechTarget;
    private AppSettingsSnapshot? _lastSpeechSettings;

    private void InitializeSpeechLifecycle()
    {
        _editorSpeechTyping.Reset(EditorTextBox, GetEditorPlainText(), GetEditorCaretIndex());
        _lastSpeechSettings = _viewModel.LoadSettingsSnapshot();
        _viewModel.SettingsChanged += SpeechSettingsChanged;
        _editorWindow.Deactivated += EditorSpeechDeactivated;
        _editorWindow.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(SpeechScrollChanged));
    }
    private void DisposeSpeechLifecycle()
    {
        _viewModel.SettingsChanged -= SpeechSettingsChanged;
        _editorWindow.Deactivated -= EditorSpeechDeactivated;
        _editorWindow.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(SpeechScrollChanged));
        CancelDocumentSpeech(false);
    }
    private void SpeechScrollChanged(object sender, ScrollChangedEventArgs e) => _speechAdorner?.InvalidateVisual();
    private void EditorSpeechDeactivated(object? sender, EventArgs e) => CancelDocumentSpeech(false);
    private void SpeechSettingsChanged(object? sender, AppSettingsSnapshot settings)
    {
        var before = _lastSpeechSettings; _lastSpeechSettings = settings.Clone();
        if (before is null) return;
        if (before.ReadingSpeedDelta != settings.ReadingSpeedDelta || before.ReadingStrategy != settings.ReadingStrategy ||
            before.ReadingHighlightMode != settings.ReadingHighlightMode || before.SelectedLanguageCode != settings.SelectedLanguageCode ||
            before.SpeechLanguageMode != settings.SpeechLanguageMode || before.UseSystemSpeechSettings != settings.UseSystemSpeechSettings ||
            before.IsGlobalCaptureEnabled != settings.IsGlobalCaptureEnabled ||
            !before.SystemVoiceIdOverrideByLanguage.OrderBy(p => p.Key).SequenceEqual(settings.SystemVoiceIdOverrideByLanguage.OrderBy(p => p.Key)))
        { CancelDocumentSpeech(false); _editorSpeechTyping.Reset(); _externalSpeechTyping.Reset(); }
    }
    private void OnEditorSpeechTextChanged()
    {
        var editor = EditorTextBox;
        if (_automaticEditorSpeech && ReferenceEquals(editor, _speechEditor) && GetEditorPlainText().StartsWith(_speechCommittedPrefix, StringComparison.Ordinal))
        {
            // Appending the next word cannot invalidate an already committed
            // earlier span. Keep its sound/cues while tracking the new snapshot.
            _speechEditorGeneration++; _speechUtteranceGeneration = _speechEditorGeneration;
        }
        else CancelDocumentSpeech(false);
        var generation = _speechEditorGeneration;
        if (_isSynchronizingEditorDocument)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (generation == _speechEditorGeneration && ReferenceEquals(editor, EditorTextBox))
                    _editorSpeechTyping.Reset(editor, GetEditorPlainText(), GetEditorCaretIndex());
            }, DispatcherPriority.Background);
            return;
        }
        Dispatcher.BeginInvoke(() =>
        {
            if (generation != _speechEditorGeneration || !ReferenceEquals(editor, EditorTextBox)) return;
            var text = GetEditorPlainText(); var caret = Math.Clamp(GetEditorCaretIndex(), 0, text.Length);
            var segment = _editorSpeechTyping.Observe(editor, text, caret, _viewModel.LoadSettingsSnapshot().ReadingStrategy);
            if (_editorWindow.IsActive && editor.IsKeyboardFocusWithin && segment is not null)
            {
                SpeakText(segment.Text, "skrivning i editoren", segment.Start);
                _automaticEditorSpeech = true; _speechCommittedPrefix = text[..(segment.Start + segment.Length)];
            }
        }, DispatcherPriority.Background);
    }
    private void OnSpeechSelectionChanged()
    {
        var text = GetEditorPlainText();
        // A selection event from the same edit must not consume its queued
        // boundary. A genuine caret/selection move resets the baseline.
        if (_editorSpeechTyping.Text != text) return;
        CancelDocumentSpeech(false);
        _editorSpeechTyping.Reset(EditorTextBox, text, GetEditorCaretIndex());
    }
    private void HandleExternalTypingSpeech(ExternalTextSnapshot? snapshot)
    {
        if (snapshot is null || !_viewModel.IsGlobalCaptureEnabled || snapshot.Start != snapshot.End)
        {
            _externalSpeechTyping.Reset();
            if (_externalAutomaticSpeech) CancelDocumentSpeech(false);
            return;
        }
        var key = (snapshot.Window, snapshot.Control, snapshot.ProcessId, snapshot.ProcessStarted);
        var sameText = snapshot.Text == _externalSpeechTyping.Text;
        var segment = _externalSpeechTyping.Observe(key, snapshot.Text, snapshot.End, _viewModel.LoadSettingsSnapshot().ReadingStrategy);
        if (_externalAutomaticSpeech && (!Equals(key, _externalSpeechTarget) || sameText ||
            !snapshot.Text.StartsWith(_externalSpeechCommittedPrefix, StringComparison.Ordinal))) CancelDocumentSpeech(false);
        if (segment is null || _selectionImportService.CurrentForegroundWindow != snapshot.Window) return;
        CancelDocumentSpeech(false);
        var options = _viewModel.CreateTextToSpeechOptions(segment.Text) with { ReadingHighlightMode = "none" };
        try
        {
            _textToSpeechService.Speak(segment.Text, options); _externalAutomaticSpeech = true;
            _externalSpeechTarget = key; _externalSpeechCommittedPrefix = snapshot.Text[..(segment.Start + segment.Length)];
            _viewModel.SetTextToSpeechSpeaking(true, "Oplæser under skrivning.");
        }
        catch (InvalidOperationException) { _viewModel.SetTextToSpeechSpeaking(false, "Oplæsning er ikke tilgængelig med den valgte stemme."); }
    }
    private void CancelDocumentSpeech(bool restore)
    {
        _textToSpeechService.Stop();
        StopSpeechHighlight(restore);
    }
    private bool SpeechPresentationIsCurrent() => ReferenceEquals(_speechEditor, EditorTextBox) && _speechUtteranceGeneration == _speechEditorGeneration;
    private void AttachSpeechPresentation()
    {
        _speechEditor = EditorTextBox; _speechUtteranceGeneration = _speechEditorGeneration;
        if (AdornerLayer.GetAdornerLayer(_speechEditor) is { } layer)
        { _speechAdorner = new SpeechRangeAdorner(_speechEditor); layer.Add(_speechAdorner); }
    }
    private void ClearSpeechPresentation()
    {
        if (_speechAdorner is { } adorner && AdornerLayer.GetAdornerLayer(adorner.AdornedElement) is { } layer) layer.Remove(adorner);
        _speechAdorner = null;
    }
    private void OverlaySpeechRequested(object? sender, string term)
    {
        CancelDocumentSpeech(false);
        try
        {
            _textToSpeechService.Speak(term, _viewModel.CreateTextToSpeechOptions(term) with { ReadingHighlightMode = "none" });
            _viewModel.SetTextToSpeechSpeaking(true, "Oplæser forslag.");
        }
        catch (InvalidOperationException) { _viewModel.SetTextToSpeechSpeaking(false, "Oplæsning er ikke tilgængelig med den valgte stemme."); }
    }
}
