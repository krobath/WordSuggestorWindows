# WSA-UX-013 editor tabs qualification — 2026-11-12

Source: staged tabs deliverable after developer commit f9f62a4; Core
56aba63042796b2a6c398828c3737ab0cb02eae5. Export uses Git archives with no source
output/cache copied. SDK 10.0.403 and installed Windows/Swift toolchains are
prerequisites. Initial tree 40ee75ceb807902e665a7a43018ca1d834d0e555 was then amended
only with the qualified analysis test synchronization and documentation. Every
tracked non-document source file in the tested export was compared to final staged
source. SHA-256 of sorted path/NUL/content-SHA256 lines: `54fc69fa4b48e1e476c942601101937f407b43fb1c01fb082e4ccb601ecdf0d9`.
This fingerprint excludes documentation, so evidence-only amendments do not
invalidate the tested source. Isolated logs: `.artifacts/cq-tabs-77243dac/` (ignored).

## Automated commands and results

Commands run inside the source-only WordSuggestorWindows export:

| Command | Result |
|---|---|
| `scripts/build_app.ps1` | PASS, 0 warnings/0 errors |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS, 354 contract checks + six real ordered Danish fixtures |
| `scripts/test_app.ps1 -SkipBuild` | PASS, 94 checks including 25 tab/storage checks |
| `scripts/test_settings_app.ps1 -SkipBuild` | PASS all software/UI suites (HelloNative/UpdateNative excluded by existing All contract) |
| `scripts/test_gui.ps1 -SkipBuild` | PASS actual editor + owned Notepad; see measured summary below |
| `scripts/test_media.ps1 -SkipBuild` | PASS, actual TTS/OCR, synthetic STT wire; no human hearing/microphone claim |
| `scripts/test_settings_qualification.ps1` | PASS, 41 safety/mapping checks |
| `scripts/test_run_scripts.ps1` | PASS, 14 owned-fixture checks |

Measured summary lines:

```text
PASS Core CLI: 6 real Danish fixtures; no document echo.
PASS CORE SETTINGS 354 checks.
PASS 94 checks. Test data: .artifacts/tests/<isolated-run>
PASS ADVANCED SETTINGS 34 checks; desktop=True; unknown-word lookup/sharing unavailable; Hello deferred.
PASS UPDATE SETTINGS 37 checks; desktop=True.
PASS ACCOUNT SETTINGS 55 checks; desktop=True; no production account mutation.
PASS ANALYSIS SETTINGS 39 checks; desktop=True.
PASS SUGGESTION SETTINGS 48 checks.
PASS SETTINGS SPEECH 102 checks; real native PCM timing/playback; audible quality not human-verified.
PASS WINDOWS CORE SETTINGS 58 checks.
PASS SETTINGS STATE 63 checks.
PASS SETTINGS UI 58 checks; interactive WPF desktop, ordinary user.
PASS SETTINGS SPEECH UI 25 checks; physical Windows input, real editor/Notepad and native playback. Human audible/screen-reader sign-off remains open.
PASS GUI 54 checks. DPI=96; monitors=1. Data: .artifacts/tests/<isolated-run>
PASS MEDIA 3 adapter/contract checks. Full microphone/snipping/highlight tests remain manual.
PASS SETTINGS 41 contract/safety checks. Mock tests do not attest native OS or Hello access.
PASS run scripts: 14 checks; only a synthetic fixture process was closed.
```

## Desktop/manual smoke matrix

| Flow | Evidence |
|---|---|
| Mouse/keyboard suggestions, prefix/caret/Unicode | PASS actual editor/Notepad |
| Tab new/switch, independent undo, restored selection | PASS actual desktop |
| Dirty close cancel/discard and neighboring document | PASS actual desktop |
| Native Open/Save, hide/reopen, one-instance behavior | PASS actual desktop |
| Autosave, crash/relaunch, active Unicode draft | PASS; only owned fixture process terminated |
| Missing/future/corrupt workspace preservation | PASS automated store tests, including same-schema corruption immediately before Save |
| Text analysis context menu, correction, one undo, explicit reading choice | PASS physical input after synchronization |
| Toolbar focus with Notepad open / overlay fallback | PASS actual desktop |
| Physical display coverage | One monitor, DPI 96; mixed-DPI/multi-monitor NOT RUN |
| Human Narrator/high contrast/many tabs | NOT RUN |
| Real microphone or speech while switching tabs | NOT RUN |
| Signed N-to-N+1 with draft/session retention | NOT RUN |
| Windows Hello | USER-DEFERRED; no native PIN/biometric run |

## Initial failures and resolution

The first Settings run failed before the native analysis right-click menu opened;
subsequent attempts exposed a click before menu input was processed and a second
menu requested while settings-triggered analysis was being refreshed. A separate
instrumented attempt hit a transient synthetic choice-store File.Replace failure;
the identical attempt then passed. Temporary event traces were removed.
The test now waits for idle layout, the actual pointer-over/foreground target,
current analysis and open menu before pressing/correcting. Expected correction,
focus, undo and choice assertions were not weakened; no automatic retry/skip was
introduced. The full uninstrumented suite and all remaining gates then passed.
Earlier failed logs are retained, not used as success evidence. Human acceptance
and production/native package gates remain open; no signed beta is claimed.

## Ownership and next step

Only Windows source/docs change. macOS, portable Core, language pack format,
shared Docs and backend remain untouched. Next delivery makes the existing
Windows beta uploader/release scripts reviewable and locally tested; it does not
publish a beta or provision signing/trust/SSH. Users can already build/run tabs
with the README commands and retain normal local drafts/settings.
