# WSA-UX-029 slice 10 qualification — 2026-11-12

## Scope and exact source

Implemented: read-only completed-token assessment and typed Windows adapter.
Not implemented here: automatic capture, unknown-word provider/store, online lookup
or sharing. A06/A07 remain disabled/off, not new fake effective switches.
See [contract](../CommittedWordAssessment.md), [slice plan](../sprints/WSA-UX-029_slice10_committed_word_assessment.md)
and [change note/file list](../changes/2026-11-12_wsa_ux_029_slice10_committed_word_assessment.md).

Windows base 9e016db, initial staged-tree archive `6550582ffe82a435e692db38d9e90cd4144a4e8c`;
Core `4c3cab1a4717699186461dc34578ff48bb7839b2`. Both were exported with Git archive;
no source outputs/caches copied, fresh dependency restore and installed toolchains
remain prerequisites. Current tracked non-Markdown inputs, including the lock and
control JSON, in both working tree and tested export were compared to final staged
blobs. Sorted path/NUL/content-SHA256 fingerprint: `b071371641309315b0b20990d9b9fff60a50125a0b6a6201a17b3a296b094c19`.
Documentation amendments after the freeze include corrected Plan.md text encoding.
One test-only amendment seeds an empty owned v2 user-word file before the mutable
baseline so it cannot consult global legacy data. That final fixture was rebuilt
with zero warnings/errors and its full 79 checks repeated after the final All run.
No product code, GUI test source or JSON control matrix changed after the export.
Ignored logs/source: `.artifacts/s10-9eee13100e17/`.

## Commands and concrete results

Commands below run from the isolated WordSuggestorWindows source export unless
noted. SDK 10.0.403, Swift 6.3 and pinned Windows SDK/DA dependencies retained.

| Command | Result |
|---|---|
| `scripts/build_app.ps1` | PASS, 0 warnings/0 errors |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS 505 checks + six ordered real Danish batch/JSON fixtures; rebuilt Core from committed archive |
| `scripts/test_app.ps1 -SkipBuild` | PASS 98 existing app checks |
| `scripts/test_settings_app.ps1 -Mode All -SkipBuild` | Final PASS all software/native-UI suites listed below; explicit HelloNative/UpdateNative excluded by existing contract |
| `scripts/test_settings_app.ps1 -Mode CommittedWord` | PASS 79 in All, then PASS 79 again on the final isolated fixture amendment |
| `scripts/test_settings_app.ps1 -Mode SpeechUi -SkipBuild` | PASS 25 separately after desktop readiness |
| `scripts/test_settings_app.ps1 -Mode AnalysisUi -SkipBuild` | PASS 39 separately after an intermittent earlier menu timeout |
| `scripts/test_gui.ps1 -SkipBuild` | PASS GUI 54 checks. DPI=96; monitors=1.; Notepad version: 11.2607.14.0 |
| `scripts/test_media.ps1 -SkipBuild` | PASS 3 actual TTS/OCR and synthetic STT adapter checks |
| `scripts/test_settings_qualification.ps1` | PASS 41 safety/mapping checks; all 72 control states preserved, new Core pin matched |
| `scripts/test_run_scripts.ps1` | PASS 14 owned-fixture checks |
| `scripts/test_release_scripts.ps1` | PASS 48 local fixture/mocked transport checks + immutable backend validator |
| `scripts/bootstrap_core_cli.ps1 -Configuration release`, then `scripts/test_core_cli.ps1 -SkipBootstrap -SettingsContract` in normal repo | Optimized CLI PASS 505 checks + six parity fixtures against the same committed Core source |
| Prior compiled Windows adapter (9e016db source) against new CLI | PASS 58 unchanged contract checks; additive capability/operation ignored safely by old client |

Final All measured summaries:

```text
PASS ADVANCED SETTINGS 34 checks; desktop=True; unknown-word lookup/sharing unavailable; Hello deferred.
PASS UPDATE SETTINGS 37 checks; desktop=True.
PASS ACCOUNT SETTINGS 55 checks; desktop=True; no production account mutation.
PASS ANALYSIS SETTINGS 39 checks; desktop=True.
PASS SUGGESTION SETTINGS 48 checks.
PASS SETTINGS SPEECH 102 checks; real native PCM timing/playback; audible quality not human-verified.
PASS WINDOWS CORE SETTINGS 58 checks.
PASS COMMITTED WORD 79 checks; read-only assessment, lookup/sharing unavailable.
PASS SETTINGS STATE 63 checks.
PASS SETTINGS UI 58 checks; interactive WPF desktop, ordinary user.
PASS SETTINGS SPEECH UI 25 checks; physical Windows input, real editor/Notepad and native playback. Human audible/screen-reader sign-off remains open.
```

