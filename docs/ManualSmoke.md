# Windows smoke and qualification

## WSA-UX-029 slice 15 - final software and acceptance boundary (2026-11-13)

Settings row editors now expose localized UIA names, including after language
changes. Layout automation covers two account states, two locales and all eight
categories. The existing clean-source script supports FullRegression and records
software gates separately from native/human release acceptance. See the
[procedure and remaining matrix](FinalAcceptance.md) and
[measured results](qualification/WSA-UX-029_slice15.md).
Sharing receiver, native installed updates, human accessibility/mixed DPI and
user-deferred Hello remain explicit acceptance gaps; full sprint/release stay open.


## WSA-UX-029 slice 14 - signed local fixtures, native acceptance pending (2026-11-13)

The existing MSIX flow now prepares three isolated signed test packages without
installing or changing device trust. The update harness covers extended data,
higher-version rollback and packaged policy qualification. Build, 56 release
checks, 54 standalone payload GUI checks and synthetic Core/store readback pass.
Installed update/rollback and G05 remain unqualified pending explicit trust
approval; no production signer/feed/publication exists. See [actual evidence](qualification/WSA-UX-029_slice14.md)
and [release procedure](Release.md). macOS/backend/shared sources unchanged.


## WSA-UX-029 slice 13 - sharing client ready, receiver pending (2026-11-13)

The Windows client prepares the unchanged Core handoff, persists an account-bound
idempotent queue and reuses secure account HTTPS transport. Schema 9 adds separate
profile-bound consent; all real sharing stays source-gated/off. The user assigned
the receiver to the macOS backend agent. See [client contract](UnknownWordSharing.md),
[backend prompt](handoffs/WSA-UX-029_slice13_backend_agent_prompt.md) and
[qualification](qualification/WSA-UX-029_slice13.md). A07 remains unavailable;
mock success is not deployed-backend acceptance. Earlier entries are historical.


## WSA-UX-029 slice 12 - verified typing lookup (2026-11-13)

Automatic Danish lookup now works in the native editor and supported Notepad with
separate opt-in, Core-owned word boundaries, rate limits and late-write protection.
A06 is implemented; A07 remains unavailable pending the macOS-owned backend. Schema
8 never promotes manual consent into automatic permission. See [contract](AutomaticUnknownWords.md),
[qualification](qualification/WSA-UX-029_slice12.md) and the approved
[remaining four-slice scope](sprints/WSA-UX-029_closeout_slices12_15.md).
Earlier dated entries below are historical. Hello remains user-deferred; sprint
and release acceptance remain open. No macOS app/library or backend files changed.


## WSA-UX-029 slice 11 — explicit unknown-word import, 2026-11-13

Users can explicitly look up and import one Danish word through Settings > Advanced
or the editor menu, after separate online consent. Core validates dictionary data;
Windows atomically saves the separate local overlay. Imported words work offline
and survive restart; opting out stops lookup, explicit clear removes imported data.
A06 is partial (explicit import only), A07 unavailable; automatic typing capture is
off. Schema 7 defaults new/migrated consent to off. See [contract](UnknownWordImport.md),
[plan](sprints/WSA-UX-029_slice11_explicit_unknown_word_import.md) and
[qualification](qualification/WSA-UX-029_slice11.md). Older dated statements below
are historical. Native Hello remains user-deferred and release gates remain open.


## Slice 9 Advanced/manual qualification — 2026-11-12

See [diagnostics/privacy contract](Diagnostics.md) and [slice-9 qualification](qualification/WSA-UX-029_slice9.md).
Run AdvancedState, AdvancedUi, All and GUI from the Windows repository root.
AdvancedUi uses actual mouse/Space/UIA input with isolated local data. It does not
touch a real account or ask for Hello. GUI creates an owned Notepad fixture;
never target a user's document. The full current 72-control matrix distinguishes
unavailable features from incomplete platform acceptance.

1. Enable each of the five categories separately; trigger suggestions and panel
   changes. Check only the intended category appears under
   `%LOCALAPPDATA%\WordSuggestor\diagnostics\runtime`. Disable; new capture stops,
   existing files remain. No document text/word/window title/path/token may appear.
2. Verify independent App vs placement/panel/Core, internal/external panel state,
   static fallback, NativeEdit success/rejection and bounded timing aggregates.
