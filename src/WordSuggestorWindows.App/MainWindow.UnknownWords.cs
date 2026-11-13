using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App;

public partial class MainWindow
{
    private AutomaticWordLearning? _automaticWords;
    private UnknownWordSharingClient? _wordSharing;
    private readonly System.Windows.Threading.DispatcherTimer _sharingRetryTimer=new(){Interval=TimeSpan.FromSeconds(30)};
    private (RichTextBox Editor,string Expected,int Caret,long Time)? _unknownInput;
    private ExternalTextSnapshot? _unknownExternalBaseline;
    private long _unknownGeneration;
    private string _unknownLastStatus="idle";
    private void InitializeUnknownWords()
    {
        if(_viewModel.UnknownWordCore is ICoreCompletedWordAdapter boundary)
            _automaticWords=new(boundary,_viewModel.UnknownWordCore,_viewModel.SettingsService,new());
        if(UnknownWordSharingDeployment.Qualified&&_viewModel.UnknownWordCore is ICoreUnknownWordHandoffAdapter handoff)
        {
            _wordSharing=new(handoff,new AccountUnknownWordSharingSender(_viewModel.AccountSession),()=>_viewModel.LoadSettingsSnapshot() is {IsUnknownWordLookupEnabled:true,IsUnknownWordCrowdHandoffEnabled:true,UnknownWordSharingConsentVersion:1} permission&&permission.UnknownWordSharingConsentUserId==_viewModel.AccountSession.Profile?.UserId,()=>UnknownWordSharingDeployment.Qualified,WindowsDataPaths.Root);
            LearnedWordStore.Changed+=LearnedWordsChanged;
            _sharingRetryTimer.Tick+=SharingRetryTick;_sharingRetryTimer.Start();
        }
        _viewModel.SettingsChanged+=UnknownSettingsChanged;
        _editorWindow.Deactivated+=UnknownEditorDeactivated;
    }
    private void UnknownSettingsChanged(object? sender,AppSettingsSnapshot settings){_unknownGeneration++;_automaticWords?.Cancel();_unknownInput=null;_unknownExternalBaseline=null;_wordSharing?.Cancel();if(!settings.IsUnknownWordCrowdHandoffEnabled&&_wordSharing is not null)_= _wordSharing.RevokeAsync();}
    private async void SharingRetryTick(object? sender,EventArgs args)=>await SynchronizeWordSharing();
    private async void LearnedWordsChanged(object? sender,string path)
    {
        if(_wordSharing is null||path!=LearnedWordStore.PathFor(WindowsDataPaths.Root))return;
        _wordSharing.Cancel();await SynchronizeWordSharing();
    }
    private async Task SynchronizeWordSharing()
    {
        if(_wordSharing is null)return;
        var status=await _wordSharing.SynchronizeAsync(new(Guid.NewGuid(),_unknownGeneration),WindowsUpdateService.DisplayVersion);
        if(status is "account_mismatch" or "rejected" or "login_or_permission_required" or "retry_exhausted" or "unavailable")_viewModel.SetStatusMessage(SettingsText.Get("UnknownSharingUnavailable"));
    }
    private void UnknownEditorDeactivated(object? sender,EventArgs args){_automaticWords?.Revalidate();_unknownInput=null;}
    private void RememberUnknownInput(string inserted)
    {
        _unknownInput=null;
        if(!_editorWindow.IsActive||!EditorTextBox.IsKeyboardFocusWithin||!EditorTextBox.Selection.IsEmpty||!TypingReadAloudPolicy.IsBoundary(inserted))return;
        var text=GetEditorPlainText();var caret=GetEditorCaretIndex();
        _unknownInput=(EditorTextBox,text[..caret]+inserted+text[caret..],caret+inserted.Length,Environment.TickCount64);
    }
    private async void ObserveUnknownEditorInput()
    {
        _automaticWords?.Revalidate();var receipt=_unknownInput;_unknownInput=null;
        if(_automaticWords is null||_isSynchronizingEditorDocument||receipt is not {} r||!ReferenceEquals(r.Editor,EditorTextBox)||Environment.TickCount64-r.Time>1000||GetEditorPlainText()!=r.Expected)return;
        var editor=EditorTextBox;var tab=_workspace.Active;var generation=_unknownGeneration;var language=_viewModel.SelectedLanguageOption.LanguageCode;
        var prefix=r.Expected[..r.Caret];
        bool Current()=>generation==_unknownGeneration&&ReferenceEquals(editor,EditorTextBox)&&ReferenceEquals(tab,_workspace.Active)&&_editorWindow.IsActive&&_selectionImportService.CurrentForegroundWindow==new System.Windows.Interop.WindowInteropHelper(_editorWindow).Handle&&editor.IsKeyboardFocusWithin&&editor.Selection.IsEmpty&&
            GetEditorCaretIndex()>=r.Caret&&GetEditorPlainText().StartsWith(prefix,StringComparison.Ordinal)&&_viewModel.SelectedLanguageOption.LanguageCode==language;
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
        _unknownLastStatus="observing";
        var status=await _automaticWords.ObserveAsync(prefix,new(tab.Id,generation),Current);
        _unknownLastStatus=status;
        if(status=="unavailable")_viewModel.SetStatusMessage(SettingsText.Get("UnknownUnavailable"));
        if(status=="saved")_viewModel.RefreshSuggestionsPreview();
    }
    private async void ObserveUnknownExternal(ExternalTextSnapshot? snapshot)
    {
        _automaticWords?.Revalidate();var before=_unknownExternalBaseline;_unknownExternalBaseline=snapshot;
        if(_automaticWords is null||snapshot is null||before is null||!_viewModel.IsGlobalCaptureEnabled||snapshot.Start!=snapshot.End||before.Start!=before.End||
            snapshot.Window!=before.Window||snapshot.Control!=before.Control||snapshot.ProcessId!=before.ProcessId||snapshot.ProcessStarted!=before.ProcessStarted)return;
        var delta=snapshot.Text.Length-before.Text.Length;
        if(delta is <1 or >2||snapshot.End!=before.End+delta||snapshot.Text[..before.End]!=before.Text[..before.End]||snapshot.Text[snapshot.End..]!=before.Text[before.End..]||!TypingReadAloudPolicy.IsBoundary(snapshot.Text.Substring(before.End,delta)))return;
        var prefix=snapshot.Text[..snapshot.End];var generation=_unknownGeneration;var language=_viewModel.SelectedLanguageOption.LanguageCode;
        bool Current()
        {
            if(generation!=_unknownGeneration||!_viewModel.IsGlobalCaptureEnabled||_viewModel.SelectedLanguageOption.LanguageCode!=language)return false;
            var actual=new WindowsNativeTextAdapter().ReadFocused();
            return actual is not null&&actual.Window==snapshot.Window&&actual.Control==snapshot.Control&&actual.ProcessId==snapshot.ProcessId&&actual.ProcessStarted==snapshot.ProcessStarted&&actual.Start==actual.End&&actual.End>=prefix.Length&&actual.Text.StartsWith(prefix,StringComparison.Ordinal);
        }
        await _automaticWords.ObserveAsync(prefix,new(Guid.NewGuid(),generation),Current);
    }
    private void DisposeUnknownWords()
    {
        _sharingRetryTimer.Stop();_sharingRetryTimer.Tick-=SharingRetryTick;
        LearnedWordStore.Changed-=LearnedWordsChanged;_wordSharing?.Dispose();
        _unknownGeneration++;_automaticWords?.Dispose();_viewModel.SettingsChanged-=UnknownSettingsChanged;_editorWindow.Deactivated-=UnknownEditorDeactivated;
    }
}
