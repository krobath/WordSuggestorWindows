# WSA-UX-029 slice 5 — suggestion preferences and verified commit plans

## Summary

Implement all twenty suggestion Settings rows with native WPF controls, Core
source options/insertion plans, three suggestion lanes and editor presentation.
This is an implementation candidate; locked-desktop qualification remains open.

## User-visible behavior

Row density, insertion spacing, responsiveness, hover speech, word information,
English internal-editor period assist, fade, source switches, domain selection
and wrap now have runtime paths. Missing pack/capability states are explicit.
Independent digit schemes swap conflicts safely; mouse/keyboard/family selection
share the verified commit transaction. Existing functionality is retained.

## Implementation details

Retain WPF/.NET and the existing process bridge; pin tested Core `7491a0b`.
Schema 4 adds cloned, validated preferences, exact old-version backups and field
merging. Core owns linguistic decisions. Requests/results keep scope/generation;
external commits additionally retain and reverify the original target snapshot.
No clipboard use or arbitrary-application injection. See [contract](../Suggestions.md).

## Files changed

- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Suggestions.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Models/SuggestionPreferences.cs`
- `/src/WordSuggestorWindows.App/Models/SuggestionItem.cs`
- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Models/ExternalSuggestionCommitRequest.cs`
- `/src/WordSuggestorWindows.App/Services/ICoreSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/EditorCommit.cs`
- `/src/WordSuggestorWindows.App/Services/SmartCommitReceipt.cs`
- `/src/WordSuggestorWindows.App/Services/SuggestionSchedulingPolicy.cs`
- `/src/WordSuggestorWindows.App/Services/SuggestionShortcutPolicy.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsNativeTextAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsExternalSuggestionCommitService.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsTextToSpeechService.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.SuggestionSettings.cs`
- `/src/WordSuggestorWindows.App/ViewModels/SuggestionOverlayEntry.cs`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/MainWindow.SuggestionSettings.cs`
- `/src/WordSuggestorWindows.App/MainWindow.SuggestionAssist.cs`
- `/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/SuggestionOverlayWindow.Behavior.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/SuggestionSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsStateSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SpeechSettingsSmoke.cs`
- `/scripts/test_settings_app.ps1`
- `/dependencies.lock.json`
- `/README.md`
- `/docs/README.md`
- `/docs/Plan.md`
- `/docs/Architecture.md`
- `/docs/ParityMatrix.md`
- `/docs/UiParityPlan.md`
- `/docs/ManualSmoke.md`
- `/docs/BridgeContract.md`
- `/docs/Suggestions.md`
- `/docs/analysis/2026-11-09_settings_parity_analysis.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/qualification/WSA-UX-029_slice5.md`
- `/docs/changes/2026-11-11_wsa_ux_029_slice5_suggestion_settings.md`

## Feature flags / settings impact

Nested suggestions preferences in schema 4. Future/invalid data is write-protected;
no new sharing/learning consent. Later analysis/account/update slices remain gated.

## Logging / telemetry impact

No new content/credential logs, raw keys, uploads or telemetry. Smart receipts and
target text live only transiently in memory. Scheduling fixture logs contain
timings/counts, not document text.

## Validation performed

See [qualification and desktop matrix](../qualification/WSA-UX-029_slice5.md).
Core 301, adapter 58, state 63, speech 102 and suggestion-state 27 passed in the
working tree. Build had zero warnings/errors. Desktop integration is explicitly
incomplete because Windows session 2 is disconnected/locked.

The final staged-source export also passed: clean build 0 warnings/errors,
6 real DA fixtures, Core 301, app 67, Settings state 63, adapter 58, native speech
102, suggestion state 27 and qualification/safety 35. Source subtree identities
are frozen in the evidence document. The original out-of-order provider test
caught an extra-queue regression; the implementation was corrected, not the
expectation. Final index review covers 48 documented Windows text/source files,
local links, no added secret/key/token patterns, binary artifacts or machine paths.

## Known limitations / follow-up

Do not claim native keyboard/mouse/Notepad, hover/fade, family acceptance, Hello,
updater, extra DPI/monitors or Narrator qualification from these headless results.
Canonical Mac F10 is English/newline-only; Windows external period rewriting is
not enabled. Finish desktop gates before functional signoff or public release.

## Consolidation notes for `WordSuggestorDocs`

Later docs-only consolidation should link Windows Suggestions/evidence and the
Core bridge addition, including the F10 source correction. Keep live platform
implementation details here and retain macOS/backend ownership separately.