3. Test disk full/access denial: keep typing, see local-write error, retain the
   user's configured choice and no false claim of a successful file write.
4. Reopen both settings surfaces and restart. Verify v5 byte-exact backup and
   panel flag, explicit false consent, old voice choices/user words/packs/documents.
   Legacy save must preserve newer primary changes.
5. Unknown-word controls must stay visible disabled/unchecked with explanation;
   no word lookup, upload or manual-user-word masquerade is permitted.
6. Human Narrator, keyboard traversal, high contrast, scaling and multi-monitor
   review remain required. Automated peer names/input are not human signoff.
7. Keep Hello parked. For updater, obtain a trusted isolated installed test fixture
   before policy mutation, then use the existing update runbook; no trust/policy
   bypass, public test feed or unsigned production release.

Earlier dated matrices below record their own checkpoints; final slice-9 evidence
states which earlier desktop/media restrictions were actually rerun successfully.

## Slice 8 update/manual qualification — 2026-11-12

See [Windows update contract](WindowsUpdates.md) and [slice-8 evidence](qualification/WSA-UX-029_slice8.md).
Run commands from the WordSuggestorWindows root. `test_settings_app.ps1 -Mode
UpdateUi` exercises real WPF input with synthetic HTTP/OS adapters and a fake
launcher; `All` includes it. It does not install or update a package.

1. In a loose developer build, confirm unknown/read-only automatic setting with
   an explanation; manual check must not open App Installer or touch user data.
2. With an already installed, trusted, isolated LocalTest package containing this
   candidate, run `scripts/test_settings_platform.ps1 -PackagedUpdateProbe
   -TestPackageFamilyName <family>` in an ordinary-user session. This activates
   the package itself, changes only schedule flags, verifies and restores all
   original settings. Production identities, elevation and paused fixtures are
   rejected. See the contract for fixture provisioning limitations.
3. Test a separately trusted beta identity/feed: current/newer/offline/invalid
   metadata and OS/feed disagreement. Wrong publisher, identity, downgrade and
   damaged signatures must fail in Windows; no direct payload fallback.
4. Confirm manual check alone never closes or installs. On an available offer,
   test No, Save/Discard/Cancel, slow save/expired ticket and confirmed close;
   App Installer must launch only after normal close completes.
5. In the isolated feed, install N→N+1, verify settings/user words/packs, relaunch,
   automatic/background checks, future feed overrides and uninstall behavior.
   Never publish synthetic test versions to the public feed.
6. Test Narrator, keyboard, high contrast, window resizing and multiple DPI/
   monitors. Automated native input is not human accessibility signoff.

Fresh isolated desktop acceptance is BLOCKED by the disconnected user session;
final Media exits 1 on an all-black OCR fixture, also reproduced with the prior
baseline. Rerun All, GUI and Media in an active unlocked session. Do not treat
earlier desktop evidence as fresh-source signoff.
Current native policy-write gate is BLOCKED; no trusted package was installed,
no machine policy was enabled and no production schedule was changed. Hello
PIN/biometric test is deferred by the user (2026-11-12), not failed or passed.

## Slice 7 account/manual qualification — 2026-11-11

Use [account/session/Hello contract](AccountAndHello.md) and [slice-7 qualification](qualification/WSA-UX-029_slice7.md) for exact automated results. `test_settings_app.ps1 -Mode AccountUi`
uses synthetic HTTP identities and a unique real Credential Manager fixture;
it makes no production account mutation. `All` includes it; `HelloNative` requires
explicit interactive execution and returns exit 3 when blocked.

1. In a local unlocked ordinary-user session with enrolled PIN/biometrics, run
   `test_settings_app.ps1 -Mode HelloNative`. Verify create/reopen, passive locked
   rejection, explicit recovery, encrypted rotation and protection-off migration.
2. Separately test actual user cancellation, lockout/unavailable hardware, OS lock,
   app close/reopen and packaged/unpackaged behavior. Prior data must remain intact
   after failed/cancelled setup. API/mock success is not positive qualification.
3. With a dedicated authorized backend test account, exercise verification-first
   registration/resend/cooldown and browser-confirmation then fresh login. Never
   send account passwords/PINs/tokens through chat or diagnostic logs.
