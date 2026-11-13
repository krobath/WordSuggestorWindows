# WSA-UX-029: measured combined installer, dialog and GUI acceptance

## Summary

Record the user's completed combined LocalTest run after 61bba3a. The dialog
boundary and actual installed editor/Notepad GUI now pass together with update,
rollback, integrity negatives and uninstall retention. Keep two native checks open.

## User-visible behavior

No application or test behavior changes. Maintainers now see the measured result:
22/24 native checks true, all 56 installed GUI checks PASS, completed=true, exit 3.
The result is partial qualification, not a public release. No unchanged rerun is
needed now, and the used fixture is no longer advertised as fresh/reusable.

## Implementation details

Validate cfe870ce945c442eb1eec39d93a50a31's actual qualification JSON, the matching
G05 and trust-cleanup receipts, the pasted GUI output and all four fixture SHA-256
values. Document dialog_observed=true then idle=true before manual update, idle
before GUI, real Core/DA, retained data, .3 dirty rollback and installed GUI 56.
Payload-corrupt .4 registers but all 441 installed files match the trusted original;
corrupt signatures are rejected. Neither accepted corrupt source nor retrospective
proof of the old d532 foreground cause is claimed.

Update local indexes, plan/parity/acceptance/sprint status and runbook. Preserve
prior source/standalone GUI observations as historical. Mark bb5e06fb7203 USED;
future reruns require a fresh nonce and must preserve existing synthetic evidence.

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
- `/docs/changes/2026-11-13_wsa_ux_029_native_dialog_acceptance.md`
- `/docs/qualification/WSA-UX-029_slice14_installer_dialog_acceptance.md`
- `/docs/qualification/WSA-UX-029_slice14_installer_dialogs.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`

## Feature flags / settings impact

No source flags/settings/schema/identity changes. automatic_update=false because
interactive_app_installer; native G05=false with 0x800706F7 and no policy mutation.
Both remain acceptance gaps. Hello user-deferred; real sharing source-gated/off.
Full sprint/release qualification remains false. No package/public feed published.

## Logging / telemetry impact

No new logs, telemetry, upload or captured user content. Commit only fixed outcome
metadata/public fixture hashes in local docs. Raw receipts and retained synthetic
profiles remain ignored. No attachment contents, private signing key, credentials,
generated MSIX or certificate file committed.

## Validation performed

- Actual user test_msix.ps1 default combined command: completed, exit 3, 22 true /
  2 false; installed GUI 56 PASS, DPI 96 / one monitor.
- Independent structured receipt checks: exact remaining false flags, positive
  dialog/idle boundary, payload count 441 and cleanup true.
- Recomputed four MSIX SHA-256 values match release-manifest.json values.
- Native G05 receipt: packaged context true, changed=false, restored=true,
  source_and_other_settings_preserved=true, production control false, 0x800706F7.
- Independent readback: test registration absent, signing key absent, temporary
  TrustedPeople leaf absent, zero WordSuggestor processes.
- Documentation-only follow-up: no software suite rerun needed or newly claimed.
  Previous build/app105/release112 results keep their 61bba3a preparation scope.
- Links, changed-file coverage, text/secret-pattern review and git diff --check.

## Known limitations / follow-up

Do not convert interactive dialog handling into unattended automatic installation.
The original failed run did not capture foreground; this new success verifies the
new sequence without proving the exact old cause. Local file-feed/test-identity
qualification is not production HTTPS/signer evidence. Human Narrator/mixed DPI,
other supported OS/hardware, interrupted update and macOS runtime remain separate.

Next investigate G05's Windows native-policy error and the approved unattended
App Installer completion path. In practice the setting and background update must
work without silently clicking consent or suppressing a failed gate. The user has
no immediate rerun task. A subsequent native check will use a fresh isolated
fixture after a concrete change. Official beta still requires its stable trusted
signer/feed/publication and remaining human/backend acceptance; Hello stays parked.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-owned qualification rather than copy raw receipts. Combined
local editor/Notepad/install/integrity/uninstall is measured; two native checks
and external/human release acceptance remain open. No app/Core/pack/macOS/backend/
shared Docs source changes or corresponding platform commits in this follow-up.
