# WSA-UX-009_windows_editor_vertical_space_and_statusbar_tuning

Date: `2026-04-11`
Status: `Done`

## Summary

Tuned the expanded Windows editor layout so the editor input consumes the remaining vertical space and the status bar reads more like the macOS/Word-style inline status strip.

## Steps

### WSA-UX-009_step_01_stretch_editor_region

Commit message:

```text
WSA-UX-009 Step 01 Stretch expanded editor region
```

Summary:

- Changed the shell grid's expanded editor row from `Auto` to `*`.
- Set the expanded editor container and inner grid to stretch vertically.
- This lets the existing `RichTextBox` star row receive the extra height instead of leaving unused space below the analyzer legend.

### WSA-UX-009_step_02_statusbar_macos_word_like_tuning

Commit message:

```text
WSA-UX-009 Step 02 Tune compact editor status bar
```

Summary:

- Adjusted the inline status metric order from `Icon Label Value` to `Icon Value Label`, closer to the macOS reference format.
- Removed the per-item separator line from the status bar.
- Tightened status bar and analyzer panel vertical padding.

### WSA-UX-009_step_03_documentation_and_validation

Commit message:

```text
WSA-UX-009 Step 03 Document editor space tuning
```

Summary:

- Updated the active plan with scope, implementation notes, validation, and the build-lock note.
- Updated the manual smoke checklist to verify editor vertical stretch and inline status bar presentation.
- Added this change note with per-step commit messages.

## Files Changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-11_wsa_ux_009_windows_editor_vertical_space_and_statusbar_tuning.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- Application event log check after the launch smoke showed no new `WordSuggestorWindows.App` crash event.

## Follow-Up

- Capture a fresh Windows screenshot after launch/expand and compare against `macos-ui.png` for final spacing calibration.
