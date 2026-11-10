# WSA-UX-029 slice 4 — native speech Settings

## Summary

Implement O01–O05 speech behavior with corrected percentage speed, real typing
strategies and native-timed highlighting. Retain WPF/OneCore/SAPI, Core and the
existing Windows data/pack/commit boundaries. Windows documentation stays local.

## User-visible behavior

Choose a per-language voice, 50–150% speed in 5% steps, None/word/sentence/paragraph
typing strategy and None/word/sentence highlight. Actual editor/qualified Notepad
boundaries trigger speech; pasted blocks do not replay. Continuing the next word
does not cancel an unchanged committed span, and new boundaries replace previous
speech without queue growth. Manual speech, focus/navigation, document changes
and Stop have scoped cancellation. Markers no longer move selection/caret or undo.

Candidate audio uses the selected voice/speed and the same stoppable service.
Older Windows system-tempo/speech-language controls remain in legacy, with saved
intent retained. Primary voice/tempo edits explicitly choose app control; system
mode is labelled honestly until then. Account, analysis and learning are later
slices, not enabled by this delivery.

## Implementation details

`MainWindow.Speech` observes actual text/target snapshots. Speech unit/trigger policy
is presentation logic only; no normalization/ranking/morphological rules move from
Core. The existing native TTS routes use a fixed embedded worker, versioned stdin
JSON and bounded numeric timing output. The service owns each process/scratch path
and opaque event identity; cancellation kills/reaps before cleanup, with dispatcher
identity/document/generation checks preventing stale events. Native metadata and
SSML-to-original range mapping feed a non-selecting WPF adorner.

Schema 3 replaces the old exponential request with linear percentage semantics,
backing up the original V1/V2 bytes once. Page/all values migrate to paragraph.
False/unknown consent, voices and unknown fields remain. SAPI's former coarse
integer rate was not the same linear scale; acoustic equivalence is not claimed.
See [speech contract/recovery](../Speech.md) and [qualification](../qualification/WSA-UX-029_slice4.md).

The old `OverlaySpeechService` is removed after preserving its candidate button
through the shared route: its unmanaged processes had no real cancellation and
ignored selected voice/tempo. Assembly-qualified icon resource addresses allow
the actual app windows to run in the integration test host; icon assets are unchanged.

## Files changed

Repository-root-relative paths; shared files are staged only for this slice.
Earlier editor tabs, developer restart and release changes stay outside the commit.

- `/src/WordSuggestorWindows.App/MainWindow.Speech.cs`
- `/src/WordSuggestorWindows.App/MainWindow.xaml.cs` (speech hooks only)
- `/src/WordSuggestorWindows.App/MainWindow.xaml` (resource addresses only)
- `/src/WordSuggestorWindows.App/InternalEditorWindow.xaml` (resource addresses only)
- `/src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/LegacySettingsWindow.xaml`
- `/src/WordSuggestorWindows.App/Controls/SpeechRangeAdorner.cs`
- `/src/WordSuggestorWindows.App/Models/SettingsSchema.cs`
- `/src/WordSuggestorWindows.App/Services/SpeechSpeedPolicy.cs`
- `/src/WordSuggestorWindows.App/Services/SpeechTypingTracker.cs`
- `/src/WordSuggestorWindows.App/Services/TypingReadAloudPolicy.cs`
- `/src/WordSuggestorWindows.App/Services/SettingsFormController.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsAppSettingsService.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsTextToSpeechService.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsVoiceCatalogService.cs`
- `/src/WordSuggestorWindows.App/Services/OverlaySpeechService.cs` (retired)
- `/src/WordSuggestorWindows.App/Speech/WindowsSpeechWorker.ps1`
- `/src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs`
- `/src/WordSuggestorWindows.App/Localization/Settings.da.json`
- `/src/WordSuggestorWindows.App/Localization/Settings.en.json`
- `/src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj`
- `/tests/WordSuggestorWindows.Smoke/SpeechSettingsSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SpeechUiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsStateSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsUiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs` (speech gate/child dispatch only)
- `/scripts/test_settings_app.ps1`
- `/README.md`
- `/docs/README.md`
- `/docs/Architecture.md`
- `/docs/Plan.md`
- `/docs/ParityMatrix.md`
- `/docs/UiParityPlan.md`
- `/docs/ManualSmoke.md`
- `/docs/Speech.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/qualification/WSA-UX-029_slice4.md`
- `/docs/changes/2026-11-10_wsa_ux_029_slice4_speech.md`

## Feature flags / settings impact

Local schema 3 at the historical path; V1/V2 migration and original-version backups.
No new sharing/learning consent, account storage, backend feature, production
identity, feed or pack promotion. Strategy remains None by default. Existing
global-capture permission governs verified external automatic reading.

## Logging / telemetry impact

No raw text/keys/audio/credentials or worker stderr in diagnostics. User text moves
over stdin, not a command line or JSON temp file. Owned audio scratch content is
deleted on normal completion/Stop; hard-crash leftovers are explicitly documented.
Native synthetic fixtures/measurements/screenshots remain in ignored `.artifacts`.

## Validation performed

[Slice 4 report](../qualification/WSA-UX-029_slice4.md) records exact-source gates,
102 speech adapter/native checks, 24 physical editor/Notepad checks, PCM measurements,
and the manual/hardware matrix. The clean export passes build with zero warnings/
errors, existing app 67, Settings state/UI 63/58, Core adapter 58, Core fixtures/
portable contract 6/169, app/Notepad GUI 30, media 3 and qualification safety 35.
Full Core tests are not rerun/claimed green; unchanged Core has the slice-3 Windows
compile blocker. The normal working-tree Debug build also passes with zero warnings/
errors and 92 existing app checks; its unrelated editor/release changes remain unstaged.
The extra native Unicode/entity fixture exposed SAPI CharacterCount/escaped-offset
semantics; projection now uses the decoded word length plus verified original
position, with unchanged regression expectations.

## Known limitations / follow-up

Utterances are bounded to 16,000 UTF-16 units/20 minutes. Speech-language auto-detect
still follows writing language. Word/sentence span details can differ from Apple
segmentation; full Mac execution is not claimed. Notepad is the qualified external
typing route. Human listening/accessibility/DPI, Windows 10, restricted policy and
signed-update qualification remain open. Native voices can round requested rates.

Next slice 5 connects suggestion sources, domain lists, details, responsiveness,
fade and shortcut lanes to real effects and adds cancellable hover previews.
Analysis/local learning, accounts and updater authority remain later work.

## Consolidation notes for `WordSuggestorDocs`

A later docs-only pass may link Windows speech/schema-3 qualification and recovery.
Keep native runtime evidence separate from Mac reference and shared contracts.
Do not infer full UI/platform/release parity or positive Hello/update approval.
