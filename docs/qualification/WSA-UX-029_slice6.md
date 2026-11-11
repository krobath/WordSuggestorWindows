# WSA-UX-029 slice 6 — analysis, local word choices and insights

Date: 2026-11-11. Base Windows `18a1f6b`; Core
`defa19bdc6a3fcb990948caa1982631ced420dbf`; macOS semantic reference `ba18b45`.
Architecture: controlled extension of WPF/Core; no replacement application.
See [runtime, privacy, storage and recovery](../AnalysisAndLearning.md).

## Source audit and boundaries

The existing unrelated editor-tab, restart and release work was recorded before
editing and retained outside the slice commit. Shared docs/macOS/backend are
read-only references. The original slice-5 desktop block has changed: session 2
is now connected/active. Its Notepad hotkey test exposed use of current key state
after key-up. The slice-6 fix uses WM_HOTKEY modifier metadata, preserving exact
modifier/AltGr checks and the original external target snapshot verification.

All T01–T06, L01–L04 and E01–E02 have effective runtime paths. Missing Core/data
states are explicit. New local capture/sharing consent defaults off. User words,
document text, insight history and pack format are preserved. There is no upload,
training, automatic ranking or external diagnostic commit.

## Reproduce with isolated/synthetic data

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode AnalysisState -SkipBuild
# Connected unlocked desktop; do not interact during synthetic mouse/key input:
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_qualification.ps1
```

Final results below supersede intermediate working-tree counts. Native automation
is evidence for the tested source and flows, not full product release signoff.

## Native smoke matrix

| Flow | Working-tree evidence |
|---|---|
| Core POS/decomposed/emoji ranges; 512-word bound | Portable Core + Windows real-adapter fixtures PASS |
| Meaningful Settings/dependent states and comma picker Escape | Native WPF PASS |
| Right-click Core correction, verified focus return, one native undo | Physical mouse/keyboard PASS |
| Right-click interpretation, opt-in local record, detail/Back | Native mouse + Settings PASS |
| Local off/share dependency; no content/upload by default | Storage/runtime PASS |
| Delete one, future/null protection, reset preserves other stores | State PASS; native confirmation-dialog cancel/confirm not yet separately qualified |
| E02 opt-in stores exact Core example; E01 off stops writes | Real Core/state PASS |
| Stale/duplicate/overlap/surrogate correction rejection | Transaction/runtime PASS |
| Internal suggestions, three lanes, Notepad, speech regression | Full Settings native gate PASS on exact qualified source |
| Microphone, OCR snipping and human speech/highlight quality | Media adapter/contract gate only; full flows remain manual |
| Human Narrator/audible quality; mixed DPI/multiple monitors | Not run; no human/hardware signoff claimed |

The native test exposed context-menu source teardown/focus ordering. Custom
actions now run once from explicit gestures while verifying the owned menu and
originating editor; UIA fallback also requires the verified active editor. The
overlay is suppressed while the menu is open. Core requirement `required` is a
grammar obligation, not uncertainty; only `meaning_dependent` adds confirmation.
The obsolete extra prompt was fixed rather than bypassed by the test.

## Final isolated source qualification

All seven repository gates passed from fresh Git archives on 2026-11-11. Windows
candidate tree `12e184d65dadf8b588200122d547618346941683` and pinned committed Core `defa19bd` were exported
to new sibling directories; no working-tree sources, binaries or build caches
were copied. Core and Windows were built inside that export. Only documentation
is updated after qualification; the complete non-documentation Git-object-list
SHA-256 is `04961cbf2e84b030ca4922d5935bf4cf1ed940b68066d527d20b31e374c1d373` and is checked unchanged before commit.

| Gate / exact command | Result |
|---|---|
| `scripts/build_app.ps1` | PASS; C# build 0 warnings / 0 errors |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS; 6 real da_DK fixtures + 335 executable protocol checks |
| `scripts/test_app.ps1 -SkipBuild` | PASS; 67 checks |
| `scripts/test_settings_app.ps1 -SkipBuild` | PASS; Analysis 39, Suggestions 48, Speech 102, Core adapter 58, State 63, Settings UI 58, Speech UI 24 |
| `scripts/test_gui.ps1 -SkipBuild` | PASS; 31 checks; physical Notepad, mouse/keyboard insertion, focus, editor selection/caret and native file dialogs |
| `scripts/test_media.ps1 -SkipBuild` | PASS; 3 adapter/contract checks; full microphone/snipping/highlight flows remain manual |
| `scripts/test_settings_qualification.ps1` | PASS; 35 contract/safety checks; mocks do not attest OS/Hello access |

Native desktop: connected ordinary-user Windows, DPI 96, one monitor. Speech tab
UI is explicitly not run in this clean candidate because the pre-existing tab
implementation is outside this commit; document identity remains contract-tested.
No human audible, Narrator, high-contrast or additional monitor/DPI signoff follows
from these automated gates. Meaning-dependent and destructive confirmation-dialog
cancel/confirm remain the targeted manual follow-up in the matrix above.

Regression findings fixed without weakening expectations: WM_HOTKEY modifiers,
owned popup focus/source ordering, Core required-vs-meaning-dependent prompting,
deferred overlay width resolving to a 2-pixel native window, external settings
refresh, and select-all's final paragraph terminator exceeding the Core text
buffer. The GUI test now also asserts usable native overlay width. Only route,
permission, mode and foreground-match metadata is printed by new diagnostics.

The standard bundled da_DK pack remains unchanged, SHA-256
`6970C398077C235355F513D9491E4193534FE88B751AEEA07EDDEF9ABC3FB82F`.
Local artifacts (ignored): `.artifacts/slice6-index-3f1843e8105143c1a2fd768c80a12f9f/results.json` and the
corresponding gate logs. Generated packages, test data and logs are not committed.

## Remaining limits and next step

Analysis is internal-editor-only, capped at 16,000 UTF-16 units / 512 painted
words. No unknown-word spelling guesses, user-choice ranking or backend learning.
Full Swift-suite/macOS execution is not claimed; the prior unrelated Windows
CFAbsoluteTimeGetCurrent suite compilation issue remains documented in Core.
Hello, Profile/auth and OS update toggles remain later slices, not side effects
of analysis controls. Next: implement the actual Profile/account interactions and
secure Windows Hello session protection with existing backend contracts.
