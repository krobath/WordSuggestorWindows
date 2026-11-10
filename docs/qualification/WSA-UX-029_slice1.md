# WSA-UX-029 slice 1 — Settings UI qualification

Date: 2026-11-10. Windows 11 host `10.0.26300.0`, ordinary user, x64, one monitor,
96 DPI. .NET SDK 10.0.403. Canonical macOS reference `ba18b45`.

## Scope and outcome

The native sidebar, grouped rows, help/focus behavior and non-recursive Windows
legacy view are implemented. Later functional slices and slice 0's positive
Hello/OS-update gates remain open. The 72-control register is a frozen baseline,
not a claim that all controls have now passed runtime qualification.

Initial regression runs use the preserved dirty working tree, including prior
editor/release work. Their results cannot be attributed solely to this commit.
An isolated export of the staged commit is checked separately below.

The isolated candidate export **passed** build, fresh Core/SQLite bootstrap, six
Core fixtures, the standard app suite (including 13 update metadata checks),
20 Settings state checks, 37 Settings desktop checks, 25 baseline app/Notepad GUI
checks, three media adapter checks and 35 settings qualification contract/safety
checks. Its GUI suite has the committed baseline scope; the 42-check working-tree
suite additionally contains the pre-existing tab/recovery work excluded here.

Qualified candidate tree: `ac3a7df1f581ebca4440a214bba669b3b63669e4`.
No working-tree sources, build outputs or repository caches were copied. Existing
installed toolchains remained prerequisites. Final documentation records were
added after this export; implementation/test/script subtrees remain identical:

- `src`: `3c3560e26f5fcb299c2b8c9d160ef830a2c72f1f`
- `tests`: `49d9008b8f6a533a5479effae93140dc0855b6b4`
- `scripts`: `21614c35d94ee2b08e506d2dc0e449bc79a9fe91`

To reproduce the isolated-source procedure, export the completed Windows commit
and the locked Core commit as sibling directories using `git archive`, without
their bin/obj/.build/.artifacts directories. Keep the installed SDK on PATH. Run
the exported repo's `scripts\build_app.ps1`, `scripts\test_core_cli.ps1`,
`scripts\test_app.ps1 -SkipBuild`, `scripts\test_settings_app.ps1 -SkipBuild`,
`scripts\test_gui.ps1 -SkipBuild`, `scripts\test_media.ps1 -SkipBuild` and
`scripts\test_settings_qualification.ps1` under an ordinary user with an unlocked
desktop. No production secrets are needed.

## Commands and recorded results

Run from the Windows repository. For isolated output in an existing working tree:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1 -ArtifactsPath .artifacts\settings-slice1
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -SkipBuild -RunnerPath .artifacts\settings-slice1\bin\WordSuggestorWindows.Smoke\debug\WordSuggestorWindows.Smoke.exe
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild -RunnerPath .artifacts\settings-slice1\bin\WordSuggestorWindows.Smoke\debug\WordSuggestorWindows.Smoke.exe
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild -FrameworkDependentApp -AppPath .artifacts\settings-slice1\bin\WordSuggestorWindows.App\debug\WordSuggestorWindows.App.exe -RunnerPath .artifacts\settings-slice1\bin\WordSuggestorWindows.Smoke\debug\WordSuggestorWindows.Smoke.exe
```

| Gate | Actual result and boundary |
|---|---|
| Build | PASS, zero C# warnings/errors |
| Settings state | PASS, 20 checks; unknown properties, independent/concurrent edits, conflict rejection, voice keys/removal, backup, restart, future/corrupt file retention |
| Settings desktop | PASS, 37 checks; actual windows, native mouse/keyboard, UIA category IDs/names and version value; isolated settings |
| App regression | PASS, 92 checks in dirty working tree, including pre-existing tab/update checks and real Core requests |
| Core CLI | PASS, six real DA JSON/batch parity fixtures; known pkg-config/SQLite discovery and debug symlink warnings, build exits zero |
| Full desktop regression | PASS, 42 checks in dirty working tree; Notepad 11.2607.14.0, external mouse/Ctrl+1, clipboard/focus, editor/files/tabs, Settings version/manual check and recovery |

Core revision remains `5d8d7a0fd578a8373ce17f79a25e78a1197692c1`; DA pack SHA-256
`6970C398077C235355F513D9491E4193534FE88B751AEEA07EDDEF9ABC3FB82F`.
No live account, production upload, signing or installation was exercised.
Sandbox SDK/atomic-replace restrictions required running native build/desktop
gates outside the sandbox; the process remained an ordinary Windows user.

## Manual smoke matrix

| Flow | Evidence | Human/hardware remainder |
|---|---|---|
| Eight categories; Profile initial | PASS actual WPF and UIA order/names | Product visual approval |
| Mouse, arrows, focus | PASS real input and keyboard focus | Full keyboard/Narrator walkthrough |
| Info open/switch/repeat/Escape | PASS one popup, proper dismissal and originating focus | Human screen reader reading of descriptions |
| Right-only scroll, 760×480 | PASS actual compact layout and reachable bottom control/footer | Large fonts, 125/150/200% DPI and multiple monitors |
| Visual hierarchy | General and compact Speech renders inspected; grouped rows/aligned columns | Compare all final pages after functional slices |
| Legacy | PASS one owned activating window, six old tabs, no recursion | Human high-contrast review of inherited legacy layout |
| Gem/Annuller | PASS draft behavior, two-window merge, Cancel, restart | Live save intentionally next slice |
| Failure | PASS invalid file retained; dialog stays open with status | Human screen reader live-message announcement |
| Account/Hello | Unavailable status, disabled Hello; no stored credentials | Positive protected-session test remains open |
| Updates | Existing manual check retained; metadata failure contract tests | Real signed N→N+1/OS automatic-preference qualification remains open |

This is automated desktop evidence and developer visual inspection, not complete
human accessibility or release sign-off.
