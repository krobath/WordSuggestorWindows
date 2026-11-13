# WSA-UX-029 slice 12 qualification - 2026-11-13

Core ebd2424384649128a967867cf1ed3189de937d8c. WPF retained; backend/macOS/Core
library/Package.swift/pack unchanged. See ../AutomaticUnknownWords.md.

Core build PASS; six DA fixtures + 653 executable contracts PASS. Full Settings
All run passes, including automatic runtime tests with real Core and physical
editor/Notepad input. Final targeted build, automatic, app and native GUI results
are recorded below after the closing validation. Native Hello was not run.

## Manual / platform matrix

| Check | Evidence |
|---|---|
| Core-owned range / known / clipped token / URL-email exclusions | PASS real CLI |
| Default off / old manual consent / automatic grant and revoke | Software and native WPF/UIA; human wording acceptance not run |
| Editor boundary -> Core -> local store | PASS physical key, deterministic dictionary fixture |
| Notepad boundary -> Core -> local store | PASS physical key, exact native target |
| Document/caret/focus and manual user words preserved | PASS native owned test files |
| Rate limit, deduplication, stale scope and revocation | PASS controlled service tests |
| Native Windows Hello | USER-DEFERRED |
| Mixed DPI / multiple monitors / human Narrator | NOT RUN here; slice 15 records available environment |
| Signed install/N-to-N+1/production policy control | Not attested here; slice 14 |
| macOS runtime | NOT RUN on Windows; shared library unchanged |
| Real unknown-word backend upload | Not enabled; macOS agent owns receiver |

Initial physical input test exposed WPF's Space handling outside PreviewTextInput;
verified Space/Enter key receipts were added. No expectation was weakened. Full
All passed before a final stronger native-foreground guard and expanded consent
fixtures; affected automatic tests plus app/GUI are rerun on final code.

Next: separately consented sharing client and backend handoff, then installer and
final acceptance. All four closing slices are defined in the local sprint plan;
this slice alone does not close the sprint or qualify an official release.

## Final executed commands

From WordSuggestorWindows, powershell -ExecutionPolicy Bypass -File:

- scripts/build_app.ps1 -ArtifactsPath .artifacts/slice12: PASS 0 warnings/errors.
- scripts/test_core_cli.ps1 -SkipBootstrap -SettingsContract: PASS six DA fixtures / 653 checks.
- scripts/test_settings_app.ps1 -Mode All -SkipBuild -RunnerPath <slice12 runner>: PASS 14 suites.
- scripts/test_settings_app.ps1 -Mode AutomaticWords ...: final PASS 24 checks.
- scripts/test_app.ps1 -SkipBuild -RunnerPath <slice12 runner>: PASS 98 checks.
- scripts/test_gui.ps1 -SkipBuild -RunnerPath <slice12 runner> -AppPath <slice12 app> -FrameworkDependentApp: PASS 54 checks, 96 DPI / one monitor.
- scripts/test_settings_qualification.ps1: PASS 41 checks.
- Earlier unchanged Windows Core adapter vs new CLI: PASS 58 checks.

Ignored logs: .artifacts/slice12-*.log. Fresh source-only whole-product acceptance
will be repeated in closing slice 15; this slice does not claim a clean-checkout
installer, positive Hello or a macOS runtime test. No signing/trust/feed changed.
