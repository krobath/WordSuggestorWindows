# WSA-UX-029 slice 9 — Advanced diagnostic candidate

## Summary

Implement five independent Advanced diagnostics controls and the two explicit
unavailable unknown-word states, keeping existing Windows legacy settings.
Retain WPF/Core/pack architecture; full parity/release gates remain open.

## User-visible behavior

Performance, App, caret/placement, panel state and Core can be enabled separately.
All default off; state persists after a successful settings write. Native DA/EN
names/help explain scope and privacy. Local file failures have a calm status;
typing continues. Unknown-word lookup/sharing do not perform work or grant consent.

## Implementation details

Typed metadata enums, bounded producer channel and background writer separate
runtime diagnostics from user content. Aggregate timers record counts/total/max;
three fixed 64-KiB files and lazy seven-day retention bound disk usage. Core
`56aba63042796b2a6c398828c3737ab0cb02eae5` adds capability-negotiated closed operational
summaries with raw logger off. Schema 6 adds panel preference and exact v5 backup;
three-way legacy merge/unknown fields remain intact. No linguistic C# rules,
pack-format change, new backend protocol or upload are introduced.

## Files changed

- `/README.md`
- `/dependencies.lock.json`
- `/docs/Architecture.md`
- `/docs/Diagnostics.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-12_wsa_ux_029_slice9_advanced_diagnostics.md`
- `/docs/qualification/WSA-UX-029_slice9.md`
- `/docs/qualification/settings-parity-status.v1.json`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/scripts/test_settings_qualification.ps1`
- `/src/WordSuggestorWindows.App/LegacySettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/MainWindow.Diagnostics.cs`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Models/OperationalDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Advanced.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `/tests/WordSuggestorWindows.Smoke/AdvancedSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/AnalysisSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/GuiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsStateSmoke.cs`

## Feature flags / settings impact

Existing four diagnostic flags become effective; IsPanelParityLoggingEnabled is
new and defaults false. Old explicit choices and false/unknown consent remain.
Unknown-word lookup/sharing have no fake persisted effective bool. Hello remains
user-deferred; production update-policy write qualification remains gated.

## Logging / telemetry impact

Runtime schema accepts only fixed event/category/metric/operation codes, timestamp,
bounded time/count fields. No text, candidates, identities, screen geometry, paths,
raw stderr or credentials. No automatic upload. Historical operational breadcrumbs
are separate and preserved. Disabling invalidates queued records; an already
started metadata append can finish. Existing files are not erased by a toggle.

## Validation performed

Fresh staged-source Windows/Core exports pass all seven software gates: build
0 warnings/errors; Core 354 + 6 DA fixtures; app 67; Advanced 34, Update 37,
Account 55, Analysis 39, Suggestions 48, Speech 102, adapter 58, State 63, UI 58,
Speech UI 24; GUI 37 at DPI 96/one monitor; Media 3; safety/mapping 41. Native
mouse/Space/UIA, real editor/owned Notepad and bounded metadata without App logging
are verified. Earlier disconnected desktop/black-OCR restrictions were rerun
successfully for this source, without weakening expectations. Native updater
remains BLOCKED/exit 3 prerequisite; HelloNative stays user-deferred/unexecuted.
See [full source fingerprints and manual matrix](../qualification/WSA-UX-029_slice9.md).
Only prose changed after the run. Diff review preserves 226 non-slice tracked
baseline files, all 18 original untracked hashes and mixed-file hunks; 36 own
Windows files, six Core files, no generated binaries/secrets/machine paths.

## Known limitations / follow-up

All 72 current statuses are mapped, not blanket-qualified. A06/A07 still need a
portable Core committed-word/provider/handoff contract and privacy validation;
manual user words are not that feature. G05 positive installed mutation, P07
user-deferred Hello, live account and human Narrator/contrast/DPI/multi-monitor,
trusted install/update/relaunch/data retention/signature tests remain open.
No official beta, production sign/feed/SSH operation or automatic learning is
authorized. Next work closes real Windows acceptance and the missing Core lane.

## Consolidation notes for `WordSuggestorDocs`

Later link the Windows diagnostic/storage/status matrix and Core-owned safe wire
contract. Keep unknown-word, Hello and installed-update gaps explicit. Shared docs,
macOS and backend remain unchanged; all live implementation docs stay repo-local.