4. Save profile name/catalog values and consent; test another-client conflict and
   second-write failure. Partial success must be explicit and drafts retained.
5. Change email with current password; confirm original identity until server
   acknowledgement; cancel pending request after reopening the app.
6. Change password, then reauthenticate. Local/all-device logout are separate;
   cancel/confirm the native all-device dialog and verify other test devices.
7. Test offline/expired/revoked recovery, reconnecting secure revocations and no
   stale-response adoption. Local suggestions/documents must remain usable.
8. Test keyboard/Narrator labels, focus, resize, high contrast and multiple DPI/
   monitors. Automated native input is not human accessibility/hardware signoff.

Current host: native Hello unavailable; positive PIN/biometric roundtrip not run.
No live-backend account acceptance is claimed. No enrollment/trust setting changed.

## Slice 6 — analysis and local word choices, 2026-11-11

Run `scripts/test_settings_app.ps1 -Mode All` and existing GUI/media/Core gates on
a connected unlocked desktop, without other input during the synthetic fixture.

1. Enable analysis/POS and verify real markers/counts; off clears stale work.
2. Switch semantic/punctuation, both comma styles and contextual/graph-only mode.
3. Right-click a Core finding, apply it, inspect caret and undo the whole group once.
4. Change text/tab/focus during analysis or an open menu; stale actions must fail.
5. Choose an ambiguous Core interpretation; local opt-out must not persist it.
6. Opt in locally; inspect count/detail/Back, restart, delete one, then test Yes/No
   for delete-all and reset. Reset must preserve documents, user words and insights.
7. Confirm sharing starts no upload/ranking; local-off clears sharing intent.
8. Confirm E02 stores no text by default, only bounded fragments after opt-in,
   and E01 off stops content and metadata. Inspect separate data locations.
9. Test oversized input, unavailable Core/pack, corrupt/future choice files and
   save/delete failure without false success or overwritten unknown data.
10. Complete human Narrator, contrast, extra DPI/monitors and audible quality review.

[Actual results and remaining manual gates](qualification/WSA-UX-029_slice6.md).

## Slice 5 desktop gate — 2026-11-11, not yet passed

Current session is disconnected/locked. On a connected unlocked desktop:

1. Run `scripts/test_settings_app.ps1 -Mode All`, plus existing GUI/media gates.
2. Check all F01–F20 rows, info help, keyboard focus and unavailable pack reasons.
3. Compare Smart/Always/Never insertion at selections, Unicode and suffix punctuation;
   use mouse and digits, inspect caret/undo and immediate punctuation/space followup.
4. Use two explicitly installed test domains; test both lane shortcuts and paging,
   reserved-key collision rollback and Danish AltGr without candidate acceptance.
5. Confirm hover debounce/leave/latest-utterance cancellation, 3-second fade/hide,
   disabled acceptance while fading and restoration from fresh input.
6. Check Core analyses/family choices and English physical newline/uppercase period
   eligibility; Danish/same-line/external text must not be auto-rewritten.
7. Verify wrap preserves document/text/caret/undo, including existing editor tabs.
8. Verify Notepad exact target/snapshot commit, focus/control change cancellation
   and Smart followup without clipboard changes. Other apps are not newly supported.
9. Restart and inspect retained schema-4 settings, false consents, user words/packs
   and byte-exact migration backup; unavailable domain IDs survive unrelated saves.
10. Check Narrator, high contrast, 125/150/200% DPI and multiple monitors separately.

Record actual results in [slice 5 evidence](qualification/WSA-UX-029_slice5.md).

Updated 2026-11-09 (editor tabs). Automated desktop observation is distinct from a human manual
audio/accessibility/hardware session. No full manual sign-off is claimed.

## Settings slice 4 — 2026-11-10

[Exact-source qualification and manual matrix](qualification/WSA-UX-029_slice4.md)
cover actual native speech, physical editor/Notepad input and lifecycle safety.
Use `scripts\test_settings_app.ps1 -Mode Speech` for policy/migration/process/native
playback, and `-Mode SpeechUi` for keyboard/focus/marker/control tests. The default
All mode includes both. These gates play synthetic audio and require an unlocked
desktop; do not interact during GUI tests. Human listening/Narrator/mixed-DPI and
signed-update checks remain explicitly open. Recovery is in [Speech.md](Speech.md).

