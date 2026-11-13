# WSA-UX-029 slice 12: verified automatic unknown-word lookup

## Summary

Add consent-controlled automatic dictionary enrichment during real editor/Notepad
typing using the existing Core import service. Retain WPF and all existing features.

## User-visible behavior

Advanced exposes an additional automatic-lookup permission, default off with a
separate default-No explanation. Existing manual consent never opts a user into
typing observation. Completed unknown Danish words can enrich local suggestions;
no document insertion or sharing is performed. Revocation cancels pending work.

## Implementation details

AutomaticWordLearning consumes Core-owned original UTF-16 ranges, delegates the
existing lookup/merge transaction and checks target/prefix/language/consent through
completion. Native editor key/composition receipts must match real document edits;
Notepad requires verified same-target single insertion snapshots. Actual foreground
is rechecked, committed prefixes may remain valid during later typing, and stale
work is cancelled. One active request, no automatic retry, 3-second / ten-per-minute
budget and bounded twenty-minute exact-word deduplication. C# performs no linguistic
tokenization/normalization/ranking. Core's additive completedWord operation is pinned
to ebd2424384649128a967867cf1ed3189de937d8c. Shared Core library, macOS app, pack,
Package.swift, backend and shared Docs are unchanged. See /docs/AutomaticUnknownWords.md.
The four approved closing slices are fixed in the local closing plan.

## Files changed

- `/README.md`
- `/dependencies.lock.json`
- `/docs/Architecture.md`
- `/docs/AutomaticUnknownWords.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-13_wsa_ux_029_slice12_automatic_unknown_words.md`
- `/docs/qualification/WSA-UX-029_slice12.md`
- `/docs/qualification/settings-parity-status.v1.json`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_settings_app.ps1`
- `/scripts/test_settings_qualification.ps1`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/MainWindow.SuggestionAssist.cs`
- `/src/WordSuggestorWindows.App/MainWindow.UnknownWords.cs`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `/src/WordSuggestorWindows.App/Models/CoreSettingsContract.cs`
- `/src/WordSuggestorWindows.App/Models/OperationalDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/AutomaticWordLearning.cs`
- `/src/WordSuggestorWindows.App/Services/ICoreCompletedWordAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.Advanced.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsDiagnostics.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSettingsAdapter.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `/src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliUnknownWords.cs`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml.cs`
- `/tests/WordSuggestorWindows.Smoke/AutomaticWordSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/UnknownWordSmoke.cs`

## Feature flags / settings impact

Schema 8 adds IsAutomaticUnknownWordLookupEnabled and
UnknownWordAutomaticConsentVersion:1, default off. Older manual permission remains
separate. Master lookup revocation disables automatic capture. A06 implemented;
A07 unavailable. Existing false/unknown settings and migration backups survive.

## Logging / telemetry impact

No document/token logs, raw keyboard capture or new clipboard path. Only bounded
local context goes to Core; dictionary receives one consented word. Existing fixed
operational categories now recognize completedWord. No backend or upload.

## Validation performed

Core build, six DA fixtures / 653 executable checks, full Settings All (14 suites),
final 24-check automatic suite including real physical editor/Notepad boundaries,
98 app checks, 54 native GUI checks, 41 control/safety checks and 58 unchanged old
client checks PASS. Final build 0 warnings/errors. See full qualification record
for exact commands, earlier Space receipt failure and final targeted reruns.

## Known limitations / follow-up

External automatic learning is qualified only on the existing native Notepad route;
other programs fail closed. Exact-case duplicates can consume additional bounded
attempts; normalization stays Core-owned. Human Narrator, mixed DPI and installer
acceptance remain open; Hello is user-deferred. macOS native testing is not claimed.

Next implement the Windows sharing client and hand off the compatible receiver to
the macOS backend agent as the user requested. A07 must remain unavailable until
that deployed contract and consent are qualified; no live upload is authorized here.

## Consolidation notes for `WordSuggestorDocs`

Record schema 8, A06 runtime and the approved four closing slices; preserve A07,
Hello and release acceptance boundaries. Implementation docs remain Windows/Core local.
