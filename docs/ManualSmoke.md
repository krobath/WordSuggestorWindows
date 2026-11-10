# Windows smoke and qualification

Updated 2026-11-08. Automated desktop observation is distinct from a human manual
audio/accessibility/hardware session. No full manual sign-off is claimed.

## Commands

Run from the Windows repository, in this order:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1
powershell -ExecutionPolicy Bypass -File scripts\run_app.ps1 -SkipBuild
```

`test_core_cli` bootstraps Core and checks six batch/JSON parity fixtures.
`test_app` exercises real Core/pack data plus adversarial fake processes and state
races; no third-party test framework/package is needed. `test_gui` creates its own
synthetic Notepad document, launches the real app, and drives mouse/keyboard/UIA.
Close other WordSuggestor instances and keep the desktop unlocked. Do not interact
with mouse/keyboard while that test runs. Data/results stay in `.artifacts/tests`.

For packaging use `scripts/package_app.ps1`, then
`scripts/test_package.ps1 -AppDirectory <artifact-app-directory>`. This verifies a
copied payload without source/SDK discovery. Signed install/update qualification is
separate and described in `Release.md`. `test_clean_checkout.ps1` exports committed
Windows/Core source and builds/tests without copied repository build output.

## Evidence matrix

For the new Settings track, [WSA-UX-029 slice 0](qualification/WSA-UX-029_slice0.md)
records a fresh working-tree baseline: 35 qualification checks, 92 app checks,
six Core fixtures, 42 actual desktop checks, three media checks, nine launch-script
and 43 release-script checks, plus clean source-export app/Core build and tests.
Run `scripts\test_settings_slice0.ps1 -Desktop -CleanSource` to reproduce.
This does not qualify new Settings features: Hello returns DeviceNotPresent;
cmdlet/native update mutation on the isolated local MSIX returns 0x800706F7 with
unchanged settings. Positive Hello-secret, packaged-process update authority,
Windows 10, physical multi-monitor and human screen-reader testing remain open.
Test package, private test key and temporary leaf trust were removed and checked.

| Scenario | Evidence/status |
|---|---|
| App/core compilation | PASS; WPF build zero warnings/errors; clean optimized Core build PASS after SDK-map correction (existing Core Sendable warning remains) |
| Real DA suggestions | PASS for Danish prefixes/æ/ø/å; six ordered batch/JSON fixtures equal |
| Pack missing/corrupt/English-empty/wrong language/new schema/version | PASS automated validation cases; legacy DA explicitly distinguished |
| Core unavailable/malformed/wrong version/wrong request ID/exit/timeout/cancel | PASS; no raw process stderr in surfaced errors; child processes reaped |
| Out-of-order results/duplicate accepts | PASS; cancelled/old UI generations rejected |
| Settings/user words restart and future-schema retention | PASS; no destructive overwrite during rollback |
| Toolbar-only startup/no focus theft | PASS real desktop with Notepad 11.2607.14.0 |
| Normal editor over external program | PASS; foreground/editor text exposed through accessibility |
| Mouse and Ctrl+1 accepts | PASS internal and real Notepad; correct replacement/caret |
| Unicode and paragraph offsets | PASS actual RichTextBox with æ/ø/å, emoji and CRLF |
| User selection preservation/replacement | PASS actual RichTextBox selection scenario |
| Native file save/open/quit guards | Native Open and Save PASS through actual keyboard with Unicode round-trips; manual cancel/overwrite/quit paths still required |
| External clipboard retention/stale snapshot rejection | PASS Notepad; targeted commit never touches clipboard |
| Per-monitor placement math | PASS negative-origin/oversized-overlay cases; physical mixed-DPI hardware not tested |
| Screen-reader semantics | UIA names/TextPattern checked; human Narrator/NVDA walkthrough NOT RUN |
| Word/Outlook/browser/Google Docs commits | NOT SUPPORTED in beta until adapter qualification; cannot accidentally use generic injection |
| UIA/Word COM TXT/TTS import | Preserved; app-specific end-to-end qualification still required |
| TTS/STT/OCR | PASS actual OCR image recognition, OneCore Helle playback process and STT UTF-8/tab wire contract; microphone/human audio/snipping not run |
| Offline backend | Local path has no backend requirement; actual account/backend integration absent |
| Standalone copied payload | PASS 25 desktop checks outside workspace/SDK paths; does not prove signed installation |
| Clean committed checkout | PASS fresh app/SQLite/Core builds, six parity fixtures and 54 checks at Windows 54acfb2 / Core 5d8d7a0 |
| Signed install/update/uninstall/tamper/rollback | NOT RUN: signing identity/feed unavailable; no trust root installed or unsigned feed enabled |

## Human manual checks still required

1. Launch toolbar, open editor over Notepad/Edge/Word, verify focus and caret; close
   editor and continue in a supported external control. Test ten candidates/four
   pages, Ctrl+1…0, Tab, mouse, repeated clicks and Ctrl+Left/Right.
2. Type/select/replace Danish text, emoji, combining marks and several paragraphs;
   move caret into the middle of a document; test undo/redo, native Open/Save,
   cancellation, overwrite and quit with unsaved changes. Keep an external window open.
3. Exercise Core/pack unavailable conditions, missing language, empty candidates,
   and restart settings/user words. Confirm no full typed tokens/document text,
   credentials, window titles or raw process errors appear in diagnostics.
4. Drag static overlay, restart, unplug/rearrange displays; test 100/125/150/200%
   scaling, negative monitor origins, work-area/taskbar clamping and missing caret.
5. Use keyboard-only menus/editor/settings and Narrator/NVDA. Confirm action names,
   candidate access, focus restoration, high contrast and increased text size.
6. Test TTS internal/external selection, voice fallback, playback stop and highlight
   restore; dictation with microphone and installed DA recognizer; OCR visible text,
   cancellation, clipboard concurrency and callback under the installed package.
7. Execute the signed installation/update/rollback/uninstall sequence in `Release.md`.

## Troubleshooting

Run Core smoke first if suggestions are empty. Build/bootstrap now fail on nonzero
native exit; a stale CLI cannot make a failed build pass. Missing `_complex` or
mixed-SDK Winsock errors indicate SDK-module discovery: rerun the bootstrap with
its local overlay, not system-file patches. `pkg-config sqlite3.pc`, existing Core
Sendable and Swift symlink warnings are recorded separately from test success.

Use `%LOCALAPPDATA%\WordSuggestor\diagnostics` for stage/outcome/voice/count
diagnostics. Raw Core/TTS/OCR process errors are deliberately omitted. Test runs
use their own data root. Existing old logs may contain pre-hardening details and
are left intact; do not treat old log text as current behavior. Speech/OCR depend
on Windows languages/devices/privacy settings and remain separate manual gates.
