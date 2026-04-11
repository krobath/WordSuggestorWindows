# WSA-UX-007_windows_rich_editor_startup_fix

Date: `2026-04-11`
Status: `Done`

## Summary

Fixed the Windows app startup regression introduced by the new rich editor surface. The app no longer exits immediately after `scripts\run_app.ps1` prints the launch message.

## Diagnosis

- The PowerShell launch script reached `Launching WordSuggestorWindows.App`, then returned to the prompt because the WPF process crashed during startup.
- The Windows Application event log showed a `.NET Runtime` crash with `System.NullReferenceException`.
- The stack trace pointed to `MainWindow.EditorTextBox_OnTextChanged` while WPF was still loading `MainWindow.xaml`.
- Root cause: assigning `RichTextBox.Document` during XAML loading raises `TextChanged` before the constructor had assigned `_viewModel`.

## Implementation Details

- Moved `_viewModel = viewModel` before `InitializeComponent()` in `MainWindow`.
- Kept `DataContext = _viewModel` after `InitializeComponent()` so the existing binding lifecycle is preserved.
- Did not revert the `RichTextBox` editor or the word-coloring baseline from `WSA-UX-006`.

## Files Changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS`
- Fixed launches left a responsive `WordSuggestorWindows.App` process.
- Application event log check after the fixed launch showed no new `WordSuggestorWindows.App` crash event.

## Follow-Up

- Continue the visual parity pass separately; this sprint is intentionally limited to restoring startup stability.
