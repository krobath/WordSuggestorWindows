# WSA-RT-008_windows_empty_overlay_boundary_state

Date: `2026-04-11`
Status: `Done`

## Summary

Changed the suggestion overlay boundary behavior so the panel remains visible but empty after `Space`, `Enter`, or accepting a suggestion. The old candidates are cleared immediately and new candidates appear when the next token begins.

## Steps

### WSA-RT-008_step_01_keep_overlay_visible_when_empty

Commit message:

```text
WSA-RT-008 Step 01 Keep overlay visible for empty boundary state
```

Summary:

- Added explicit overlay session visibility state to `MainWindowViewModel`.
- Decoupled `ShouldShowSuggestionOverlay` from `HasSuggestions`, allowing an empty overlay to remain visible.
- Added boundary-token detection for text that ends in whitespace, newline, or another non-token character.
- Kept the empty-document state hidden while keeping non-empty boundary states visible as an empty panel.

### WSA-RT-008_step_02_document_empty_overlay_contract

Commit message:

```text
WSA-RT-008 Step 02 Document empty overlay boundary state
```

Summary:

- Updated the active Windows plan with the refined overlay visibility contract.
- Updated manual smoke checks for accept, `Space`, and `Enter` boundary behavior.
- Added this change note with the new per-step commit messages.

## Files Changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-11_wsa_rt_008_windows_empty_overlay_boundary_state.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- Application event log check after the launch smoke showed no new `WordSuggestorWindows.App` crash event.

## Follow-Up

- Manually smoke-test `Space`, `Enter`, `Tab`, `Ctrl+1`, and overlay mouse-click accept paths to confirm the panel remains visible but empty at token boundaries.
