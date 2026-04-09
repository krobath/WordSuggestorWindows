# Windows UI Parity Plan Baseline

- Date: `2026-04-09`
- Area: `WordSuggestor`
- Author: `Codex`
- Status: `done`
- Sprint ID: `WSA-DX-003_windows_ui_parity_plan`

## Summary

Converted the macOS UI review and the user's clarifications into a concrete Windows UI parity baseline so the next implementation sprints can target the right shell, editor, overlay, and popover behavior.

## User-Visible Behavior

- No runtime behavior changed in this sprint.
- The Windows repo documentation now explicitly defines the intended Windows UX direction:
  - native Windows look and feel
  - same app icon as macOS
  - same major surface layout and control ordering as macOS
  - static/follow-caret suggestion panel modes with static fallback
  - right-click correction popover as the first required editor correction surface

## Implementation Details

- Added a dedicated `docs/UiParityPlan.md` document that records the agreed Windows UX contract.
- Updated the architecture document to reflect the real target shell model:
  - floating toolbar
  - expandable internal editor
  - floating suggestion overlay
  - correction/context popover
- Updated the parity matrix to distinguish between temporary functional baseline and true UI parity work.
- Updated the active plan with concrete new sprints for toolbar parity, editor parity, overlay parity, and right-click popover work.

## Files Changed

- `README.md` - linked the new UI parity document
- `docs/Architecture.md` - updated architecture baseline to reflect Windows-native shell parity direction
- `docs/ParityMatrix.md` - expanded parity targets and statuses
- `docs/Plan.md` - added concrete UI parity sprints and updated implementation order
- `docs/UiParityPlan.md` - added Windows UI parity contract
- `docs/changes/2026-04-09_wsa_dx_003_windows_ui_parity_plan.md` - this note

## Feature Flags / Settings

- No runtime feature flags added.

## Logging / Telemetry

- No telemetry changes.

## Validation

- Documentation-only sprint.
- No build or runtime validation required.

## Known Limitations / Follow-up

- The current WPF baseline still uses a normal app window and remains a temporary shell.
- `WSA-UX-001` is the next sprint needed to move the Windows app toward actual macOS-aligned shell parity.