Human smoke: choose a voice, adjust 50/100/150%, use each typing strategy, then
change highlight mode. Continue typing the next word during audio; move the caret,
change window/tab, stop and restart. Check normal selection, text and undo. Repeat
in supported Notepad with the internal editor open. Inspect retained system mode
in legacy, then change primary tempo and verify the explicit override persists.
Verify the OS settings links on each supported Windows version. Full listening,
hardware accessibility and release-installation approval is not inferred from
automated desktop observation.

## Settings slice 3 — 2026-11-10

[Qualification and exact-source results](qualification/WSA-UX-029_slice3.md) cover
the new Core boundary, not completed Settings feature parity. Run
`scripts\test_core_cli.ps1 -SettingsContract` and
`scripts\test_settings_app.ps1 -Mode Core`; the default Settings gate includes
Core, state and UI. Existing editor/Notepad focus and acceptance gates still apply.
No UI controls are enabled by this slice. A live document-switch caller must
recheck returned scope before applying results; current scope/mismatch checks
are adapter-level, not a claim of a new analysis UI flow.

Portable contracts pass on Windows only; matching macOS execution remains open.
The repository-wide Swift test was attempted but cannot compile unchanged Windows
test/tool sources using CFAbsoluteTimeGetCurrent. Hardware DPI, Narrator/Hello,
audible TTS and signed install/update gates remain open. See the linked matrix.

## Settings slice 2 — 2026-11-10

[Current evidence and recovery](qualification/WSA-UX-029_slice2.md) supersede the
slice-1 Save/Cancel behavior below for the primary window. Run the same
`scripts\test_settings_app.ps1`: 63 state and 58 actual-window checks cover live
save, independent UI/writing language, legacy conflicts, I/O/corrupt failures,
permission semantics and native dropdown Escape. Extended real-app/Notepad tests
verify hidden/re-enabled overlay choice, stable static placement across caret moves,
explicit follow selection, preserved focus and unchanged target text.

Human checks: switch Follow Windows/Danish/English while keeping Danish writing
language and an open document. Check headers, option/help/accessibility text and
voice retention. Change a preference, close/reopen/restart and verify persistence.
Keep legacy open, change different fields in both views, then save legacy and check
both values. Deliberately conflict the three-valued highlight choice and verify
rejection without losing either persisted state or the legacy draft. Cancel legacy.
Disable caret permission, write/move the caret in Notepad and verify a static panel;
enable it and verify it remains static until selecting follow explicitly.

Test invalid/unwritable preferences only in a disposable profile; check that live
controls revert, errors are readable and stored bytes survive. Rehearse schema-1
migration and recovery using the linked procedure. These human/Narrator/high-contrast,
mixed-DPI/multiple-screen and signed-update checks remain **NOT RUN**.
Older app copy outside Settings/legacy is not fully translated.

## Historical Settings slice 1 — 2026-11-10

The native sidebar, grouped rows, single info popup and Windows legacy window are
implemented. [Settings evidence](qualification/WSA-UX-029_slice1.md) records 20
state and 37 interactive checks, plus the preserved working-tree app/Core/Notepad
regression. These do not close the full 72-control functional parity target.

Run `powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1`
from this repo (`-Mode State` skips desktop input). UI mode requires an unlocked
desktop and uses isolated synthetic settings. It moves mouse/keyboard.

For human sign-off: open toolbar Settings; verify Profile first and the seven
categories below it. Navigate with arrows/Tab/Shift+Tab; open help using keyboard
and mouse, repeat the same info action, Escape, click outside and change category.
Check one popup and sensible focus/reading order. Resize to 760×480 and scroll only
the right area. In Advanced, open legacy twice and verify one separate activating
window. Change different settings in both views, save legacy then primary, restart
and verify both values. Cancel another draft. Verify error status on a test-only
unwritable/corrupt settings file without overwriting it. Never corrupt real data.

Walk through with Narrator/NVDA and high contrast, then 125/150/200% scaling and
multiple monitors. **These human/hardware checks have not been run.** Konto/Hello,
live save, OS update preference and later functional controls remain open. Keep
slice 0 platform failures and the existing signed install/update gates visible.

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

