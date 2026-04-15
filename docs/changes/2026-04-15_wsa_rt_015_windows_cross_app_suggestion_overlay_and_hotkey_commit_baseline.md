# WSA-RT-015_windows_cross_app_suggestion_overlay_and_hotkey_commit_baseline

Date: `2026-04-15`
Status: `Done`

## Why

The Windows suggestion overlay had only been wired to the internal editor. macOS, by contrast, treats the suggestion panel as a global runtime surface that can follow the active typing context in other apps and still fall back safely when caret placement is unreliable.

This sprint establishes the first real Windows cross-app baseline so the overlay can participate in supported external app flows without forcing the user back into the internal editor first.

## Implemented

- Added a low-level Windows keyboard-capture service that tracks the active token in the foreground app while global suggestions are enabled.
- Added an external suggestion-session path in the main view-model so the floating overlay can stay active even when the internal editor is collapsed.
- Reduced external suggestion refresh latency to a faster `75 ms` debounce so the overlay feels responsive while the user types.
- Added UI Automation-based external anchor extraction with:
  - confirmed text-range anchor placement
  - approximate focused-element fallback placement
- Updated the overlay placement path so external sessions can drive follow-caret placement instead of relying only on the internal editor caret.
- Added global `Ctrl+1` through `Ctrl+0` accept shortcuts and `Ctrl+Left` / `Ctrl+Right` page shortcuts for active external suggestion sessions.
- Added a guarded external commit service that:
  - removes the tracked token length with backspace
  - injects the accepted suggestion text
  - appends one trailing space
- Preserved the existing internal editor overlay/session path; this sprint extends runtime scope instead of replacing the editor flow.
- Follow-up stabilization on `2026-04-15` removed the overlay `Owner` relationship to the main WordSuggestor window so the suggestion panel can float as a true external overlay above Word, Edge, and similar apps.
- Follow-up stabilization on `2026-04-15` also added token-, anchor-, and overlay-level diagnostics to `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log` so cross-app suggestion failures can be traced without guessing.
- Keyboard-to-text translation now resolves the active keyboard layout from the foreground thread instead of always using WordSuggestor's own thread layout, which is more robust for external typing contexts.

## Guardrails kept

- No `WordSuggestorCore` changes.
- No macOS code changes.
- No removal of the existing internal editor overlay behavior.
- External follow-caret still falls back to static placement when a trustworthy anchor cannot be extracted.

## Validation

- `dotnet build .\WordSuggestorWindows\src\WordSuggestorWindows.App\WordSuggestorWindows.App.csproj -t:Rebuild --no-restore /p:UseAppHost=false /p:OutDir=C:\Users\mswin01\Code\WordSuggestor\.build-validate\WordSuggestorWindows.App\`
- `git -C .\WordSuggestorWindows diff --check`

## Notes

- This sprint is a Windows baseline for cross-app suggestions, not the final compatibility closure for every custom editor/control stack.
- App-specific commit and caret quirks should continue to be tracked through targeted compatibility follow-ups after this runtime foundation.
- The latest debugging focus for `WSA-RT-015` is not suggestion generation speed but external host visibility: whether token capture fires, whether an external anchor can be resolved, and whether the now-unowned overlay window is actually shown.
