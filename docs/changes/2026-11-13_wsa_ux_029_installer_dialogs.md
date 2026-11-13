# WSA-UX-029: installer dialog boundaries and verified Notepad focus

## Summary

Prepare a truthful native rerun after the user's installed GUI test failed with
an App Installer update dialog still open. Isolate installer UI from later test
phases and diagnose actual foreground/text readiness before sending test input.

## User-visible behavior

Production app behavior, Core/pack contracts, Settings controls and update policy
are unchanged. The test now pauses with ACTION MSIX when Microsoft App Installer
is visible. The operator cancels the test dialog within 120 seconds; the harness
observes closure and resumes. It never clicks update/consent buttons. A run that
needs such handling cannot count as an unattended automatic-update PASS.

## Implementation details

Test-only DesktopEnvironmentProbe enumerates visible uncloaked windows and uses
native process package family identity for Microsoft.DesktopAppInstaller, including
legacy frame children. Root windows are deduplicated; no captions/content queried.
A CLI mode returns a closed metadata schema through the existing bounded process
transport. msix_desktop validates it and requires stable idle before install,
automatic launch, manual update, rollback and installed GUI, and after manual
update. Timeout records a specific blocked phase and stops before proceeding.
Initial and late dialog observations prevent a false automatic-update PASS.

GuiSmoke checks for installer windows again at startup/Notepad readiness and waits
for the expected native HWND/PID/GUI-thread focused edit target. Preserve nullable
UIA focus assistance rather than require its provider to exist immediately.
Fixed failure metadata distinguishes AppInstaller blocking, denied foreground
and unavailable edit target. Cleanup only saves after native target verification
and revalidates the owned HWND's PID before normal close; no input into another
foreground app or save prompt. No production focus/commit adapter changes.

## Files changed

- `/README.md`
- `/docs/Architecture.md`
- `/docs/FinalAcceptance.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-13_wsa_ux_029_installer_dialogs.md`
- `/docs/qualification/WSA-UX-029_slice14_installer_dialogs.md`
- `/docs/qualification/WSA-UX-029_slice14_package_integrity.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/msix_desktop.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/tests/WordSuggestorWindows.Smoke/DesktopEnvironmentProbe.cs`
- `/tests/WordSuggestorWindows.Smoke/DesktopEnvironmentSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/GuiSmoke.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`

## Feature flags / settings impact

No app flags, consents, data schemas, MSIX identity/publisher or minimum-OS change.
G05 remains read-only with its separate 0x800706F7 blocker. Hello stays user-deferred;
sharing source gate remains off. Existing Skip modes stay isolated/false in native
reports, and full sprint/release qualification remains false. No weakening of
ShowPrompt/feed policy or expectation that corrupt signing/content must be safe.

## Logging / telemetry impact

No telemetry/upload or window-title/document-content logging. New test metadata
contains schema, installer window count, foreground boolean/PID, per-phase dialog
observed/idle and fixed automatic-not-completed reason. Native focus failures print
fixed booleans/PID only. Synthetic test artifacts/public certificate metadata stay
ignored under .artifacts; private non-exportable signing key deleted at preparation.

## Validation performed

- build_app.ps1: PASS, zero warnings/errors.
- test_app.ps1 -SkipBuild: PASS 105, including seven new classification/real native
  probe checks (empty/hidden/unrelated/hosted/background, fixed output schema).
- test_release_scripts.ps1: PASS 112, including 18 new schema bounds/privacy,
  interactive-versus-unattended outcome, dialog-close and timeout checks.
- Actual probe via local .NET environment/bounded transport: PASS fixed JSON;
  no visible installer windows at measurement. Positive popup recognition still
  needs the user's installed rerun; unit dialog cases use synthetic snapshots.
- Standalone GUI: NOT PASS. Final diagnostic NotepadForegroundUnavailable,
  focus_request_accepted=false, foreground_matches=false, Explorer PID 7096,
  installer_windows=0; stopped before test input. Earlier development trials are
  retained in raw logs; no GUI assertion was weakened to get a green run.
- Fresh signed .1/.2/.3/.4 LocalTest bb5e06fb7203 prepared using unchanged pinned
  Core and existing packaging. Key removed; independently verified no signer key,
  no leaf trust and no test registration. No UAC/install/full native run here.
- Exact commands, hashes, prior d532 failure, raw log paths and manual matrix:
  /docs/qualification/WSA-UX-029_slice14_installer_dialogs.md.

## Known limitations / follow-up

The old screenshot is consistent with focus interference but did not record
foreground at failure. Do not assert that it proves the popup caused the failure.
The final standalone probe independently shows Explorer retaining focus without
an installer popup; the user's ordinary interactive PowerShell rerun is needed.
No global foreground workaround, UI suppression or automatic consent is added.
Runtime popup detection/closure, automatic update and installed GUI are not newly
qualified. G05 and hardware/human/production signing/feed gates remain open.

Next the operator runs the already prepared isolated fixture, approves temporary
leaf trust and cancels a WordSuggestor update dialog only when ACTION MSIX asks.
The test should reach the real editor/Notepad flow after installer idle and record
any failure precisely. Pending/interactive automatic update remains false. Native
cleanup must be checked even after failure; preserve used profiles and prepare a
new nonce for another attempt. No official beta publication is authorized by this.

## Consolidation notes for `WordSuggestorDocs`

Consolidate only by linking this Windows-owned evidence/runbook. macOS, Core,
backend and shared Docs were not changed; native macOS compatibility is not
attested by Windows tests. Keep prepared artifacts distinct from actual installed
qualification and retain the G05/Hello/backend/official signing acceptance gaps.
