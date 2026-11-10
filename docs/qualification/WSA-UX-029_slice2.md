# WSA-UX-029 slice 2 — live Settings, migration and General

Date: 2026-11-10. Windows x64 host `10.0.26300.0`, ordinary user, one monitor at
96 DPI. .NET SDK 10.0.403. macOS reference `ba18b45`; Windows base `33923af`.

## Scope

Implemented: live Settings saving, schema 2 migration, independent interface and
writing-language preferences, Danish/English Settings and legacy UI, and the
Follow caret permission with real measurement guards/static fallback. Full
application translation and the remaining functional slices are not qualified.
No Core/backend contract, language-pack, credential or signing change is made.

## Reproduce

Run from the Windows repository; existing build and test scripts are retained:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1 -ArtifactsPath .artifacts\settings-slice2
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -SkipBuild -RunnerPath .artifacts\settings-slice2\bin\WordSuggestorWindows.Smoke\debug\WordSuggestorWindows.Smoke.exe
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild -RunnerPath .artifacts\settings-slice2\bin\WordSuggestorWindows.Smoke\debug\WordSuggestorWindows.Smoke.exe
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SkipBootstrap
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild -FrameworkDependentApp -AppPath .artifacts\settings-slice2\bin\WordSuggestorWindows.App\debug\WordSuggestorWindows.App.exe -RunnerPath .artifacts\settings-slice2\bin\WordSuggestorWindows.Smoke\debug\WordSuggestorWindows.Smoke.exe
```

Do not interact with the mouse/keyboard during desktop gates. Tests create isolated
synthetic profiles; they do not change the normal user profile or install trust.
Native SDK/atomic-file and desktop gates require access beyond the tool sandbox;
their Windows process still runs as an ordinary user.

## Results

| Gate | Evidence |
|---|---|
| Isolated output build | PASS, zero warnings/errors |
| Settings state | PASS, 63 checks: V1 in-memory migration, byte-exact BOM backup, false/unknown preservation, strict/future data, one notification, conflicts, locale resolution, pack/runtime rejection and last-applied presentation after failure |
| Settings desktop | PASS, 58 checks: actual windows/mouse/keyboard/UIA, English Settings with real Danish Core, permission/placement distinction, explicit legacy drafts/conflicts, live failure rollback, real file lock, native dropdown Escape and reopening after failure |
| Working-tree app | PASS, 92 existing checks including pre-existing editor/update work |
| Core | PASS, six real DA JSON/batch parity fixtures against existing qualified CLI |
| Working-tree app/Notepad GUI | PASS, 47 checks including five new caret-permission/overlay checks and existing tab/recovery work |

Working-tree regression includes earlier uncommitted features, which are not
silently included in this slice's commit. The final isolated export passed:
build (zero warnings/errors), fresh Core/SQLite and six fixtures, 67 standard app
checks, 63 Settings state checks, 58 Settings desktop checks, 30 app/Notepad GUI
checks, three media checks and 35 qualification contract/safety checks. The five
new permission/overlay checks are present in the committed GUI suite; the older
tab/recovery extensions account for the larger dirty-working-tree suite.

Qualified candidate tree: `015127db9167f0cd727b2fcb62868d06e1bb0d86`.
Windows and Core were exported from git into empty sibling directories, with no
source/build caches copied. Installed toolchains were prerequisites. Build and
Core output locations were explicitly checked inside that export. One earlier
wrapper run resolved relative paths against the wrong native working directory;
it is marked invalid for isolated-source qualification and counts only as a
working-tree regression. The corrected run above is the qualification evidence.

The final documentation is recorded after qualification; the implementation,
tests and script subtree hashes are verified unchanged before commit.

- `src`: `0e40a630d6e6bbe0ae5f775e3513a2d50c7cc62f`
- `tests`: `0cfe3ddcf313192b12ca0d12414412d994d4020f`
- `scripts`: `21614c35d94ee2b08e506d2dc0e449bc79a9fe91`

To reproduce clean-source qualification, use `git archive` to export the completed
Windows commit and the locked Core commit into empty sibling directories. Keep the
installed SDK on PATH, then run the exported repo's existing build, Core bootstrap,
app, Settings, GUI, media and qualification scripts. Use `-SkipBuild` for app/UI/media
after the first build; do not use `-SkipBootstrap` for the first exported Core build.
Check that logs name the exported output directories, not the original workspace.
No production secrets, installed production beta or signing identity are required.

Core stays at `5d8d7a0fd578a8373ce17f79a25e78a1197692c1`; Danish pack SHA-256 is
`6970C398077C235355F513D9491E4193534FE88B751AEEA07EDDEF9ABC3FB82F`.

## Control and manual smoke matrix

| Area | Automated/developer evidence | Remaining human/hardware qualification |
|---|---|---|
| G01 interface language | Three persisted choices, explicit/system resolution, matched embedded catalogues, live labels/help/headers and UIA names | Narrator pronunciation/announcements and long translated labels; older non-Settings app copy is not fully translated |
| G02 writing language | Shared provider/toolbar authority; unavailable packs disabled/rejected; real DA suggestions under English Settings | Actual compatible EN/DE packs; cross-platform fixtures with identical packs |
| G03 permission | Off persists static and stops new geometry; on does not auto-follow; real Notepad overlay choice hidden/re-enabled, static across caret moves and no target edits | Physical mixed DPI/multiple screens; uncertain geometry continues existing static fallback |
| Live save | Immediate file/runtime state, Close/Escape retain it, unknown properties/voices preserved | Human screen reader status announcements |
| Legacy | Independent saved changes merge; conflicting draft stays open; Cancel affects only that draft | Full human keyboard/high-contrast walkthrough |
| Failure/privacy | Real sharing violation and corrupt data leave bytes/runtime unchanged; unavailable startup defaults disable external capture/content collection | Operator recovery rehearsal on a dedicated user profile |
| Migration | Exact v1 backup plus rolling .bak, unchanged writing/voice/false values, future writes blocked | Signed MSIX N→N+1/known-good-higher-version rollback is still an open release gate |
| UI sizes | 760×480 scrolling/footer tests and inspected Danish/English renders | 125/150/200% DPI, multiple monitors, high contrast, Narrator/NVDA |
| Account/update | Existing unavailable states/manual update behavior retained | Positive Hello/OS-update qualification remains open from slice 0 |

All desktop evidence is automated plus developer inspection; no complete human
accessibility, acoustic/microphone or installed-release sign-off is claimed.

## Data recovery and rollback

The running app retains and presents its last successfully applied snapshot if
the file later becomes unavailable; reopening Settings shows the warning alongside
that effective state. It does not misleadingly show external capture as off while
the last applied session still has it enabled. A fresh unavailable-profile launch
uses the safe off defaults described below.

The app keeps the historical path `settings/settings-v1.json` under its existing
LocalAppData root, but writes `schemaVersion: 2`. User words, packs, editor drafts,
documents and diagnostics remain at their existing independent locations.

V1 reading is side-effect-free. First successful persistence copies the exact
original file to `settings-v1.json.v1.bak` once, then atomically installs schema 2
and keeps the normal rolling `settings-v1.json.bak`. Interface defaults to
`system_default`, never to the writing language. Permission defaults true for a
v1 profile and preserves its old static/follow intention; a new explicit false
always normalizes to static. Existing false preferences and unknown data survive.

For an unreadable, invalid or future profile, the app refuses writes and preserves
the file. Internal offline writing remains usable, while unavailable-profile
startup disables external capture, error tracking, text examples and caret work.
Recover only with the app fully exited: copy the current file somewhere safe,
inspect its schema, and restore a known-good compatible backup if appropriate.
Never edit/delete a file still owned by a running app. Do not remove user words,
packs or editor data to repair a preferences file. Renaming a schema-2 file to a
schema-1 filename is not a migration; the filename already remains historical.

Older slice-1 code refuses to save schema 2, preserving the newer data. A release
rollback must use a higher MSIX package version and code that understands schema
2, or an explicitly reviewed preference restore from the exact v1 backup. That
restore loses post-migration preference changes only; it must not reset independent
user content. Do not downgrade or relabel schema 2 as schema 1 by editing its number.
