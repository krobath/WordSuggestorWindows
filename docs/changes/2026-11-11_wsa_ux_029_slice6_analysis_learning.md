# WSA-UX-029 slice 6 — analysis, local choices and insights

## Summary

Implement real Core-backed internal-editor analysis and the analysis/learning/
error-tracking Settings controls. Retain WPF and the existing engine/pack boundary.

## User-visible behavior

Users can review actual word classes/readings and diagnostic corrections, with
native undo, explicit local choice storage/detail/deletion/reset and independent
insight-example opt-in. No automatic ranking, upload or external diagnostic edit.

## Implementation details

Pin Core defa19b. Schema 5 migrates comma semantics with exact original backups;
false/unknown consent and existing stores survive. The cancellable document
pipeline validates original text/language/generation. Adorners preserve text/
selection/undo; grouped edits require a verified editor. Owned-menu actions use
one commit path and cannot reactivate from another app. Local choices are bounded,
atomic and separate from user words/insights. Reset has explicit confirmation and
visible failure. Bound-safe rich-text offsets prevent Select-all input crashes;
finite native sizes and a minimum overlay width preserve physical hit targets.
External settings changes reschedule suggestions. WM_HOTKEY metadata fixes the previously unqualified Notepad route.

## Files changed

- `/README.md`
- `/dependencies.lock.json`
- `/docs/AnalysisAndLearning.md`
- `/docs/Architecture.md`
- `/docs/BridgeContract.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-11_wsa_ux_029_slice6_analysis_learning.md`
- `/docs/qualification/WSA-UX-029_slice5.md`
- `/docs/qualification/WSA-UX-029_slice6.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/src/WordSuggestorWindows.App/Controls/AnalysisRangeAdorner.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/MainWindow.Analysis.cs`
- `/src/WordSuggestorWindows.App/MainWindow.SuggestionSettings.cs`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Models/SavedWordChoice.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/DiagnosticEditPlanner.cs`
- `/src/WordSuggestorWindows.App/Services/ICoreSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/ScreenPlacement.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Analysis.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsText.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsErrorInsightsStore.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsWordChoiceStore.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.Analysis.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `/tests/WordSuggestorWindows.Smoke/AnalysisSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/GuiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/PackFixture.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsStateSmoke.cs`

## Feature flags / settings impact

Schema 5: comma style/context mode and separate SaveWordChoices/ShareWordChoices.
Local capture/sharing defaults off; local-off disables share. Core persistence and
adaptive learning stay off. Missing-data intentions are retained. T01–T06/L01–L04/
E01–E02 have real runtime paths; Profile/Hello/update remain later slices.

## Logging / telemetry impact

No raw keys, credentials, full-document diagnostic logs or uploads. Chosen words/
reading metadata are opt-in local evaluation content. Insight sentence fragments
require separate E02 opt-in and Core bounds. Technical route diagnostics are codes.

## Validation performed

See [qualification and native/manual matrix](../qualification/WSA-UX-029_slice6.md).
Fresh exact-source archives passed all seven repository gates: C# build 0 warnings/
errors; Core 335 plus 6 DA fixtures; app 67; Analysis 39, Suggestions 48, Speech 102,
adapter 58, State 63, Settings UI 58, Speech UI 24; GUI 31, media and qualification
safety gates. The clean candidate excludes pre-existing tabs/release/restart work;
its speech-tab UI check is explicitly not run. No binary/cache source was copied.
Source-object fingerprint and exact tree are in the qualification document. Only
documentation changes after that gate. Staged diff is checked for whitespace,
secrets, machine paths, binary artifacts, local links and unrelated changes.
Tests exposed and fixed popup ownership/focus, Core requirement prompting,
hotkey metadata, native overlay size and rich-text final-terminator offsets;
expectations were retained and native width coverage added.

## Known limitations / follow-up

Internal analysis limit 16,000 UTF-16 units / 512 painted words. No fabricated
spelling classes or backend learning. Meaning-dependent and destructive dialog
confirmation/cancel, human Narrator/audio, contrast and additional DPI/monitors
remain targeted manual gates. Full Swift/macOS execution is not newly qualified.
Next: build Profile/account actions and secure Windows Hello against existing
contracts; this slice starts no production login, update or publishing flow.

## Consolidation notes for `WordSuggestorDocs`

Later docs-only consolidation should link this Windows contract/evidence and the
Core document API, consent/store boundaries and slice-5 native follow-up. Keep
live platform implementation details local; macOS/backend/shared docs are unchanged.