`test_gui.ps1 -EditorOnly` uses an isolated profile with external capture disabled,
so an existing user instance can remain open. For an independent build add
`-SkipBuild -FrameworkDependentApp -RunnerPath <smoke-exe> -AppPath <app-exe>`.
This mode deliberately skips Notepad/global-hotkey integration checks.

November 12 cleanup qualification: clean-source build and 94 app checks pass,
including 25 tab/storage checks. Full Core/Settings and actual desktop results,
including initial failures and their follow-up, are in
[WSA-UX-013 qualification](qualification/WSA-UX-013_editor_tabs.md).
Automated desktop evidence remains separate from human manual sign-off.

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
| Tabs and recovery | PASS automated model/storage cases and actual desktop new/switch/undo/selection/dirty-close/Unicode-restart checks; future/corrupt session data retained |
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
8. Create several tabs using +, Ctrl+T and Filer; switch with mouse and Ctrl+Tab /
   Ctrl+Shift+Tab. Edit different text and selections, use undo/redo, and verify
   suggestions always insert in the active document. Test Save/Save As, reopening
   the same file with unsaved edits, close Yes/No/Cancel, closing an inactive tab,
   and closing the final tab. At app exit, cancel a later prompt and verify earlier
   tabs remain. Hide/reopen, then restart to check drafts/caret/file paths. Compare
   many tabs, high contrast, Narrator and speech switching with macOS semantics.

## Troubleshooting

### Build output locked by WordSuggestor

The toolbar may still be running after the editor window is closed. From the
workspace parent run `powershell -ExecutionPolicy Bypass -File
.\WordSuggestorWindows\scripts\run_app.ps1 -SkipBootstrap -Restart` and respond to
the app's save prompts. The script posts normal close only to the verified
executable's toolbar, waits for actual process exit and then builds/launches.
Cancel or timeout preserves the running app and aborts the build; no forced
termination is used. Standalone build also fails early for a locked output and
offers the independent `-ArtifactsPath` alternative.

`powershell -ExecutionPolicy Bypass -File scripts\test_run_scripts.ps1` verifies
exact-path selection, wrong-target rejection, normal close and cancelled-close
retention against an invisible synthetic window. Fourteen checks passed on November 12, including relative paths, child-only startup
text and parent environment restoration. No user app was closed by that test.

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

## Release tooling cleanup — 2026-11-12

Run `scripts/test_release_scripts.ps1` for fixture manifest/hash/identity/feed,
ED25519 pin, WhatIf, server exit-code and mocked private staging tests. This makes
no SSH connection and changes no trust. See [the measured cleanup matrix](qualification/WSA-RT-017_release_cleanup.md).
Use [Release.md](Release.md) for a separately authorized disposable signed N-to-N+1
test, including automatic/manual checks, invalid packages, retention and uninstall.
The cleanup does not close any native installation/update or public HTTPS gate.
The default local MSIX harness returns BLOCKED until temporary trust is explicitly
opted into. Human accessibility/DPI, actual cross-update editor draft retention,
production trust and live server/Mac regression remain separate acceptance.

## Slice 10 completed-word transport — WSA-UX-029, 2026-11-12

Run `scripts/test_settings_app.ps1 -Mode CommittedWord` for real Core and hostile process fixtures. The operation is not wired to normal typing; verify A06/A07 remain disabled/off and ordinary editor/Notepad suggestions, tab recovery and diagnostics remain unchanged. Full native/manual results are recorded in [slice-10 qualification](qualification/WSA-UX-029_slice10.md); no new Hello, signing or install/update sign-off is inferred.

## Explicit import human follow-up (slice 11)

1. In Advanced, verify lookup starts off; decline the separate consent explanation.
   Reopen and approve; confirm the text names both dictionary sites and one word.
2. Open the import dialog from Settings and editor; keyboard-navigate every control.
   Choose an eligible Danish word absent from the installed pack and look it up.
3. Check success/miss/offline states, then switch focus or close during lookup.
   No cancelled lookup may later create or replace the learned snapshot.
4. Restart and check imported-word suggestions offline; revoke online consent and
   verify existing local suggestions remain. Explicitly clear with native confirmation;
   ensure manual user words, choice data, settings and pack remain intact.
5. Repeat with Narrator and mixed DPI/multiple monitors. These human checks remain
   NOT RUN; automated coverage and tested source are in qualification/WSA-UX-029_slice11.md.
