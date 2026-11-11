# WSA-UX-029 slice 5 — suggestion settings qualification

## Follow-up on slice 6 source — 2026-11-11

The session is now connected. Re-running the old slice-5 candidate reached the
Notepad check and exposed current-key-state sampling after key-up. The slice-6
fix consumes WM_HOTKEY modifier metadata, retaining exact modifiers/target checks.
The updated working tree passes all 48 suggestion checks, including physical
mouse/digits, three lanes, Smart followup, fading and real Notepad insertion.
This is evidence for the new source, not a retrospective PASS for `18a1f6b`.
See [final exact-source slice-6 qualification](WSA-UX-029_slice6.md). Human
Narrator/audio and extra DPI/monitors remain open.

Date: 2026-11-11. Windows base `2a5cac49`; Core
`7491a0b748ac87d5286b50f4c0c93d1b90c6a84d`; canonical Mac `ba18b45`.
Status: **implementation candidate; desktop qualification pending**.

The WPF implementation is retained and extended. Existing unrelated editor tabs,
developer restart and release changes are preserved outside this slice's index.
No shared/macOS/backend files are changed. See [runtime/data contract](../Suggestions.md).

## Results and reproduction

Initial working-tree checks: isolated app/smoke build passed with zero warnings
and errors. Core executable gate passed 301 checks; Windows Core adapter 58,
Settings state 63, native speech adapter 102, new suggestion state 27.
Exact staged-source export results are recorded below after qualification.

Final isolated source qualification passed on 2026-11-11. Windows staged tree
`b169330dd4f8daa45d3da3da212a1e52136116af` and the committed pinned Core were
exported with `git archive` into empty sibling directories. No working-tree
sources, untracked editor/release files, generated binaries or caches were copied.
Both app and Core were built inside that export. Final edits after this gate are
documentation only; source subtrees are checked unchanged before commit.

| Existing gate / mode | Exact candidate result |
|---|---|
| `build_app.ps1` | PASS, 0 warnings, 0 errors |
| `test_core_cli.ps1 -SettingsContract` | PASS, 6 real DA fixtures and 301 portable checks |
| `test_app.ps1 -SkipBuild` | PASS, 67 checks, including uncooperative/out-of-order provider regression |
| `test_settings_app.ps1 -Mode State -SkipBuild` | PASS, 63 checks |
| `test_settings_app.ps1 -Mode Core -SkipBuild` | PASS, 58 adapter checks |
| `test_settings_app.ps1 -Mode Speech -SkipBuild` | PASS, 102 checks, native PCM/cues; audible quality not human-verified |
| `test_settings_app.ps1 -Mode SuggestionState -SkipBuild` | PASS, 27 state/scheduling/real Core commit checks |
| `test_settings_qualification.ps1` | PASS, 35 contract/safety checks |
| Desktop Settings/GUI/media/speech UI | BLOCKED, disconnected locked session; not a PASS |

Qualified source identities:

- `src`: `63b6071d50a88da7fb8a2897deb6c8070aa1f13a`
- `tests`: `234a83c824f7b8ed1edf16c87d134c5ffedda33a`
- `scripts`: `405838e6684b54a7811c394ac455349362d6d4c1`
- `dependencies.lock.json`: `3ba9911203a7c82a457d7fa528d6c7958b5178ea`

The initial new queue failed the existing out-of-order provider test. Its extra
semaphore was removed: production child execution remains bounded by the existing
adapter process gate; an uncooperative legacy provider cannot block a newer
request, and its late result remains generation-checked. The unchanged 67-check
app gate now passes. No test expectation was weakened to hide that failure.
Full Swift-suite/macOS execution is not claimed; the prior unrelated Windows
CFAbsoluteTimeGetCurrent suite compilation issue remains outside this slice.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode SuggestionState -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode State -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode Core -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode Speech -SkipBuild
# Unlocked, connected desktop required:
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
```

The five-profile controlled fixture sends 12 text changes at 10 ms intervals to
an 80 ms cancellable provider. Each profile produced one completed request and
max one active request. This measures orchestration with a timed provider, not
real Core latency. Raw per-profile measurements remain in local ignored logs.

## Desktop smoke matrix

| Coverage | Current result |
|---|---|
| Twenty Settings rows, labels, missing-data states, live save/shortcut swap | WPF fixture reached/passed these assertions before the focus boundary; full desktop flow incomplete |
| Real Core Smart/Always/Never local transaction, suffix and caret | Headless transaction PASS; physical acceptance still pending |
| Physical mouse/digit acceptance, three lanes, Notepad | Automated fixture implemented; not completed on current locked/disconnected session |
| Hover TTS cancellation, idle fade/hide, wrap/undo | Implemented; desktop qualification pending |
| Conflict rollback, AltGr, family choices, English live period | Pending targeted native qualification |
| High contrast, Narrator reading, extra DPI/monitors | Not run; no accessibility/hardware signoff claimed |
| Existing GUI, media, speech UI regression | Blocked by disconnected desktop; earlier slice PASS is not new evidence |

Native evidence: `query session` reported mswin01 session 2 **Disc** and LogonUI
was running in that session. Both the new fixture and the unchanged speech UI
gate safely rejected missing foreground focus before injecting input. Reconnect
and unlock this session, rerun the desktop gates and record actual results before
calling slice 5 functionally qualified. No tests are weakened to bypass that gate.

## Known differences and recovery

F10 source correction: canonical Mac limits live period to English newline
transitions. The earlier analysis's broader uppercase description is superseded.
Windows enables this only in its internal editor; external rewriting remains
disabled pending an independently qualified adapter. Pack-dependent controls
remain unavailable for missing data; seed domain fixtures are not product packs.
Schema-4 backups preserve previous settings. Recover manually from the exact
`.v3.bak` while the app is closed; older binaries refuse the newer schema rather
than destroying it. No changes to user words/packs/backend/update/storage scope.
