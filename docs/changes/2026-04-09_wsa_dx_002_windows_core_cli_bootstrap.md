# Windows Core CLI Bootstrap

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `implemented`
- Sprint ID: `WSA-DX-002_windows_core_cli_bootstrap`

## Summary

Added a reproducible local Windows bootstrap path for `WordSuggestorCore` CLI execution, including SQLite dev artifact provisioning, SDK-path normalization, and a repo-local smoke script that validates CLI output against the Danish pack.

## User-Visible Behavior

- No direct end-user UI change.
- The Windows app track can now build and validate the local `WordSuggestorCore` CLI dependency on this machine.

## Implementation Details

- Added `bootstrap_core_cli.ps1` to:
  - enter the Visual Studio x64 developer shell,
  - provision `sqlite3.h` and `sqlite3.lib` under `WordSuggestorWindows/.artifacts/sqlite-dev`,
  - pin Windows SDK env vars to `10.0.22621.0`,
  - normalize Windows Kits include/lib path variables so Swift does not mix `10.0.22621.0` with `10.0.26100.0`,
  - build `WordSuggestorSuggestCLI`,
  - publish `WORDSUGGESTOR_SUGGEST_CLI_PATH` for the current PowerShell process.
- Updated `test_core_cli.ps1` to:
  - run the bootstrap first,
  - execute the built CLI executable when available instead of re-entering `swift run`.
- Updated repo docs to surface the bootstrap script as part of the Windows runbook.

## Files Changed

- `.gitignore` - ignores local artifact cache
- `README.md` - documents the bootstrap script
- `scripts/bootstrap_core_cli.ps1` - local Windows CLI/bootstrap workflow
- `scripts/test_core_cli.ps1` - smoke test now uses the built CLI executable
- `docs/Plan.md` - records `WSA-DX-002` and updates Windows track status
- `docs/changes/2026-04-09_wsa_rt_001_windows_core_bridge_and_internal_editor.md` - notes that the earlier bridge blocker is resolved
- `docs/changes/2026-04-09_wsa_dx_002_windows_core_cli_bootstrap.md` - this note

## Feature Flags / Settings

- Optional environment variable:
  - `WORDSUGGESTOR_SUGGEST_CLI_PATH` can still be supplied explicitly, but the bootstrap now resolves and exports it automatically in the invoking shell.

## Logging / Telemetry

- No telemetry added.
- The bootstrap script emits clear console output for:
  - SQLite artifact location
  - resolved CLI path
  - modulemap-bootstrap warning state

## Validation

- Commands run:
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1`
  - `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1`
- Result:
  - WPF app build: `PASS`
  - Local `WordSuggestorCore` CLI bootstrap + smoke: `PASS`
  - Observed CLI output for `skri` included Danish suggestions such as `skrive`, `skriv`, and `skriver`.

## Known Limitations / Follow-up

- The script cannot copy missing modulemaps into `C:\Program Files (x86)\Windows Kits\10\Include\10.0.22621.0\...` under the current user context, so modulemap bootstrap remains best-effort with a warning.
- The working local path currently succeeds despite that warning because SDK pinning and environment normalization are sufficient on this machine.
- Next step should be a manual Windows UI smoke of the WPF app using the now-working local CLI bridge.
