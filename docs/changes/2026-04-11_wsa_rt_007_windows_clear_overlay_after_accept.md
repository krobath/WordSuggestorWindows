# WSA-RT-007_windows_clear_overlay_after_accept

Date: `2026-04-11`
Status: `Done`

## Summary

Cleared the floating suggestion overlay immediately after accepting a suggestion, so the previous token's candidates are not shown after the accepted word and trailing space are inserted.

## Steps

### WSA-RT-007_step_01_clear_overlay_session_after_accept

Commit message:

```text
WSA-RT-007 Step 01 Clear suggestion session after accept
```

Summary:

- Added `ClearSuggestionSession()` to `MainWindowViewModel`.
- Updated `ExecuteAcceptSelectedSuggestion()` to preserve accepted-word insertion and caret placement, then clear the current suggestion list.
- Cancelled the pending refresh created by the `EditorText` update during accept so stale candidates do not immediately reappear.
- Reset selected suggestion, current page, busy state, and overlay notifications.

### WSA-RT-007_step_02_documentation_and_smoke_contract

Commit message:

```text
WSA-RT-007 Step 02 Document accept clears overlay
```

Summary:

- Updated the active Windows plan with the accept/clear overlay runtime contract.
- Updated the manual smoke checklist to verify that stale candidates disappear after `Tab` or `Ctrl+1` accept.
- Added this sprint change note with per-step commit messages and validation.

## Files Changed

- `WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `WordSuggestorWindows/docs/Plan.md`
- `WordSuggestorWindows/docs/ManualSmoke.md`
- `WordSuggestorWindows/docs/changes/2026-04-11_wsa_rt_007_windows_clear_overlay_after_accept.md`

## Validation

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS`
- Application event log check after the launch smoke showed no new `WordSuggestorWindows.App` crash event.

## Follow-Up

- Manually smoke-test `Tab`, `Ctrl+1`, and overlay mouse click accept paths in the internal editor.
