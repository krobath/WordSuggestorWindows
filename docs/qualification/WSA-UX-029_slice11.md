# WSA-UX-029 slice 11 qualification - 2026-11-13

## Scope and exact source

Implemented: explicit Danish dictionary import after separate consent, Core-owned
selection/merge, separate atomic learned overlay and offline suggestion reuse.
A06 is partial; A07 unavailable. Passive typing capture and sharing are disabled.
See [contract](../UnknownWordImport.md), [slice plan](../sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md)
and [change note/file list](../changes/2026-11-13_wsa_ux_029_slice11_explicit_unknown_word_import.md).

Windows base d39afaa, initial staged-tree archive `ee8d1423b9b7d2e58597159c37ee0a60e919f75b`;
Core `5cd7503097f3e12d336bf6829f00fab83917f11d`. Both exported with Git archive;
no prior outputs/caches copied. Installed toolchains and fresh dependency restore
remain prerequisites. Tracked non-Markdown files in final working tree, staged
index and tested export were compared byte-for-byte after Git newline normalization.
Sorted path/NUL/content-SHA256 fingerprint: `e5c97184067117a708b551b26d0ed8fd72bee804f18d9bf913ebf78bd63f37c9`.
Ignored evidence/export: `.artifacts/s11-8b788242c51a/`.

One final test-only amendment isolates WORDSUGGESTOR_DATA_ROOT/credential scope and
seeds an empty owned manual overlay so no global legacy data can be consulted.
The export was rebuilt with zero warnings/errors and both import suites repeated
(38 software and 47 desktop checks). No product code or JSON changed after the
initial export. Final Markdown amendments record these results.

## Commands and concrete results

Run from WordSuggestorWindows with `powershell -ExecutionPolicy Bypass -File`:

| Script / arguments | Actual result |
|---|---|
| scripts/build_app.ps1 | PASS fresh source export, 0 warnings / 0 errors |
| scripts/test_core_cli.ps1 -SettingsContract | PASS fresh Core build, six real DA fixtures + 613 executable checks |
| scripts/test_app.ps1 -SkipBuild | PASS 98 checks |
| scripts/test_settings_app.ps1 -Mode All -SkipBuild | PASS all 13 suites below |
| scripts/test_gui.ps1 -SkipBuild | PASS 54 checks, real editor/Notepad, DPI 96, one monitor |
| scripts/test_media.ps1 -SkipBuild | PASS 3 adapter/contract checks; not human microphone acceptance |
| scripts/test_settings_qualification.ps1 | PASS 41 contract/safety checks |
| scripts/test_run_scripts.ps1 | PASS 14 checks; only owned synthetic process closed |
| scripts/test_release_scripts.ps1 | PASS 48 synthetic release-tooling checks, no signing/install/upload |
| scripts/test_settings_app.ps1 -Mode UnknownWords -SkipBuild | PASS final amended fixture, 38 checks |
| scripts/test_settings_app.ps1 -Mode UnknownWordsUi -SkipBuild | PASS final amended fixture, 47 checks |
| Earlier unchanged Windows Core adapter against new CLI | PASS 58 checks |

All-mode output:

```text
PASS ADVANCED SETTINGS 34 checks; desktop=True; explicit unknown-word import available; sharing unavailable; Hello deferred.
PASS UPDATE SETTINGS 37 checks; desktop=True.
PASS ACCOUNT SETTINGS 55 checks; desktop=True; no production account mutation.
PASS ANALYSIS SETTINGS 39 checks; desktop=True.
PASS SUGGESTION SETTINGS 48 checks.
PASS SETTINGS SPEECH 102 checks; real native PCM timing/playback; audible quality not human-verified.
PASS WINDOWS CORE SETTINGS 58 checks.
PASS COMMITTED WORD 79 checks; read-only assessment, no implicit lookup/sharing.
PASS SETTINGS STATE 63 checks.
PASS SETTINGS UI 58 checks; interactive WPF desktop, ordinary user.
PASS SETTINGS SPEECH UI 25 checks; physical Windows input, real editor/Notepad and native playback. Human audible/screen-reader sign-off remains open.
PASS UNKNOWN WORD 38 checks; desktop=False; explicit only; no sharing or passive capture.
PASS UNKNOWN WORD 47 checks; desktop=True; explicit only; no sharing or passive capture.
```

