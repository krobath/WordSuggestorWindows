# WSA-UX-008_windows_editor_layout_statusbar_startup_parity

Date: `2026-04-11`
Status: `Done`

## Summary

Aligned the Windows editor startup and lower editor layout more closely with the macOS reference: the app now starts collapsed, the status metrics render as one compact status bar, and the text analysis panel no longer shows an implementation note.

## Steps

### WSA-UX-008_step_01_collapsed_startup

Commit message:

```text
WSA-UX-008 Step 01 Keep startup shell collapsed
```

Summary:

- Preserved injected startup text in `MainWindowViewModel`.
- Stopped startup sample text from forcing `IsEditorExpanded = true`.
- Kept suggestion refresh warm-up for the injected sample so suggestions are ready when the user opens the editor.

### WSA-UX-008_step_02_editor_statusbar_density

Commit message:

```text
WSA-UX-008 Step 02 Rebalance editor statusbar layout
```

Summary:

- Replaced the four separate status metric cards with one compact horizontal status bar.
- Removed implementation-note copy from the `Tekstanalyse` panel.
- Reduced lower editor padding and margins so the `RichTextBox` receives more vertical space.

### WSA-UX-008_step_03_documentation_and_smoke_contract

Commit message:

```text
WSA-UX-008 Step 03 Document editor layout parity contract
```

Summary:

- Updated the active Windows plan with the new sprint scope, implementation notes, validation, and known visual follow-up.
- Updated the manual smoke checklist to expect collapsed startup, manual editor expansion, one status bar, and a note-free `Tekstanalyse` panel.
- Added this sprint change note with per-step commit messages and summaries.

## Files Changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-11_wsa_ux_008_windows_editor_layout_statusbar_startup_parity.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS`

## Follow-Up

- Compare a fresh Windows screenshot with the macOS reference and tune exact status bar spacing, editor height, and legend wrapping if needed.
