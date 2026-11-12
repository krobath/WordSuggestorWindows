using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class MainWindow : Window
{
    private const double CollapsedWidth = 560;
    private const double CollapsedHeight = 68;
    private const double OverlayVerticalGap = 10;
    private const double StaticOverlayHorizontalOffset = 118;
    private const double StaticOverlayVerticalOffset = 44;
    private const int TextToSpeechHotKeyId = 0x5754;
    private const int ExternalSuggestionAcceptBaseHotKeyId = 0x5800;
    private const int ExternalSuggestionPreviousPageHotKeyId = 0x5810;
    private const int ExternalSuggestionNextPageHotKeyId = 0x5811;
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VirtualKeyT = 0x54;
    private const uint VirtualKey0 = 0x30;
    private const uint VirtualKey1 = 0x31;
    private const uint VirtualKeyLeft = 0x25;
    private const uint VirtualKeyRight = 0x27;
    private static readonly string SelectionImportDiagnosticLogPath = Path.Combine(
        WindowsDataPaths.Root,
        "diagnostics",
        "selection-import.log");
    private static readonly string OcrFlowDiagnosticLogPath = Path.Combine(
        WindowsDataPaths.Root,
        "diagnostics",
        "ocr-flow.log");
    private static readonly string TtsFlowDiagnosticLogPath = Path.Combine(
        WindowsDataPaths.Root,
        "diagnostics",
        "tts-flow.log");
    private static readonly Brush SpeechHighlightBrush = BrushFromHex("#8DCCFF");
    private readonly MainWindowViewModel _viewModel;
    private readonly InternalEditorWindow _editorWindow;
    private RichTextBox EditorTextBox => _editorWindow.TextEditor;
    private readonly WindowsSelectionImportService _selectionImportService = new();
    private readonly WindowsGlobalSuggestionCaptureService _globalSuggestionCaptureService = new();
    private readonly WindowsExternalSuggestionCommitService _externalSuggestionCommitService = new();
    private readonly WindowsOcrService _ocrService = new();
    private readonly WindowsSpeechToTextService _speechToTextService = new();
    private readonly WindowsTextToSpeechService _textToSpeechService = new();
    private readonly DispatcherTimer _externalSelectionPollTimer;
    private readonly DispatcherTimer _speechHighlightTimer;
    private readonly DispatcherTimer _preciseSpeechHighlightTimer;
    private IntPtr _windowHandle;
    private IntPtr _lastExternalWindowHandle;
    private bool _isInitialPositionApplied;
    private bool _isSynchronizingEditorDocument;
    private bool _isUpdatingEditorSelection;
    private bool _isTextToSpeechHotKeyRegistered;
    private bool _areExternalSuggestionHotKeysRegistered;
    private SelectionImportResult? _lastExternalSelection;
    private ExternalSuggestionAnchorSnapshot? _lastExternalSuggestionAnchor;
    private string _lastExternalSuggestionAnchorDiagnosticSignature = string.Empty;
    private DateTimeOffset _speechHighlightStartedAt;
    private IReadOnlyList<TextSpan> _speechHighlightSpans = [];
    private TextSpan? _currentSpeechHighlightSpan;
    private EditorSelectionRange? _speechSelectionRangeToRestore;
    private TimeSpan _speechHighlightDuration = TimeSpan.Zero;
    private readonly List<TextToSpeechBoundaryCue> _preciseSpeechCues = [];
    private DateTimeOffset _preciseSpeechPlaybackStartedAt;
    private int _nextPreciseSpeechCueIndex;
    private int _preciseSpeechHighlightBaseOffset;
    private bool _usePreciseSpeechHighlight;
    private Point? _manualOverlayTopLeft;
    private SuggestionOverlayWindow? _overlayWindow;
    private SettingsWindow? _settingsWindow;
    private bool _isResolvingExternalSelection;
    private bool _isUpdatingExternalSuggestionAnchor;
    private long _caretMeasurementGeneration;
    private string? _documentPath;
    private string _savedDocumentText = string.Empty;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        var placement = viewModel.LoadSettingsSnapshot();
        if (placement.OverlayLeftPhysical is double x && placement.OverlayTopPhysical is double y && double.IsFinite(x) && double.IsFinite(y))
            _manualOverlayTopLeft = new Point(x, y);
        _externalSelectionPollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _externalSelectionPollTimer.Tick += ExternalSelectionPollTimerOnTick;
        _speechHighlightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(90)
        };
        _speechHighlightTimer.Tick += SpeechHighlightTimerOnTick;
        _preciseSpeechHighlightTimer = new DispatcherTimer();
        _preciseSpeechHighlightTimer.Tick += PreciseSpeechHighlightTimerOnTick;
        _selectionImportService.DiagnosticEmitted += SelectionImportServiceOnDiagnosticEmitted;
        _globalSuggestionCaptureService.DiagnosticEmitted += SelectionImportServiceOnDiagnosticEmitted;
        _globalSuggestionCaptureService.TokenChanged += GlobalSuggestionCaptureServiceOnTokenChanged;
        InitializeComponent();
        DataContext = _viewModel;
        _editorWindow = new InternalEditorWindow(this, viewModel);
        NonActivatingWindow.Apply(this);
        _viewModel.CanCommitLocally = () => _editorWindow.IsActive && EditorTextBox.IsKeyboardFocusWithin;
        _viewModel.ExternalCommitRequested = TryCommitExternalSuggestion;
        Loaded += OnLoaded;
        Closing += (_, e) => { if (!ConfirmDocumentChanges()) e.Cancel = true; };
        Closed += OnClosed;
        LocationChanged += OnWindowLocationOrSizeChanged;
        SizeChanged += OnWindowLocationOrSizeChanged;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        _viewModel.AnalysisCommitRequested = CommitAnalysisEdits;
        _viewModel.SettingsChanged += SuggestionSettingsOnChanged;
        _viewModel.LocalSuggestionCommitted += LocalSuggestionOnCommitted;
        _editorWindow.Deactivated += SuggestionEditorDeactivated;
        ApplySuggestionEditorLayout();
        _speechToTextService.TranscriptReceived += SpeechToTextServiceOnTranscriptReceived;
        _speechToTextService.StatusChanged += SpeechToTextServiceOnStatusChanged;
        _speechToTextService.SessionStopped += SpeechToTextServiceOnSessionStopped;
        _textToSpeechService.StatusChanged += TextToSpeechServiceOnStatusChanged;
        _textToSpeechService.SpeechStopped += TextToSpeechServiceOnSpeechStopped;
        _textToSpeechService.PrecisePlaybackStarted += TextToSpeechServiceOnPrecisePlaybackStarted;
        _textToSpeechService.BoundaryCueReceived += TextToSpeechServiceOnBoundaryCueReceived;
        InitializeSpeechLifecycle();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_windowHandle)?.AddHook(WindowMessageHook);
        RegisterTextToSpeechHotKey();
        SyncGlobalSuggestionCapture();
        _externalSelectionPollTimer.Start();

        if (!_isInitialPositionApplied)
        {
            PositionAtTopCenter();
            ApplyShellSize(force: true);
            _isInitialPositionApplied = true;
        }

        SyncOverlayVisibility();
        SyncEditorDocumentFromViewModel(force: true);
        EditorTextBox.SelectionBrush = SpeechHighlightBrush;
        EditorTextBox.SelectionOpacity = 0.52;

        if (_viewModel.IsEditorExpanded)
        {
            RefocusEditor();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        DisposeSpeechLifecycle();
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ShutdownAnalysisPresentation();
        _viewModel.SettingsChanged -= SuggestionSettingsOnChanged;
        _viewModel.LocalSuggestionCommitted -= LocalSuggestionOnCommitted;
        _editorWindow.Deactivated -= SuggestionEditorDeactivated;
        CancelSuggestionAssist();
        _viewModel.Dispose();
        _editorWindow.CloseForShutdown();
        LocationChanged -= OnWindowLocationOrSizeChanged;
        SizeChanged -= OnWindowLocationOrSizeChanged;
        _externalSelectionPollTimer.Stop();
        _externalSelectionPollTimer.Tick -= ExternalSelectionPollTimerOnTick;
        _speechHighlightTimer.Stop();
        _speechHighlightTimer.Tick -= SpeechHighlightTimerOnTick;
        _preciseSpeechHighlightTimer.Stop();
        _preciseSpeechHighlightTimer.Tick -= PreciseSpeechHighlightTimerOnTick;
        if (_windowHandle != IntPtr.Zero)
        {
            HwndSource.FromHwnd(_windowHandle)?.RemoveHook(WindowMessageHook);
            UnregisterTextToSpeechHotKey();
            UnregisterExternalSuggestionHotKeys();
        }

        _selectionImportService.DiagnosticEmitted -= SelectionImportServiceOnDiagnosticEmitted;
        _globalSuggestionCaptureService.DiagnosticEmitted -= SelectionImportServiceOnDiagnosticEmitted;
        _globalSuggestionCaptureService.TokenChanged -= GlobalSuggestionCaptureServiceOnTokenChanged;
        _globalSuggestionCaptureService.Dispose();
        _speechToTextService.TranscriptReceived -= SpeechToTextServiceOnTranscriptReceived;
        _speechToTextService.StatusChanged -= SpeechToTextServiceOnStatusChanged;
        _speechToTextService.SessionStopped -= SpeechToTextServiceOnSessionStopped;
        _speechToTextService.Dispose();
        _textToSpeechService.StatusChanged -= TextToSpeechServiceOnStatusChanged;
        _textToSpeechService.SpeechStopped -= TextToSpeechServiceOnSpeechStopped;
        _textToSpeechService.PrecisePlaybackStarted -= TextToSpeechServiceOnPrecisePlaybackStarted;
        _textToSpeechService.BoundaryCueReceived -= TextToSpeechServiceOnBoundaryCueReceived;
        _textToSpeechService.Dispose();

        if (_overlayWindow is not null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Close();
            _settingsWindow = null;
        }
    }

    private static void SelectionImportServiceOnDiagnosticEmitted(object? sender, SelectionImportDiagnostic diagnostic)
    {
        WriteSelectionImportDiagnostic(diagnostic);
    }

    private static void WriteSelectionImportDiagnostic(SelectionImportDiagnostic diagnostic)
    {
        var line = $"WordSuggestor selection import: {diagnostic}";
        Debug.WriteLine(line);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SelectionImportDiagnosticLogPath)!);
            File.AppendAllText(SelectionImportDiagnosticLogPath, line + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void RegisterTextToSpeechHotKey()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        _isTextToSpeechHotKeyRegistered = RegisterHotKey(_windowHandle, TextToSpeechHotKeyId, ModControl | ModAlt, VirtualKeyT);
        WriteTtsFlowDiagnostic(_isTextToSpeechHotKeyRegistered
            ? "Global TTS hotkey registered: Ctrl+Alt+T."
            : $"Global TTS hotkey registration failed: GetLastWin32Error={Marshal.GetLastWin32Error()}.");
    }

    private void RegisterExternalSuggestionHotKeys() => RegisterSuggestionLaneHotKeys();
    private void UnregisterExternalSuggestionHotKeys() => UnregisterSuggestionLaneHotKeys();

    private void UnregisterTextToSpeechHotKey()
    {
        if (!_isTextToSpeechHotKeyRegistered || _windowHandle == IntPtr.Zero)
        {
            return;
        }

        _ = UnregisterHotKey(_windowHandle, TextToSpeechHotKeyId);
        _isTextToSpeechHotKeyRegistered = false;
    }

    private void SyncGlobalSuggestionCapture()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (_viewModel.IsGlobalCaptureEnabled)
        {
            try
            {
                _globalSuggestionCaptureService.Start(_windowHandle);
                WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                    DateTimeOffset.Now,
                    "GlobalSuggestionCapture",
                    "Started",
                    "Windows global suggestion capture started."));
            }
            catch (InvalidOperationException ex)
            {
                WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                    DateTimeOffset.Now,
                    "GlobalSuggestionCapture",
                    "Failed",
                    ex.Message));
            }

            return;
        }

        _globalSuggestionCaptureService.Stop();
        UnregisterExternalSuggestionHotKeys();
        _viewModel.EndExternalSuggestionSession(hideOverlayWhenPossible: true);
        _lastExternalSuggestionAnchor = null;
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == TextToSpeechHotKeyId)
        {
            handled = true;
            WriteTtsFlowDiagnostic("Global TTS hotkey invoked.");
            _ = Dispatcher.InvokeAsync(async () => await SpeakSelectionOrEditorTextAsync());
            return IntPtr.Zero;
        }

        if (msg == WmHotKey && TryHandleSuggestionLaneHotKey(wParam.ToInt32(), (uint)lParam.ToInt64() & 0xffff)) { handled = true; return IntPtr.Zero; }

        return IntPtr.Zero;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.EditorText)) ApplySuggestionEditorLayout();
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.IsEditorExpanded):
                if (_viewModel.IsEditorExpanded)
                {
                    _editorWindow.ShowEditor();
                    SyncEditorDocumentFromViewModel(force: true);
                    Dispatcher.BeginInvoke(RefocusEditor);
                }
                else
                {
                    _editorWindow.Hide();
                    _viewModel.SetLocalEditorActive(false);
                }
                SyncOverlayVisibility();
                break;
            case nameof(MainWindowViewModel.CaretIndex):
                if (!_isSynchronizingEditorDocument && !_isUpdatingEditorSelection) SetEditorCaretIndex(_viewModel.CaretIndex);
                break;
            case nameof(MainWindowViewModel.EditorText):
                SyncEditorDocumentFromViewModel();
                break;
            case nameof(MainWindowViewModel.AnalysisDocument):
            case nameof(MainWindowViewModel.IsAnalyzerColoringEnabled):
                ApplyEditorColoring();
                break;
            case nameof(MainWindowViewModel.IsSuggestionInteractionEnabled):
            case nameof(MainWindowViewModel.ShouldShowSuggestionOverlay):
            case nameof(MainWindowViewModel.CurrentSuggestionPage):
            case nameof(MainWindowViewModel.VisibleSuggestions):
                SyncOverlayVisibility();
                break;
            case nameof(MainWindowViewModel.SuggestionPlacementMode):
                if (_viewModel.IsStaticPlacementMode)
                {
                    CaptureOrInitializeStaticOverlayPosition();
                }

                SyncOverlayVisibility();
                break;
            case nameof(MainWindowViewModel.RequestsCaretGeometry):
                _caretMeasurementGeneration++;
                _lastExternalSuggestionAnchor = null;
                _lastExternalSuggestionAnchorDiagnosticSignature = string.Empty;
                if (_viewModel.IsStaticPlacementMode) CaptureOrInitializeStaticOverlayPosition();
                SyncOverlayVisibility();
                break;
            case nameof(MainWindowViewModel.IsGlobalCaptureEnabled):
                SyncGlobalSuggestionCapture();
                break;
        }
    }

    private void GlobalSuggestionCaptureServiceOnTokenChanged(object? sender, ExternalSuggestionTokenChangedEventArgs e)
    {
        var speechSnapshot = _globalSuggestionCaptureService.CurrentSnapshot;
        Dispatcher.BeginInvoke(() =>
        {
            HandleExternalTypingSpeech(speechSnapshot);
            ObserveExternalSuggestionInput(speechSnapshot);
            _viewModel.ExternalSuggestionSuffix = speechSnapshot is null ? "" : EditorCommit.Suffix(speechSnapshot.Text, speechSnapshot.Start, speechSnapshot.End);
            _viewModel.ExternalSuggestionTarget = speechSnapshot;
            if (e.WindowHandle == IntPtr.Zero)
            {
                _viewModel.EndExternalSuggestionSession(hideOverlayWhenPossible: true);
                SyncOverlayVisibility();
                return;
            }
            if (!_viewModel.IsGlobalCaptureEnabled ||
                e.WindowHandle == _windowHandle || _editorWindow.IsActive)
            {
                return;
            }

            if (_lastExternalWindowHandle != e.WindowHandle) _lastExternalSuggestionAnchor = null;
            _lastExternalWindowHandle = e.WindowHandle;
            var source = DescribeExternalWindow(e.WindowHandle);
            if (e.IsBoundary)
            {
                _viewModel.UpdateExternalSuggestionToken(string.Empty, e.WindowHandle, source);
            }
            else
            {
                _viewModel.UpdateExternalSuggestionToken(e.Token, e.WindowHandle, source);
            }

            UpdateExternalSuggestionAnchorAsync();
            WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                DateTimeOffset.Now,
                "GlobalSuggestionCapture",
                "TokenDispatched",
                $"source={source}, boundary={e.IsBoundary}, tokenLength={e.Token.Length}, activeExternalSession={_viewModel.IsExternalSuggestionSessionActive}"));
            SyncOverlayVisibility();
        });
    }

    private void TryAcceptExternalSuggestion(int visibleIndex) => _viewModel.AcceptSuggestionAtIndex(visibleIndex);

    private bool TryCommitExternalSuggestion(ExternalSuggestionCommitRequest request)
    {
        using var measurement = _viewModel.Diagnostics.Measure(DiagnosticMetric.NativeCommit);
        var snapshot = _globalSuggestionCaptureService.CurrentSnapshot;
        if (snapshot is null || snapshot.Window != request.WindowHandle || request.ExpectedTarget != snapshot) { _viewModel.Diagnostics.Record(DiagnosticCategory.Placement, DiagnosticEvent.NativeEditRejected); return false; }
        var committed = _externalSuggestionCommitService.TryCommitSuggestion(snapshot, request.TypedToken,
            request.Suggestion.CommitPlan?.Text ?? request.Suggestion.InsertText ?? request.Suggestion.Term, out var diagnostic, exactInsertion: request.Suggestion.CommitPlan is not null);
        _viewModel.Diagnostics.Record(DiagnosticCategory.Placement, committed ? DiagnosticEvent.NativeEditVerified : DiagnosticEvent.NativeEditRejected);
        WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(DateTimeOffset.Now,
            "ExternalSuggestionCommit", committed ? "Success" : "Rejected", diagnostic));
        _globalSuggestionCaptureService.ClearTokenAfterCommittedSuggestion();
        _lastExternalSuggestionAnchor = null;
        if (committed) RememberExternalSmartCommit(request.Suggestion);
        if (!committed)
        {
            _viewModel.EndExternalSuggestionSession(hideOverlayWhenPossible: true);
            _viewModel.SetStatusMessage("Indsættelse blev afbrudt. Kontrollér fokus og skriv videre.");
        }
        return committed;
    }

    private void OnWindowLocationOrSizeChanged(object? sender, EventArgs e)
    {
        UpdateOverlayPosition();
    }

    private void PositionAtTopCenter()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + ((workArea.Width - Width) / 2);
        Top = workArea.Top + 18;
    }

    private void ApplyShellSize(bool force = false)
    {
        var nextWidth = CollapsedWidth;
        var nextHeight = CollapsedHeight;

        if (!force &&
            Math.Abs(Width - nextWidth) < 0.5 &&
            Math.Abs(Height - nextHeight) < 0.5)
        {
            return;
        }

        var topEdge = Top;
        var midX = Left + (Width / 2);

        Width = nextWidth;
        Height = nextHeight;
        Left = midX - (nextWidth / 2);
        Top = topEdge;

        ClampToWorkArea();
    }

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        if (Left < workArea.Left)
        {
            Left = workArea.Left;
        }

        if (Top < workArea.Top)
        {
            Top = workArea.Top;
        }

        if (Left + Width > workArea.Right)
        {
            Left = Math.Max(workArea.Left, workArea.Right - Width);
        }

        if (Top + Height > workArea.Bottom)
        {
            Top = Math.Max(workArea.Top, workArea.Bottom - Height);
        }
    }

    private void ToolbarSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
    }

    private static bool IsInteractiveElement(DependencyObject? origin)
    {
        var current = origin;
        while (current is not null)
        {
            if (current is ButtonBase or ComboBox or TextBox or RichTextBox)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void ExpandCollapseButton_OnClick(object sender, RoutedEventArgs e)
        => OpenEditor();

    public void OpenEditor()
    {
        _viewModel.EnsureEditorExpanded();
        _editorWindow.ShowEditor();
        RefocusEditor();
    }

    internal async void ToolbarAction_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string action })
        {
            if (action == "wordList")
            {
                new UserWordsWindow(_viewModel) { Owner = this }.Show();
                return;
            }
            if (action == "importClipboard")
            {
                var foreground = _selectionImportService.CurrentForegroundWindow;
                var selection = await _selectionImportService.TryReadSelectionWithClipboardFallbackAsync(foreground, IntPtr.Zero);
                if (selection is not null) _viewModel.ImportTextIntoEditor(selection.Text, selection.Source);
                else _viewModel.SetStatusMessage("Clipboard-import kunne ikke gennemføres sikkert.");
                return;
            }
            if (action == "import")
            {
                await ImportSelectionIntoEditorAsync();
                return;
            }

            if (action == "ocr")
            {
                await RunOcrScreenSnipAsync();
                return;
            }

            if (action == "speechToText")
            {
                ToggleSpeechToText();
                return;
            }

            if (action == "textToSpeech")
            {
                await SpeakSelectionOrEditorTextAsync();
                return;
            }

            if (action == "insights")
            {
                ShowInsightsWindow();
                return;
            }

            if (action == "settings")
            {
                ShowSettingsWindow();
                return;
            }

            _viewModel.HandleToolbarAction(action);
        }
    }

    private void ShowInsightsWindow()
    {
        var insightsWindow = new InsightsWindow(_viewModel.LoadInsightsSnapshot())
        {
            Owner = this
        };
        insightsWindow.Show();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_viewModel)
        {
            Owner = this
        };
        _settingsWindow.Closed += SettingsWindowOnClosed;
        _settingsWindow.Show();
    }

    private void SettingsWindowOnClosed(object? sender, EventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= SettingsWindowOnClosed;
            _settingsWindow = null;
        }
    }

    private void ToggleSpeechToText()
    {
        if (_speechToTextService.IsListening)
        {
            _speechToTextService.Stop();
            _viewModel.SetSpeechToTextListening(false, "Tale-til-tekst stopper.");
            return;
        }

        try
        {
            _viewModel.EnsureEditorExpanded();
            SyncEditorDocumentFromViewModel(force: true);
            var status = _speechToTextService.Start(_viewModel.SelectedLanguageOption);
            _viewModel.SetSpeechToTextListening(true, status);
            RefocusEditor();
        }
        catch (InvalidOperationException ex)
        {
            _viewModel.SetSpeechToTextListening(false, $"Tale-til-tekst kunne ikke starte: {ex.Message}");
        }
    }

    private void SpeechToTextServiceOnTranscriptReceived(object? sender, SpeechToTextTranscript transcript)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!transcript.IsFinal)
            {
                _viewModel.SetStatusMessage($"Lytter: {transcript.Text}");
                return;
            }

            _viewModel.InsertDictatedText(
                transcript.Text,
                $"talegenkendelse {transcript.CultureName} ({transcript.Confidence:P0})");
            RefocusEditor();
        });
    }

    private void SpeechToTextServiceOnStatusChanged(object? sender, string status)
    {
        Dispatcher.BeginInvoke(() => _viewModel.SetStatusMessage(status));
    }

    private void SpeechToTextServiceOnSessionStopped(object? sender, string status)
    {
        Dispatcher.BeginInvoke(() => _viewModel.SetSpeechToTextListening(false, status));
    }

    private async Task SpeakSelectionOrEditorTextAsync()
    {
        WriteTtsFlowDiagnostic(
            $"UI TTS action started: lastExternalWindow=0x{_lastExternalWindowHandle.ToInt64():X}, editorChars={_viewModel.EditorText.Length}.");
        if (_textToSpeechService.IsSpeaking)
        {
            _textToSpeechService.Stop();
            StopSpeechHighlight(restoreEditorSelection: true);
            _viewModel.SetTextToSpeechSpeaking(false, "Oplæsning stopper.");
            WriteTtsFlowDiagnostic("UI TTS action stopped active speech.");
            return;
        }

        var internalSelection = GetSelectedEditorSpeechText();
        if (internalSelection is not null)
        {
            _speechSelectionRangeToRestore = CaptureCurrentEditorSelectionRange();
            // Speech highlighting does not move the user selection or caret.
            WriteTtsFlowDiagnostic($"UI TTS action using internal editor selection: chars={internalSelection.Length}.");
            SpeakText(internalSelection.Text, "intern editor-markering", internalSelection.Start);
            return;
        }

        var externalSelection = await ResolvePreferredExternalSelectionAsync("TTS");
        if (externalSelection is not null)
        {
            WriteTtsFlowDiagnostic(
                $"UI TTS action selected external text route: source={externalSelection.Source}, chars={externalSelection.Text.Length}, hwnd=0x{externalSelection.WindowHandle.ToInt64():X}.");
            MirrorAndSpeakExternalSelection(externalSelection);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_viewModel.EditorText))
        {
            _viewModel.EnsureEditorExpanded();
            SyncEditorDocumentFromViewModel(force: true);
            WriteTtsFlowDiagnostic($"UI TTS action using staged internal editor text: chars={_viewModel.EditorText.Length}.");
            SpeakText(_viewModel.EditorText, "intern editor-tekst", 0);
            RefocusEditor();
            return;
        }

        _viewModel.SetStatusMessage("Ingen tekst fundet til oplæsning. Markér tekst eller skriv/importér tekst i editoren.");
        WriteTtsFlowDiagnostic("UI TTS action completed without readable text.");
    }

    private void MirrorAndSpeakExternalSelection(SelectionImportResult selection)
    {
        _viewModel.ImportTextIntoEditor(selection.Text, $"{selection.Source} til oplæsning");
        RefocusEditor();
        WriteTtsFlowDiagnostic($"UI TTS action mirrored external selection: source={selection.Source}, chars={selection.Text.Length}.");
        SpeakText(selection.Text, selection.Source, 0);
    }

    private void SpeakText(string text, string source, int highlightBaseOffset)
    {
        CancelDocumentSpeech(false);
        try
        {
            if (_speechSelectionRangeToRestore is null && _viewModel.IsEditorExpanded)
            {
                _speechSelectionRangeToRestore = CaptureCurrentEditorSelectionRange();
            }

            var options = _viewModel.CreateTextToSpeechOptions(text);
            var invocation = _textToSpeechService.Speak(text, options);
            AttachSpeechPresentation();
            if (invocation.SupportsPreciseBoundaryMetadata)
            {
                PreparePreciseSpeechHighlight(highlightBaseOffset);
            }
            else
            {
                StartSpeechHighlight(text, highlightBaseOffset, options);
            }
            var voiceSummary = invocation.FallbackReason is null
                ? $"{invocation.VoiceDisplayName} ({invocation.VoiceSource})"
                : $"fallback: {invocation.FallbackReason}";
            _viewModel.SetTextToSpeechSpeaking(
                true,
                $"Oplæser {text.Trim().Length} tegn fra {source} med {voiceSummary}.");
        }
        catch (InvalidOperationException ex)
        {
            StopSpeechHighlight(restoreEditorSelection: true);
            _viewModel.SetTextToSpeechSpeaking(false, $"Oplæsning kunne ikke starte: {ex.Message}");
            WriteTtsFlowDiagnostic($"UI TTS action failed to start speech: {ex.GetType().Name}");
        }
    }

    private void TextToSpeechServiceOnStatusChanged(object? sender, string status)
    {
        Dispatcher.BeginInvoke(() => { if (_textToSpeechService.IsCurrent(sender)) _viewModel.SetStatusMessage(status); });
    }

    private void TextToSpeechServiceOnPrecisePlaybackStarted(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() => { if (_textToSpeechService.IsCurrent(sender) && SpeechPresentationIsCurrent()) StartPreciseSpeechHighlightPlayback(); });
    }

    private void TextToSpeechServiceOnBoundaryCueReceived(object? sender, TextToSpeechBoundaryCue cue)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!_textToSpeechService.IsCurrent(sender) || !SpeechPresentationIsCurrent() || !_usePreciseSpeechHighlight)
            {
                return;
            }

            _preciseSpeechCues.Add(cue with
            {
                Start = cue.Start + _preciseSpeechHighlightBaseOffset
            });
            _preciseSpeechCues.Sort((left, right) => left.StartTime.CompareTo(right.StartTime));

            if (_preciseSpeechPlaybackStartedAt != default && !_preciseSpeechHighlightTimer.IsEnabled)
            {
                ScheduleNextPreciseSpeechHighlight();
            }
        });
    }

    private void TextToSpeechServiceOnSpeechStopped(object? sender, string status)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!_textToSpeechService.IsCurrent(sender)) return;
            StopSpeechHighlight(restoreEditorSelection: true);
            _viewModel.SetTextToSpeechSpeaking(false, status);
        });
    }

    private async Task RunOcrScreenSnipAsync()
    {
        _viewModel.SetStatusMessage("Vælg et skærmudsnit til OCR.");
        WriteOcrFlowDiagnostic("UI OCR action started.");
        HideOverlayWindow();

        var wasVisible = IsVisible;
        if (wasVisible)
        {
            WriteOcrFlowDiagnostic("Hiding WordSuggestor window before Snipping Tool capture.");
            Hide();
            await Task.Delay(150);
        }

        OcrImportResult? result = null;
        try
        {
            result = await _ocrService.CaptureScreenAndRecognizeAsync();
        }
        catch (OperationCanceledException)
        {
            WriteOcrFlowDiagnostic("OCR action cancelled.");
        }
        finally
        {
            if (wasVisible)
            {
                WriteOcrFlowDiagnostic("Restoring WordSuggestor window after Snipping Tool capture.");
                Show();
            }

            Activate();
        }

        if (result is null)
        {
            WriteOcrFlowDiagnostic("OCR action completed without import result.");
            _viewModel.SetStatusMessage("OCR fandt ingen tekst, eller skærmudsnittet blev annulleret.");
            SyncOverlayVisibility();
            return;
        }

        WriteOcrFlowDiagnostic($"OCR action importing recognized text: chars={result.Text.Length}, lines={result.LineCount}, source={result.Source}");
        _viewModel.ImportTextIntoEditor(result.Text, $"{result.Source} ({result.LineCount} linjer)");
        WriteOcrFlowDiagnostic($"OCR action imported into editor: editorChars={_viewModel.EditorText.Length}, caretIndex={_viewModel.CaretIndex}");
        RefocusEditor();
    }

    private static void WriteOcrFlowDiagnostic(string message)
    {
        var line = $"{DateTimeOffset.Now:O} [UI] {message}";
        Debug.WriteLine($"WordSuggestor OCR: {message}");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OcrFlowDiagnosticLogPath)!);
            File.AppendAllText(OcrFlowDiagnosticLogPath, line + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void WriteTtsFlowDiagnostic(string message)
    {
        var line = $"{DateTimeOffset.Now:O} [UI] {message}";
        Debug.WriteLine($"WordSuggestor TTS: {message}");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TtsFlowDiagnosticLogPath)!);
            File.AppendAllText(TtsFlowDiagnosticLogPath, line + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void ExternalSelectionPollTimerOnTick(object? sender, EventArgs e)
    {
        if (_isResolvingExternalSelection || _textToSpeechService.IsSpeaking)
        {
            return;
        }

        try
        {
            var foreground = _selectionImportService.CurrentForegroundWindow;
            if (_viewModel.IsExternalSuggestionSessionActive && foreground != _viewModel.ExternalSuggestionWindowHandle)
            {
                _viewModel.EndExternalSuggestionSession(hideOverlayWhenPossible: true);
                _lastExternalSuggestionAnchor = null;
                SyncOverlayVisibility();
            }
            if (foreground != IntPtr.Zero && !WindowsNativeTextAdapter.IsOwnWindow(foreground))
            {
                _lastExternalWindowHandle = foreground;
            }

            if (_viewModel.IsExternalSuggestionSessionActive)
            {
                UpdateExternalPollingInterval(activeExternalSession: true);
            }
            else
            {
                UpdateExternalPollingInterval(activeExternalSession: false);
            }

            // Selection is read only on explicit TXT/TTS actions.

        }
        catch (Exception ex)
        {
            WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                DateTimeOffset.Now,
                "UIA",
                "PollFailed",
                $"External selection poll failed safely: {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private void UpdateExternalPollingInterval(bool activeExternalSession)
    {
        var target = activeExternalSession
            ? TimeSpan.FromMilliseconds(120)
            : TimeSpan.FromMilliseconds(350);
        if (_externalSelectionPollTimer.Interval != target)
        {
            _externalSelectionPollTimer.Interval = target;
        }
    }

    private void UpdateExternalSuggestionAnchor()
    {
        if (!_viewModel.RequestsCaretGeometry || !_viewModel.IsExternalSuggestionSessionActive)
        {
            _lastExternalSuggestionAnchor = null;
            _lastExternalSuggestionAnchorDiagnosticSignature = string.Empty;
            return;
        }

        var targetWindow = _viewModel.ExternalSuggestionWindowHandle != IntPtr.Zero
            ? _viewModel.ExternalSuggestionWindowHandle
            : _lastExternalWindowHandle;
        if (targetWindow == IntPtr.Zero || targetWindow == _windowHandle)
        {
            _lastExternalSuggestionAnchor = null;
            _lastExternalSuggestionAnchorDiagnosticSignature = string.Empty;
            return;
        }

        _lastExternalSuggestionAnchor = _selectionImportService.TryReadSuggestionAnchorFromWindowHandle(
            targetWindow,
            _windowHandle,
            "external suggestion session");

        var signature = _lastExternalSuggestionAnchor is null
            ? $"missing:{targetWindow.ToInt64():X}"
            : $"rect:{targetWindow.ToInt64():X}:{_lastExternalSuggestionAnchor.Quality}:{_lastExternalSuggestionAnchor.ScreenRect}";

        if (string.Equals(signature, _lastExternalSuggestionAnchorDiagnosticSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastExternalSuggestionAnchorDiagnosticSignature = signature;
        WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
            DateTimeOffset.Now,
            "SuggestionAnchor",
            _lastExternalSuggestionAnchor is null ? "Unavailable" : "Updated",
            _lastExternalSuggestionAnchor is null
                ? $"No anchor was resolved for hwnd=0x{targetWindow.ToInt64():X}."
                : $"Resolved {_lastExternalSuggestionAnchor.Quality} anchor at {_lastExternalSuggestionAnchor.ScreenRect} for hwnd=0x{targetWindow.ToInt64():X}."));
    }

    private void UpdateExternalSuggestionAnchorAsync()
    {
        if (!_viewModel.RequestsCaretGeometry || !_viewModel.IsExternalSuggestionSessionActive || _isUpdatingExternalSuggestionAnchor)
        {
            return;
        }

        var targetWindow = _viewModel.ExternalSuggestionWindowHandle != IntPtr.Zero
            ? _viewModel.ExternalSuggestionWindowHandle
            : _lastExternalWindowHandle;

        if (targetWindow == IntPtr.Zero || targetWindow == _windowHandle)
        {
            _lastExternalSuggestionAnchor = null;
            _lastExternalSuggestionAnchorDiagnosticSignature = string.Empty;
            return;
        }

        _isUpdatingExternalSuggestionAnchor = true;
        var measurementGeneration = _caretMeasurementGeneration;
        var capturedMainWindow = _windowHandle;
        _ = Task.Run(() =>
        {
            var anchor = _selectionImportService.TryReadSuggestionAnchorFromWindowHandle(
                targetWindow, capturedMainWindow, "external suggestion session");

            Dispatcher.BeginInvoke(() =>
            {
                _isUpdatingExternalSuggestionAnchor = false;

                if (measurementGeneration != _caretMeasurementGeneration || !_viewModel.RequestsCaretGeometry ||
                    !_viewModel.IsExternalSuggestionSessionActive || _viewModel.ExternalSuggestionWindowHandle != targetWindow || _editorWindow.IsActive)
                {
                    return;
                }

                var signature = anchor is null
                    ? $"missing:{targetWindow.ToInt64():X}"
                    : $"rect:{targetWindow.ToInt64():X}:{anchor.Quality}:{anchor.ScreenRect}";

                if (string.Equals(signature, _lastExternalSuggestionAnchorDiagnosticSignature, StringComparison.Ordinal))
                {
                    return;
                }

                _lastExternalSuggestionAnchorDiagnosticSignature = signature;
                _lastExternalSuggestionAnchor = anchor;
                WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                    DateTimeOffset.Now,
                    "SuggestionAnchor",
                    anchor is null ? "Unavailable" : "Updated",
                    anchor is null
                        ? $"No anchor was resolved for hwnd=0x{targetWindow.ToInt64():X}."
                        : $"Resolved {anchor.Quality} anchor at {anchor.ScreenRect} for hwnd=0x{targetWindow.ToInt64():X}."));

                UpdateOverlayPosition();
            });
        });
    }

    private string DescribeExternalWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return "ekstern app";
        }

        GetWindowThreadProcessId(windowHandle, out var processId);
        try
        {
            var process = Process.GetProcessById((int)processId);
            return string.IsNullOrWhiteSpace(process.ProcessName)
                ? "ekstern app"
                : process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "ekstern app";
        }
        catch (InvalidOperationException)
        {
            return "ekstern app";
        }
    }

    private async Task ImportSelectionIntoEditorAsync()
    {
        var internalSelection = _editorWindow.IsActive ? GetSelectedEditorPlainText() : string.Empty;
        if (!string.IsNullOrWhiteSpace(internalSelection))
        {
            _viewModel.ImportTextIntoEditor(internalSelection, "intern editor-markering");
            RefocusEditor();
            return;
        }

        var externalSelection = await ResolvePreferredExternalSelectionAsync("TXT");
        if (externalSelection is not null)
        {
            WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                DateTimeOffset.Now,
                "Resolve",
                "Success",
                $"TXT selected external route: source={externalSelection.Source}, chars={externalSelection.Text.Length}, hwnd=0x{externalSelection.WindowHandle.ToInt64():X}"));
            _viewModel.ImportTextIntoEditor(externalSelection.Text, externalSelection.Source);
            RefocusEditor();
            return;
        }

        _viewModel.SetStatusMessage("Ingen markeret tekst fundet. Prøv at markere tekst i editoren eller i en app der eksponerer Windows UI Automation-selection.");
    }

    internal void EditorCommand_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string action })
        {
            return;
        }

        switch (action)
        {
            case "openDocument":
            case "saveDocument":
                HandleDocumentAction(action == "saveDocument");
                break;
            case "cut":
                ApplicationCommands.Cut.Execute(null, EditorTextBox);
                _viewModel.SetStatusMessage("Klip udført i editoren.");
                break;
            case "copy":
                ApplicationCommands.Copy.Execute(null, EditorTextBox);
                _viewModel.SetStatusMessage("Kopiér udført i editoren.");
                break;
            case "paste":
                ApplicationCommands.Paste.Execute(null, EditorTextBox);
                _viewModel.SetStatusMessage("Indsæt udført i editoren.");
                break;
            case "coloring":
                _viewModel.ToggleAnalyzerColoring();
                break;
            case "semantic":
                _viewModel.ToggleSemanticDiagnostics();
                break;
            case "punctuation":
                _viewModel.TogglePunctuationDiagnostics();
                break;
            case "refreshSuggestions":
                _viewModel.RefreshSuggestionsPreview();
                break;
        }

        RefocusEditor();
    }

    private bool HandleDocumentAction(bool save)
    {
        if (!save && !ConfirmDocumentChanges()) return false;
        try
        {
            if (save)
            {
                _viewModel.SetStatusMessage("Vælg hvor dokumentet skal gemmes.");
                var dialog = new Microsoft.Win32.SaveFileDialog { Title = "Gem WordSuggestor-tekst", Filter = "Tekstfiler (*.txt)|*.txt", DefaultExt = ".txt", FileName = _documentPath is null ? "Dokument.txt" : Path.GetFileName(_documentPath) };
                if (dialog.ShowDialog(_editorWindow) != true) return false;
                var temp = dialog.FileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(temp, _viewModel.EditorText, new System.Text.UTF8Encoding(false));
                    File.Move(temp, dialog.FileName, overwrite: true);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
                _documentPath = dialog.FileName;
                _savedDocumentText = _viewModel.EditorText;
                _viewModel.SetStatusMessage("Dokumentet er gemt.");
            }
            else
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Åbn WordSuggestor-tekst", Filter = "Tekstfiler (*.txt)|*.txt|Alle filer (*.*)|*.*" };
                if (dialog.ShowDialog(_editorWindow) != true) return false;
                if (new FileInfo(dialog.FileName).Length > 2_000_000) { _viewModel.SetStatusMessage("Dokumentet er for stort til denne beta."); return false; }
                _viewModel.ImportTextIntoEditor(File.ReadAllText(dialog.FileName), "tekstfil");
                _documentPath = dialog.FileName;
                _savedDocumentText = _viewModel.EditorText;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { _viewModel.SetStatusMessage("Dokumentet kunne ikke åbnes eller gemmes. Kontrollér filadgangen."); return false; }
    }

    private bool ConfirmDocumentChanges()
    {
        if (_viewModel.EditorText == _savedDocumentText) return true;
        var result = MessageBox.Show(_editorWindow.IsVisible ? _editorWindow : this,
            "Vil du gemme ændringerne i dokumentet?", "WordSuggestor", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result == MessageBoxResult.No || (result == MessageBoxResult.Yes && HandleDocumentAction(true));
    }

    private void Exit_OnClick(object sender, RoutedEventArgs e) => Close();

    internal void EditorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isSynchronizingEditorDocument)
        {
            _isUpdatingEditorSelection = true;
            try
            {
                _viewModel.CaretIndex = GetEditorCaretIndex();
                _viewModel.EditorText = GetEditorPlainText();
            }
            finally { _isUpdatingEditorSelection = false; }
        }

        OnEditorSpeechTextChanged();
        ObserveSuggestionEditorInput();
        UpdateOverlayPosition();
    }

    internal void EditorTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_isSynchronizingEditorDocument || _isUpdatingEditorSelection)
        {
            return;
        }

        OnSpeechSelectionChanged();
        _isUpdatingEditorSelection = true;
        try { _viewModel.UpdateEditorSelection(GetTextOffset(EditorTextBox.Selection.Start), GetTextOffset(EditorTextBox.Selection.End)); }
        finally { _isUpdatingEditorSelection = false; }
        UpdateOverlayPosition();
    }

    internal void EditorTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        RegisterSuggestionKey(e);
        if (TryHandleControlDigitSuggestion(e) || TryHandleControlPaging(e))
        {
            return;
        }

        RecordEditorInsightKey(e);

        if (e.Key == Key.Tab && _viewModel.AcceptSelectedSuggestion())
        {
            RefocusEditor();
            e.Handled = true;
        }
    }

    internal void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key is Key.O or Key.S)
        {
            HandleDocumentAction(e.Key == Key.S);
            e.Handled = true;
            return;
        }
        if (e.OriginalSource == EditorTextBox)
        {
            return;
        }

        if (TryHandleControlDigitSuggestion(e) || TryHandleControlPaging(e))
        {
            RefocusEditor();
        }
    }

    private void RecordEditorInsightKey(KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            _viewModel.RecordBackspaceActivity();
            return;
        }

        var boundary = e.Key switch
        {
            Key.Enter => "enter",
            Key.OemPeriod or Key.Decimal => ".",
            Key.D1 when (Keyboard.Modifiers & ModifierKeys.Shift) != 0 => "!",
            Key.OemQuestion when (Keyboard.Modifiers & ModifierKeys.Shift) != 0 => "?",
            _ => null
        };

        if (boundary is not null)
        {
            _viewModel.RecordSentenceBoundary(boundary);
        }
    }

    private bool TryHandleControlDigitSuggestion(KeyEventArgs e) => TryHandleSuggestionDigit(e);

    private bool TryHandleControlPaging(KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            return false;
        }

        if (e.Key == Key.Left)
        {
            _viewModel.ChangeSuggestionPage(-1);
            RefocusEditor();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Right)
        {
            _viewModel.ChangeSuggestionPage(1);
            RefocusEditor();
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void RefocusEditor()
    {
        if (!_editorWindow.IsVisible) return;
        _editorWindow.Activate();
        EditorTextBox.Focus();
        _viewModel.SetLocalEditorActive(_editorWindow.IsActive && EditorTextBox.IsKeyboardFocusWithin);
        SetEditorCaretIndex(_viewModel.CaretIndex);
        UpdateOverlayPosition();
    }

    private void SyncEditorDocumentFromViewModel(bool force = false)
    {
        if (_isSynchronizingEditorDocument)
        {
            return;
        }

        var desired = _viewModel.EditorText;
        if (!force && string.Equals(GetEditorPlainText(), desired, StringComparison.Ordinal))
        {
            ApplyEditorColoring();
            return;
        }

        _isSynchronizingEditorDocument = true;
        try
        {
            var documentRange = new TextRange(EditorTextBox.Document.ContentStart, EditorTextBox.Document.ContentEnd)
            {
                Text = desired
            };

            foreach (var block in EditorTextBox.Document.Blocks.OfType<Paragraph>())
            {
                block.Margin = new Thickness(0);
                block.LineHeight = double.NaN;
            }
        }
        finally
        {
            _isSynchronizingEditorDocument = false;
        }

        ApplyEditorColoring();
        SetEditorCaretIndex(_viewModel.CaretIndex);
    }

    private string GetEditorPlainText()
    {
        var text = new TextRange(EditorTextBox.Document.ContentStart, EditorTextBox.Document.ContentEnd).Text;
        return NormalizeRichTextBoxText(text);
    }

    private string GetSelectedEditorPlainText()
    {
        var text = new TextRange(EditorTextBox.Selection.Start, EditorTextBox.Selection.End).Text;
        return NormalizeRichTextBoxText(text).Trim();
    }

    private TextSpan? GetSelectedEditorSpeechText()
    {
        var rawText = NormalizeRichTextBoxText(new TextRange(EditorTextBox.Selection.Start, EditorTextBox.Selection.End).Text);
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var leadingWhitespace = rawText.Length - rawText.TrimStart().Length;
        var trimmedText = rawText.Trim();
        return new TextSpan(GetTextOffset(EditorTextBox.Selection.Start) + leadingWhitespace, trimmedText.Length, trimmedText);
    }

    private SelectionImportResult? TryGetRecentExternalSelection()
    {
        if (_lastExternalSelection is null)
        {
            return null;
        }

        var age = DateTimeOffset.Now - _lastExternalSelection.CapturedAt;
        if (age > TimeSpan.FromSeconds(12))
        {
            return null;
        }

        if (_lastExternalWindowHandle != IntPtr.Zero &&
            _lastExternalSelection.WindowHandle != IntPtr.Zero &&
            _lastExternalSelection.WindowHandle != _lastExternalWindowHandle)
        {
            return null;
        }

        return _lastExternalSelection;
    }

    private void RememberExternalSelection(SelectionImportResult selection)
    {
        _lastExternalSelection = selection;
        if (selection.WindowHandle != IntPtr.Zero && selection.WindowHandle != _windowHandle)
        {
            _lastExternalWindowHandle = selection.WindowHandle;
        }
    }

    private Task<SelectionImportResult?> ResolvePreferredExternalSelectionAsync(string consumer) => Task.FromResult(ResolvePreferredExternalSelection(consumer));

    private SelectionImportResult? ResolvePreferredExternalSelection(string consumer)
    {
        _isResolvingExternalSelection = true;
        try
        {
            var foreground = _selectionImportService.CurrentForegroundWindow;
            if (_viewModel.IsExternalSuggestionSessionActive && foreground != _viewModel.ExternalSuggestionWindowHandle)
            {
                _viewModel.EndExternalSuggestionSession(hideOverlayWhenPossible: true);
                _lastExternalSuggestionAnchor = null;
                SyncOverlayVisibility();
            }
            if (foreground != IntPtr.Zero && !WindowsNativeTextAdapter.IsOwnWindow(foreground))
            {
                var liveExternalSelection = _selectionImportService.TryReadSelectionFromForegroundWindow(
                    _windowHandle,
                    emitDiagnostics: true);
                if (liveExternalSelection is not null)
                {
                    RememberExternalSelection(liveExternalSelection);
                    WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                        DateTimeOffset.Now,
                        "Resolve",
                        "LiveExternalSuccess",
                        $"{consumer} will use live external selection from hwnd=0x{liveExternalSelection.WindowHandle.ToInt64():X} with {liveExternalSelection.Text.Length} characters."));
                    return liveExternalSelection;
                }
            }

            var recentExternalSelection = TryGetRecentExternalSelection();
            if (recentExternalSelection is not null)
            {
                WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                    DateTimeOffset.Now,
                    "Resolve",
                    "CachedSuccess",
                    $"Recent cached selection prepared for {consumer} from {recentExternalSelection.Source} with {recentExternalSelection.Text.Length} characters from hwnd=0x{recentExternalSelection.WindowHandle.ToInt64():X}."));
                return recentExternalSelection;
            }

            var lastWindowSelection = _selectionImportService.TryReadSelectionFromWindowHandle(
                _lastExternalWindowHandle,
                _windowHandle,
                "last external target",
                emitDiagnostics: true);
            if (lastWindowSelection is not null)
            {
                RememberExternalSelection(lastWindowSelection);
                WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                    DateTimeOffset.Now,
                    "Resolve",
                    "LastWindowSuccess",
                    $"{consumer} resolved selection from last external window hwnd=0x{lastWindowSelection.WindowHandle.ToInt64():X} with {lastWindowSelection.Text.Length} characters."));
                return lastWindowSelection;
            }

            // Clipboard copy is an explicit user action from the import menu.
            SelectionImportResult? clipboardSelection = null;
            if (clipboardSelection is not null)
            {
                RememberExternalSelection(clipboardSelection);
                WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                    DateTimeOffset.Now,
                    "Resolve",
                    "ClipboardSuccess",
                    $"{consumer} resolved selection via clipboard fallback from hwnd=0x{clipboardSelection.WindowHandle.ToInt64():X} with {clipboardSelection.Text.Length} characters."));
                return clipboardSelection;
            }

            WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                DateTimeOffset.Now,
                "Resolve",
                "NoSelection",
                $"{consumer} found no external selection. lastExternalWindow=0x{_lastExternalWindowHandle.ToInt64():X}"));
            return null;
        }
        finally
        {
            _isResolvingExternalSelection = false;
        }
    }

    private int GetEditorCaretIndex()
    {
        return GetTextOffset(EditorTextBox.CaretPosition);
    }

    private int GetTextOffset(TextPointer pointer)
    {
        var text = new TextRange(EditorTextBox.Document.ContentStart, pointer).Text;
        // Select-all includes WPF's final paragraph terminator, which is absent
        // from the plain-text/Core buffer. All offsets use that buffer's bounds.
        return Math.Clamp(text.Length, 0, GetEditorPlainText().Length);
    }

    private void SetEditorCaretIndex(int index)
    {
        var pointer = GetTextPointerAtCharOffset(Math.Clamp(index, 0, GetEditorPlainText().Length));
        _isUpdatingEditorSelection = true;
        try
        {
            EditorTextBox.CaretPosition = pointer;
            EditorTextBox.Selection.Select(pointer, pointer);
        }
        finally { _isUpdatingEditorSelection = false; }
    }

    private void SetEditorSelection(int start, int length)
    {
        var safeStart = Math.Clamp(start, 0, GetEditorPlainText().Length);
        var safeEnd = Math.Clamp(safeStart + length, safeStart, GetEditorPlainText().Length);
        var startPointer = GetTextPointerAtCharOffset(safeStart);
        var endPointer = GetTextPointerAtCharOffset(safeEnd);
        EditorTextBox.Selection.Select(startPointer, endPointer);
        EditorTextBox.CaretPosition = endPointer;
    }

    private void ApplyEditorColoring() => RenderDocumentAnalysis();

    private void PreparePreciseSpeechHighlight(int baseOffset)
    {
        _speechHighlightTimer.Stop();
        _preciseSpeechHighlightTimer.Stop();
        ClearCurrentSpeechHighlight();
        _speechHighlightSpans = [];
        _speechHighlightDuration = TimeSpan.Zero;
        _preciseSpeechCues.Clear();
        _nextPreciseSpeechCueIndex = 0;
        _preciseSpeechPlaybackStartedAt = default;
        _preciseSpeechHighlightBaseOffset = Math.Max(0, baseOffset);
        _usePreciseSpeechHighlight = true;
        WriteTtsFlowDiagnostic($"Precise speech highlight armed: baseOffset={_preciseSpeechHighlightBaseOffset}.");
    }

    private void StartSpeechHighlight(string text, int baseOffset, TtsSpeechOptions options)
    {
        _usePreciseSpeechHighlight = false;
        var readingHighlightMode = _viewModel.LoadSettingsSnapshot().ReadingHighlightMode;
        if (string.Equals(readingHighlightMode, "none", StringComparison.OrdinalIgnoreCase))
        {
            WriteTtsFlowDiagnostic("Speech highlight skipped: reading highlight mode is none.");
            return;
        }

        var spans = BuildSpeechHighlightSpans(text, baseOffset, readingHighlightMode);
        if (spans.Length == 0)
        {
            WriteTtsFlowDiagnostic("Speech highlight skipped: no word tokens found.");
            return;
        }

        ClearCurrentSpeechHighlight();
        _speechHighlightSpans = spans;
        _speechHighlightStartedAt = DateTimeOffset.Now;
        _speechHighlightDuration = EstimateSpeechDuration(text, spans.Length, options);
        _speechHighlightTimer.Start();
        ApplySpeechHighlight(spans[0]);
        WriteTtsFlowDiagnostic(
            $"Speech highlight started: tokens={spans.Length}, baseOffset={baseOffset}, estimatedMs={_speechHighlightDuration.TotalMilliseconds:F0}.");
    }

    private void StopSpeechHighlight(bool restoreEditorSelection)
    {
        restoreEditorSelection &= SpeechPresentationIsCurrent();
        _speechEditorGeneration++;
        _externalAutomaticSpeech = false;
        _automaticEditorSpeech = false;
        _speechCommittedPrefix = _externalSpeechCommittedPrefix = "";
        _externalSpeechTarget = null;
        ClearSpeechPresentation();
        _viewModel.SetTextToSpeechSpeaking(false, _viewModel.StatusMessage);
        _speechHighlightTimer.Stop();
        _preciseSpeechHighlightTimer.Stop();
        ClearCurrentSpeechHighlight();
        _speechHighlightSpans = [];
        _speechHighlightDuration = TimeSpan.Zero;
        _preciseSpeechCues.Clear();
        _nextPreciseSpeechCueIndex = 0;
        _preciseSpeechPlaybackStartedAt = default;
        _usePreciseSpeechHighlight = false;

        if (restoreEditorSelection)
        {
            RestoreSpeechEditorSelection();
        }
        _speechSelectionRangeToRestore = null;
        _speechEditor = null;
    }

    private void StartPreciseSpeechHighlightPlayback()
    {
        if (!_usePreciseSpeechHighlight)
        {
            return;
        }

        _preciseSpeechPlaybackStartedAt = DateTimeOffset.Now;
        _nextPreciseSpeechCueIndex = 0;
        WriteTtsFlowDiagnostic($"Precise speech highlight playback started: cues={_preciseSpeechCues.Count}.");
        ScheduleNextPreciseSpeechHighlight();
    }

    private void PreciseSpeechHighlightTimerOnTick(object? sender, EventArgs e)
    {
        _preciseSpeechHighlightTimer.Stop();
        if (!_usePreciseSpeechHighlight || _nextPreciseSpeechCueIndex >= _preciseSpeechCues.Count)
        {
            return;
        }

        var cue = _preciseSpeechCues[_nextPreciseSpeechCueIndex++];
        ApplySpeechHighlight(new TextSpan(cue.Start, cue.Length, string.Empty));
        ScheduleNextPreciseSpeechHighlight();
    }

    private void ScheduleNextPreciseSpeechHighlight()
    {
        if (!_usePreciseSpeechHighlight ||
            _preciseSpeechPlaybackStartedAt == default ||
            _nextPreciseSpeechCueIndex >= _preciseSpeechCues.Count)
        {
            return;
        }

        var nextCue = _preciseSpeechCues[_nextPreciseSpeechCueIndex];
        var elapsed = DateTimeOffset.Now - _preciseSpeechPlaybackStartedAt;
        var remaining = nextCue.StartTime - elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            var cue = _preciseSpeechCues[_nextPreciseSpeechCueIndex++];
            ApplySpeechHighlight(new TextSpan(cue.Start, cue.Length, string.Empty));
            ScheduleNextPreciseSpeechHighlight();
            return;
        }

        _preciseSpeechHighlightTimer.Interval = remaining;
        _preciseSpeechHighlightTimer.Start();
    }

    private void SpeechHighlightTimerOnTick(object? sender, EventArgs e)
    {
        if (_speechHighlightSpans.Count == 0 || _speechHighlightDuration <= TimeSpan.Zero)
        {
            return;
        }

        var elapsed = DateTimeOffset.Now - _speechHighlightStartedAt;
        var progress = Math.Clamp(elapsed.TotalMilliseconds / _speechHighlightDuration.TotalMilliseconds, 0.0, 0.98);
        var index = Math.Clamp((int)Math.Floor(progress * _speechHighlightSpans.Count), 0, _speechHighlightSpans.Count - 1);
        ApplySpeechHighlight(_speechHighlightSpans[index]);
    }

    private void ApplySpeechHighlight(TextSpan span)
    {
        if (!SpeechPresentationIsCurrent() || !WindowsTextToSpeechService.ValidRange(GetEditorPlainText(), span.Start, span.Length)) return;
        if (_currentSpeechHighlightSpan is not null &&
            _currentSpeechHighlightSpan.Start == span.Start &&
            _currentSpeechHighlightSpan.Length == span.Length)
        {
            return;
        }

        ClearCurrentSpeechHighlight();
        var start = GetTextPointerAtCharOffset(span.Start);
        var end = GetTextPointerAtCharOffset(span.Start + span.Length);
        SelectSpeechHighlightRange(start, end);
        _currentSpeechHighlightSpan = span;
    }

    private void ClearCurrentSpeechHighlight()
    {
        if (_currentSpeechHighlightSpan is null)
        {
            return;
        }

        _currentSpeechHighlightSpan = null;
        _speechAdorner?.SetRange(null, null);
    }

    private void SelectSpeechHighlightRange(TextPointer start, TextPointer end)
    {
        _speechAdorner?.SetRange(start, end);
    }

    private void RestoreSpeechEditorSelection()
    {
        // The adorner never moved selection/caret; retain the exact user state.
        _speechSelectionRangeToRestore = null;
    }

    private static TimeSpan EstimateSpeechDuration(string text, int wordCount, TtsSpeechOptions options)
    {
        const double baseWordsPerMinute = 125.0;
        var speedMultiplier = options.UseSystemSpeechSettings
            ? 1.0
            : SpeechSpeedPolicy.Multiplier(options.ReadingSpeedDelta);
        var wordsDurationMs = wordCount / (baseWordsPerMinute * speedMultiplier) * 60_000.0;
        var punctuationPauseMs = text.Count(character => character is '.' or ',' or ';' or ':' or '!' or '?') * 120.0;
        var bridgeWarmupMs = wordCount <= 4 ? 1_000.0 : 550.0;
        return TimeSpan.FromMilliseconds(Math.Max(2_400.0, bridgeWarmupMs + wordsDurationMs + punctuationPauseMs));
    }

    private TextPointer GetTextPointerAtCharOffset(int charOffset)
    {
        var wanted = Math.Clamp(charOffset, 0, GetEditorPlainText().Length);
        var pointer = EditorTextBox.Document.ContentStart;
        // TextRange counts CRLF between paragraphs but not formatting symbols.
        // Compare against that same representation for every offset, including emoji.
        while (pointer is not null)
        {
            var offset = new TextRange(EditorTextBox.Document.ContentStart, pointer).Text.Length;
            if (offset >= wanted && pointer.IsAtInsertionPosition) return pointer;
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var runLength = pointer.GetTextRunLength(LogicalDirection.Forward);
                if (wanted >= offset && wanted <= offset + runLength)
                    return pointer.GetPositionAtOffset(wanted - offset, LogicalDirection.Forward)!.GetInsertionPosition(LogicalDirection.Forward);
            }
            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        }
        return EditorTextBox.Document.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
    }

    private static string NormalizeRichTextBoxText(string text)
    {
        if (text.EndsWith("\r\n", StringComparison.Ordinal))
        {
            return text[..^2];
        }

        if (text.EndsWith('\n'))
        {
            return text[..^1];
        }

        return text;
    }

    private EditorSelectionRange CaptureCurrentEditorSelectionRange()
    {
        var start = GetTextOffset(EditorTextBox.Selection.Start);
        var end = GetTextOffset(EditorTextBox.Selection.End);
        var safeStart = Math.Min(start, end);
        return new EditorSelectionRange(safeStart, Math.Abs(end - start));
    }

    private static TextSpan[] BuildSpeechHighlightSpans(string text, int baseOffset, string readingHighlightMode)
    {
        if (string.Equals(readingHighlightMode, "sentence", StringComparison.OrdinalIgnoreCase))
        {
            return SentenceTokenRegex()
                .Matches(text)
                .Select(match =>
                {
                    var leadingWhitespace = match.Value.Length - match.Value.TrimStart().Length;
                    var trimmed = match.Value.Trim();
                    return new TextSpan(baseOffset + match.Index + leadingWhitespace, trimmed.Length, trimmed);
                })
                .Where(span => span.Length > 0)
                .ToArray();
        }

        return WordTokenRegex()
            .Matches(text)
            .Select(match => new TextSpan(baseOffset + match.Index, match.Length, match.Value))
            .ToArray();
    }

    private static SolidColorBrush BrushFromHex(string hex) =>
        (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    [GeneratedRegex(@"[\p{L}\p{M}\p{Nd}][\p{L}\p{M}\p{Nd}'-]*", RegexOptions.CultureInvariant)]
    private static partial Regex WordTokenRegex();

    [GeneratedRegex(@"[^.!?\r\n]+[.!?]*", RegexOptions.CultureInvariant)]
    private static partial Regex SentenceTokenRegex();

    private void SyncOverlayVisibility()
    {
        if (_analysisMenu?.IsOpen == true) { HideOverlayWindow(); return; }
        if (!_viewModel.ShouldShowSuggestionOverlay)
        {
            HideOverlayWindow();
            return;
        }

        EnsureOverlayWindow();
        if (_viewModel.IsExternalSuggestionSessionActive && _viewModel.HasSuggestions && _viewModel.IsSuggestionInteractionEnabled) RegisterExternalSuggestionHotKeys();
        else UnregisterExternalSuggestionHotKeys();
        UpdateOverlayPosition();

        if (_overlayWindow is not null && !_overlayWindow.IsVisible)
        {
            _overlayWindow.Show();
            RecordDiagnosticPanelState();
            WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                DateTimeOffset.Now,
                "SuggestionOverlay",
                "Shown",
                $"Overlay shown. externalSession={_viewModel.IsExternalSuggestionSessionActive}, topmost={_overlayWindow.Topmost}, owner={( _overlayWindow.Owner is null ? "none" : "main-window")}"));
        }
    }

    private void EnsureOverlayWindow()
    {
        if (_overlayWindow is not null)
        {
            return;
        }

        _overlayWindow = new SuggestionOverlayWindow(_viewModel)
        {
            Topmost = true
        };
        _overlayWindow.ManualPlacementCommitted += OverlayWindowOnManualPlacementCommitted;
        _overlayWindow.SpeechRequested += OverlaySpeechRequested;
        _overlayWindow.HoverSpeechRequested += OverlayHoverSpeechRequested;
        _overlayWindow.HoverSpeechCancelled += OverlayHoverSpeechCancelled;
        WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
            DateTimeOffset.Now,
            "SuggestionOverlay",
            "Created",
            "Created overlay window without owner so it can float above external apps."));
    }

    private void HideOverlayWindow()
    {
        if (_overlayWindow is not null && _overlayWindow.IsVisible)
        {
            UnregisterExternalSuggestionHotKeys();
            _overlayWindow.Hide();
            RecordDiagnosticPanelState();
            WriteSelectionImportDiagnostic(new SelectionImportDiagnostic(
                DateTimeOffset.Now,
                "SuggestionOverlay",
                "Hidden",
                "Overlay window was hidden because the suggestion session is no longer visible."));
        }
    }

    private void UpdateOverlayPosition()
    {
        if (_overlayWindow is null || !_viewModel.ShouldShowSuggestionOverlay) return;
        using var measurement = _viewModel.Diagnostics.Measure(DiagnosticMetric.OverlayPlacement);
        var confirmed = _viewModel.RequestsCaretGeometry && (_viewModel.IsExternalSuggestionSessionActive
            ? _lastExternalSuggestionAnchor is { Quality: SuggestionAnchorQuality.Confirmed }
            : TryGetCaretScreenRect(out _));
        var follow = _viewModel.IsFollowCaretPlacementMode && confirmed;
        RecordDiagnosticPlacement(follow);
        var anchor = follow ? ResolveOverlayAnchor() : _manualOverlayTopLeft ?? DefaultStaticPosition();
        ScreenPlacement.Place(_overlayWindow, anchor, follow);
    }

    private Point DefaultStaticPosition()
    {
        var toolbar = ScreenPlacement.TopLeft(this);
        return new Point(toolbar.X + 24, toolbar.Y + 88);
    }

    private Point ResolveOverlayAnchor()
    {
        if (_viewModel.IsExternalSuggestionSessionActive)
        {
            if (_lastExternalSuggestionAnchor is { Quality: SuggestionAnchorQuality.Confirmed } confirmedAnchor)
            {
                // Exact caret position from TextPattern (Word, Notepad, terminals, etc.)
                return new Point(
                    confirmedAnchor.ScreenRect.Left + (confirmedAnchor.ScreenRect.Width / 2),
                    confirmedAnchor.ScreenRect.Bottom);
            }

            var fallback = _manualOverlayTopLeft ?? new Point(Left + 24, Top + 88);
            return fallback;

        }

        if (_viewModel.IsFollowCaretPlacementMode &&
            TryGetCaretScreenRect(out var caretRect))
        {
            return new Point(caretRect.Left + (caretRect.Width / 2), caretRect.Bottom);
        }

        return _manualOverlayTopLeft ?? new Point(Left + 24, Top + 88);
    }

    private void CaptureOrInitializeStaticOverlayPosition()
    {
        if (_overlayWindow is not null && _overlayWindow.IsVisible)
        {
            _manualOverlayTopLeft = ScreenPlacement.TopLeft(_overlayWindow);
            return;
        }

        _manualOverlayTopLeft ??= DefaultStaticPosition();
    }

    private void OverlayWindowOnManualPlacementCommitted(object? sender, Point topLeft)
    {
        if (_overlayWindow is null)
        {
            return;
        }

        _manualOverlayTopLeft = ScreenPlacement.Place(_overlayWindow, topLeft, false);
        _viewModel.SaveOverlayPosition(_manualOverlayTopLeft.Value.X, _manualOverlayTopLeft.Value.Y);
        _viewModel.SetStatusMessage("Ordforslagsboksen blev flyttet i statisk placering.");
    }

    private bool TryGetCaretScreenRect(out Rect caretRect)
    {
        caretRect = Rect.Empty;
        if (!_viewModel.RequestsCaretGeometry) return false;

        if (!EditorTextBox.IsLoaded)
        {
            return false;
        }

        var rect = EditorTextBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
        if (rect.IsEmpty)
        {
            rect = EditorTextBox.CaretPosition.GetCharacterRect(LogicalDirection.Backward);
        }

        if (rect.IsEmpty)
        {
            return false;
        }

        var topLeft = EditorTextBox.PointToScreen(new Point(rect.Left, rect.Top));
        var bottomRight = EditorTextBox.PointToScreen(new Point(rect.Right, rect.Bottom));
        caretRect = new Rect(topLeft.X, topLeft.Y, Math.Max(1, bottomRight.X - topLeft.X), Math.Max(20, bottomRight.Y - topLeft.Y));
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private sealed record TextSpan(int Start, int Length, string Text);

    private sealed record EditorSelectionRange(int Start, int Length);
}