Actual public COR probes used synthetic packs and only the test words skrive and
laese (with Danish ae character). Both ASCII and Unicode resolved from
ordregister:cor, exit 0, stderr zero bytes. No learned files were written by these
read-only probes. Deterministic provider and raw COR JSON fixtures independently
verify the shared selector, real field names and exact Unicode request URL.

`swift test --filter UnknownWordLearningTests` was attempted with the pinned
Windows toolchain; unrelated existing useconds_t/usleep and
CFAbsoluteTimeGetCurrent compilation errors prevent XCTest execution. The added
XCTest is not claimed to have run. Native macOS build/runtime NOT RUN on Windows.

## Desktop and manual matrix

| Flow | Evidence / remaining qualification |
|---|---|
| A06 off by default, consent declined/accepted/revoked | PASS native WPF/UIA, injected consent decision; human wording approval not run |
| Explicit dialog lookup -> actual Core -> atomic store | PASS real WPF/UIA with deterministic dictionary provider |
| Close pending lookup, no late learned file | PASS actual dialog and cancelled process |
| Live production dictionary transport, ASCII/Unicode | PASS read-only synthetic-pack probes |
| Learned suggestions after restart and opt-out | PASS actual Core + persisted overlay |
| Stale scope, revocation between phases, preimage change | PASS controlled service boundary; no late writes |
| Corrupt/future main or backup, concurrent file lease | PASS local storage protection tests |
| Clear imported data | PASS store adapter; native confirmation/clear action human check remains open |
| Manual user words/settings/pack preserved | PASS separate stores, migration backup and Core pack hash |
| Editor caret, Unicode, keyboard/mouse commit, tabs/recovery | PASS native 54-check desktop suite |
| External Notepad, toolbar focus and overlay fallback | PASS owned native tests; Notepad version: 11.2607.14.0 |
| Speech/highlight/strategy | PASS existing native suites; no human audible sign-off |
| Screen-reader names/keyboard accessibility | UIA names and existing keyboard gates PASS; human Narrator NOT RUN |
| DPI / multiple monitors | 96 DPI / one monitor tested; mixed DPI and multiple monitors NOT RUN |
| Positive Windows Hello PIN/biometrics | USER-DEFERRED, not executed |
| Trusted installed update / N-to-N+1 / rollback | NOT RUN, existing release gate remains open |
| macOS native build/runtime | NOT RUN; canonical app source remains unchanged |
| Production signing, SSH/upload and public beta | NOT RUN, no deployment/publication |

## Early development failures

An initial native import check observed the atomic file before the asynchronous
caller had updated its final status. The assertion was retained; the test now
waits for both persisted file and completed UI status and verifies dialog activation.
A Unicode fixture/localized strings assembled through legacy PowerShell text
encoding were corrected to UTF-8; the token validation had correctly rejected the
invalid fixture. Final source-export and repeated tests pass without weakened
expectations or hidden retries. Earlier failed local logs remain under .artifacts.

## Compatibility and next product step

Only Windows and Core changed. macOS reference remains ba18b451372dd2e3f125e557c5db45894f4e1ae3;
backend and shared Docs are untouched. Existing macOS COR provider calls still use
the same initializer defaults. Shared fixes add the real COR-id alias and correct
Unicode URL escaping; engine/ranking, Package.swift and pack bytes are unchanged.
This is source/API review and Windows executable evidence, not macOS runtime sign-off.

Next let the user learn words during writing by connecting verified completed-word
events to this same consent-controlled service. Add focus/document-generation and
rate-limit qualification before enabling passive capture. Sharing stays disabled;
Hello and official beta install/update gates remain separately open.
