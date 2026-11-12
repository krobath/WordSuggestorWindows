# WSA-UX-029 slice 11: explicit unknown-word import

## Summary

Enable a usable explicit Danish dictionary import with separate online consent,
Core-owned validation/merge, atomic local persistence and offline suggestion reuse.
Retain WPF and the existing three-layer architecture; A06 partial, A07 unavailable.

## User-visible behavior

Settings > Advanced and the internal editor menu open the native lookup dialog.
The user enters one word, approves a default-No explanation of third-party lookup
and explicitly chooses Look up and save. Known/ineligible/not-found/offline/error
states are calm. Opt-out cancels pending work; previously imported words remain
available offline until explicitly cleared. No automatic typing/clipboard capture,
document insertion, surrounding-text transmission or sharing is enabled.

## Implementation details

UnknownWordImportService holds a file lease over Core preparation and atomic
commit, rechecking persisted consent, UI/language scope and source fingerprint.
Core lookup and merge are read-only; Core owns all linguistic rules and snapshot
format. LearnedWordStore flushes a unique temp file then performs File.Replace or
File.Move after final cancellation/context checks. Main/future backup files are
protected; clearing removes only owned learned main/backup files. Manual UserWords,
pack and choice data remain separate. Native UI cancellation covers close,
deactivation, input/language change and revocation; one-second explicit throttle.

Schema 7 defaults consent off for new and migrated data; exact prior-version
backups, false/unknown settings and three-way legacy merge remain supported.
Bridge capability/scoped identity/closed payload checks and process reaping extend
the existing transport; diagnostics gain fixed operation enums only. DA/EN strings,
UIA names and live status are provided. Existing public release flow is unchanged.
See /docs/UnknownWordImport.md for protocol, file limits, consent and failure detail.

Core is pinned to 5cd7503097f3e12d336bf6829f00fab83917f11d. Its additive optional
transport hook preserves macOS initializer calls; shared COR-id/Unicode URL bugs
are fixed with tests. macOS source, engine/ranking, Package.swift, pack bytes and
backend are unchanged. Native macOS qualification is not claimed on Windows.

## Files changed

- `/README.md`
- `/dependencies.lock.json`
- `/docs/Architecture.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/UiParityPlan.md`
- `/docs/UnknownWordImport.md`
- `/docs/changes/2026-11-13_wsa_ux_029_slice11_explicit_unknown_word_import.md`
- `/docs/qualification/WSA-UX-029_slice11.md`
- `/docs/qualification/settings-parity-status.v1.json`
- `/docs/sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/scripts/test_settings_qualification.ps1`
- `/src/WordSuggestorWindows.App/InternalEditorWindow.xaml`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Models/CoreUnknownWordResult.cs`
- `/src/WordSuggestorWindows.App/Models/OperationalDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/ICoreUnknownWordAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/LearnedWordStore.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Advanced.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/UnknownWordConsent.cs`
- `/src/WordSuggestorWindows.App/Services/UnknownWordImportService.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliUnknownWords.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/UnknownWordLookupWindow.cs`
- `/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.SuggestionSettings.cs`
- `/tests/WordSuggestorWindows.Smoke/AdvancedSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/CommittedWordAssessmentSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/UnknownWordSmoke.cs`

## Feature flags / settings impact

IsUnknownWordLookupEnabled defaults false, UnknownWordLookupConsentVersion requires
1. A06 is capability-gated and explicitly import-only. A07 sharing stays disabled.
Learned words continue to contribute offline after opt-out. Automatic completed-
word observation is not connected; Hello remains user-deferred and release gates
remain open. Existing choice-sharing preferences do not authorize this path.

## Logging / telemetry impact

Only closed operation/category/counter metadata. Words/proposals/snapshots are
storage/IPC data, never logs, exception details or telemetry. No credentials or
new backend calls. Dictionary requests necessarily contain the explicitly chosen
word and use bounded HTTPS transport; the separate default-No explanation says so.

## Validation performed

Build with zero warnings/errors; Core six real Danish fixtures plus 613 executable
checks; live synthetic ASCII/Unicode lookups resolved, stderr zero. Final import suites PASS 38 software / 47 desktop checks.
All nine isolated source-export gates pass, including the complete 13-suite
Settings run and 54-check editor/Notepad suite. Exact source evidence is recorded in /docs/qualification/WSA-UX-029_slice11.md.

## Known limitations / follow-up

Explicit Danish import only, no passive capture or sharing. The learned overlay
currently feeds suggestions/assessment; wordInfo/document readings remain pack-backed. No new installed
package/signing/update qualification or public release. Human Narrator, audible
speech and mixed DPI remain open; native Hello explicitly deferred by the user.
Full Swift test target compilation is blocked on Windows by existing unrelated
useconds_t/usleep and CFAbsoluteTimeGetCurrent errors; executable CLI tests are
separate evidence. macOS native testing is external.

Next allow learning while writing by connecting verified completed-word events to
this same consent-controlled service. Focus, document-generation and rate limits
must be qualified before enabling passive observation. Sharing remains unavailable.

## Consolidation notes for `WordSuggestorDocs`

Record A06 partial (usable explicit import), A07 unavailable, schema 7 and pinned
shared provider fixes. Platform implementation docs remain local to Windows/Core;
macOS/backend/shared docs are unchanged in this delivery.
