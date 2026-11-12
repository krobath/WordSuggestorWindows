using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App;

public partial class MainWindow
{
    private DiagnosticEvent? _diagnosticPanelState, _diagnosticPlacementState;
    private void RecordDiagnosticPanelState()
    {
        if (!_viewModel.Diagnostics.Enabled(DiagnosticCategory.Panel)) { _diagnosticPanelState = null; return; }
        var state = _overlayWindow?.IsVisible != true ? DiagnosticEvent.OverlayHidden : _viewModel.IsExternalSuggestionSessionActive ? DiagnosticEvent.OverlayExternal : DiagnosticEvent.OverlayLocal;
        if (_diagnosticPanelState == state) return;
        _diagnosticPanelState = state; _viewModel.Diagnostics.Record(DiagnosticCategory.Panel, state);
    }
    private void RecordDiagnosticPlacement(bool follow)
    {
        if (!_viewModel.Diagnostics.Enabled(DiagnosticCategory.Placement)) { _diagnosticPlacementState = null; return; }
        var state = follow ? DiagnosticEvent.CaretConfirmed : _viewModel.IsFollowCaretPlacementMode ? DiagnosticEvent.CaretFallback : DiagnosticEvent.StaticPlacement;
        if (_diagnosticPlacementState == state) return;
        _diagnosticPlacementState = state; _viewModel.Diagnostics.Record(DiagnosticCategory.Placement, state);
    }
}
