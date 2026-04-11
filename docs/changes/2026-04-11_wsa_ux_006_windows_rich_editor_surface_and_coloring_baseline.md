# WSA-UX-006_windows_rich_editor_surface_and_coloring_baseline

Date: `2026-04-11`
Status: `Done`

## Summary

Changed the internal Windows editor from a plain text box to a fixed-size rich text surface that fills the available expanded-window space, wraps horizontally, scrolls vertically, and provides a first visible word-coloring baseline.

## User-visible behavior

- The internal editor input field now occupies the stable available space between the command row and the status/analyzer panels.
- Text wraps to the editor width instead of requiring horizontal scrolling.
- Long text scrolls vertically inside the editor field.
- Words now receive visible POS-style colors when the `Farver` toggle is active.
- The status metrics and text analysis legend remain visible below the editor field.

## Implementation details

- Replaced the WPF `TextBox` with a `RichTextBox`, because single-word coloring is not possible in the old plain text control.
- Added manual text synchronization between the `RichTextBox` document and `MainWindowViewModel.EditorText`.
- Preserved caret-index tracking so suggestions, accept flow, and overlay placement still work with the new rich editor.
- Reworked caret anchoring to use `RichTextBox.CaretPosition.GetCharacterRect`.
- Added a first local Windows classifier for visible POS-style coloring. It uses the same color categories as the analyzer legend and keeps the code isolated to the Windows host.

## Files changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/Plan.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap -SampleText "Jeg skriver den blå bog og læser hurtigt"` -> `PASS`
- Recent Application event log check after launch showed no new `WordSuggestorWindows.App` crash event.

## Follow-up

- Replace the heuristic Windows-side coloring baseline with the full lexicon-backed analyzer semantics from the macOS `TextAnalyzer`.
- Re-test and harden caret index mapping for long multi-paragraph documents.
