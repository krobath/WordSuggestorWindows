# Windows Repo Baseline

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `implemented`
- Sprint ID: `WSA-DX-001_windows-repo-baseline`

## Summary

Established the initial `WordSuggestorWindows` repository baseline so the Windows port can proceed with the same planning and documentation discipline used across the rest of the workspace.

## User-Visible Behavior

- No end-user behavior change yet.
- This sprint creates the delivery baseline for upcoming Windows implementation work.

## Implementation Details

- Added a repository README that defines the Windows repo as the native host application track for WordSuggestor.
- Added a Windows plan document with canonical sprint IDs, staged milestones, and working rules.
- Added an architecture baseline that treats `WordSuggestorCore` as shared and Windows runtime/UI/integration as native adapters.
- Added a parity matrix to keep feature delivery ordered by risk and avoid a direct AppKit-to-Windows rewrite.

## Files Changed

- `README.md` - defines repository purpose and document entry points
- `docs/Plan.md` - active Windows sprint plan and execution order
- `docs/Architecture.md` - target architecture and adapter model
- `docs/ParityMatrix.md` - initial macOS-to-Windows feature parity tracking
- `docs/changes/2026-04-09_wsa_dx_001_windows_repo_baseline.md` - sprint implementation note

## Feature Flags / Settings

- None in this sprint.

## Logging / Telemetry

- None in this sprint.

## Validation

- Commands run:
  - `Get-ChildItem WordSuggestorWindows`
- Result:
  - Repository was confirmed empty except for `.git` before baseline files were created.
  - No build or runtime validation was performed in this sprint.

## Known Limitations / Follow-up

- No Windows app scaffold exists yet.
- No `WordSuggestorCore` bridge exists yet.
- Next sprint should establish the first runnable Windows app shell and a minimal suggestion flow.
