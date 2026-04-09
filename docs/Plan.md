# WordSuggestorWindows Plan

Last updated: `2026-04-09`
Owner: `Windows track`
Status legend: `Done`, `In progress`, `Planned`, `Blocked`

## Naming rule

Canonical sprint IDs follow:

- `<AREA>-<TRACK>-<NNN>_<short-slug>`

For this repository, the expected default area is:

- `WSA` for app/runtime/UI work

Typical track codes for the Windows port:

- `DX` - repo/process/baseline
- `RT` - runtime/input/caret/panel/commit integration
- `TS` - smoke tests and validation

## Active plan

### WSA-DX-001_windows-repo-baseline
Status: `Done` (`2026-04-09`)

Scope:

- Create the Windows delivery repository baseline.
- Establish architecture, parity, and documentation conventions.
- Define the first safe implementation order.

Deliverables:

- `README.md`
- `docs/Architecture.md`
- `docs/ParityMatrix.md`
- `docs/changes/2026-04-09_wsa_dx_001_windows_repo_baseline.md`

Exit criteria:

- Windows repo has a canonical plan and architecture baseline.
- Porting work is staged by risk instead of attempting a direct AppKit-to-Windows rewrite.

### WSA-RT-001_windows_core_bridge_and_internal_editor
Status: `Planned`

Scope:

- Establish the first Windows app shell.
- Prove end-to-end local suggestions in an internal editor.
- Bridge Windows app code to `WordSuggestorCore`.

Target outcome:

- User can type in the Windows app and receive local WordSuggestor suggestions without cross-app integration.

### WSA-RT-002_windows_overlay_panel_and_commit_path
Status: `Planned`

Scope:

- Implement Windows suggestion panel behavior for the app shell.
- Add suggestion acceptance and insertion behavior.
- Align keyboard and panel interaction with the macOS UX contract where reasonable.

### WSA-RT-003_windows_external_input_and_caret_integration
Status: `Planned`

Scope:

- Implement global typing capture and focused-text tracking on Windows.
- Implement caret/selection/context extraction for supported targets.
- Implement external-app commit path with guarded fallbacks.

### WSA-TS-001_windows_smoke_and_regression_baseline
Status: `Planned`

Scope:

- Define local smoke commands and validation artifacts.
- Add parity-focused regression checklist for Windows milestones.

## Recommended implementation order

1. `WSA-DX-001` - baseline docs and repo structure
2. `WSA-RT-001` - internal editor + core bridge
3. `WSA-RT-002` - in-app suggestion panel parity
4. `WSA-RT-003` - external-app Windows integration
5. `WSA-TS-001` - smoke and regression gates

## Working rules for this repo

1. Each sprint gets a change-note in `docs/changes/`.
2. Each completed sprint updates this plan.
3. If validation is deferred, the reason must be written explicitly.
4. Commit messages should include the sprint ID at the start.