New assessment tests cover read-only known/unknown/short/Core-numeric and Unicode
semantics, two-field response, request/document/generation/language identity,
repeated idempotent calls, missing/explicit/future/legacy/corrupt/oversized user
words, no directory/store creation, unchanged ordered results/pack hash and safe
operational metadata. Adversarial Windows children cover absent capability or
operation (no token sent), malformed/extra/duplicate/missing result fields,
wrong schema/disposition/envelope identity, process exit/stderr, timeout,
in-flight and queued cancellation and actual child reaping.

## Desktop and manual matrix

| Flow | Status |
|---|---|
| Settings categories, live persistence and legacy merge | PASS actual WPF/UIA/keyboard/mouse |
| A06/A07 remain unavailable/off | PASS Advanced UI tests |
| Editor suggestions, Unicode/caret, mouse/keyboard same commit | PASS actual desktop |
| Editor tabs, native Open/Save, recovery and Notepad isolation | PASS 54-check native desktop suite |
| Toolbar focus, stale target rejection, overlay fallback | PASS owned Notepad/editor flows |
| Text analysis corrections, one undo and explicit reading choice | PASS 39 native checks |
| Physical speech/highlight/strategy and external Notepad typing | PASS 25 native checks; no human audible sign-off |
| OCR/TTS/STT | Actual OCR/TTS + synthetic STT wire PASS; no microphone acceptance |
| Display hardware | 96 DPI, one monitor; mixed DPI/multi-monitor NOT RUN |
| Human Narrator/high contrast | NOT RUN |
| Positive Windows Hello PIN/biometric | USER-DEFERRED; not executed |
| Trusted installed update policy/N-to-N+1/rollback | NOT RUN; existing gates remain open |
| macOS native build/runtime | NOT RUN on Windows |
| Production signing, SSH/upload, public beta | NOT RUN; unchanged external gates |

## Initial failures and resolution

The first full All run passed through the new 79 checks but failed SpeechUi's
initial keyboard ownership assertion. An isolated rerun and the prior unmodified
compiled version failed the same assertion while foreground belonged to SearchHost;
the first Notepad attempt also lacked a verified focused text field. The user then
made the unlocked desktop available. A subsequent full run hit an intermittent
analysis reading-menu timeout; the same frozen test subsequently passed isolated.
SpeechUi, AnalysisUi, GUI, Media and remaining software gates all passed, then a
final complete All invocation passed. Original failed logs remain; no expectation
was weakened, GUI test code changed, hidden retry added or failure relabeled PASS.

The original new fixture assumptions about variants/NFC/NFD and numeric observer
signals were corrected against the existing independent Core paths, not by
altering language rules. A test's preexisting suggestion call created a directory
before its read-only assertion; that writing baseline was moved after the check
and seeded with an owned empty v2 overlay to avoid inherited global legacy data.
Core fixture SQLite connections explicitly close to avoid masking an assertion
with Windows temporary-file cleanup errors. These are recorded in each local note.

## macOS, ownership and remaining product work

Core changes only seven CLI/test/doc files. Compare 56aba63..4c3cab1 for Sources,
Package.swift, Tests, Ressources and LanguagePacks: no changes. macOS ba18b45 is
clean and its Xcode project uses product WordSuggestorCore via the local package,
not WordSuggestorSuggestCLI. Public engine API/default behavior, pack format and
data are unchanged; existing ordered and old-client tests pass. This is source/API/
transport compatibility evidence, not a macOS build or blanket runtime guarantee.
Backend, shared Docs, Tools and extensions remain untouched/clean.

Windows owns the adapter/UI/status/evidence; Core owns the portable executable
contract and its local change note. Changes are committed once per changed repo;
no push or production action. When a push is separately authorized, push the Core
commit before the Windows dependency pin so remote clean builds can resolve it.

Next supply a reviewed Core-owned provider/store and explicit lookup consent,
with verified physical completion and stale/focus cancellation. Users could then
look up and learn unknown words locally; until those pieces work, A06 remains
unavailable. A07 sharing needs separate consent/handoff; the new assessment cannot
authorize network, persistence, learning or ranking changes. Hello and release/
human hardware acceptance remain separate. The overall sprint/release stay open.
