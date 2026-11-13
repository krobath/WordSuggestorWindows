# WSA-UX-029 native rollback and genuine negative deployment fixtures

## Summary

Native testing now proves higher-version rollback, dirty-draft preservation,
installed GUI/Notepad and rejection of genuinely newer invalid feeds. Correct the
test setup/PowerShell error handling and preserve incomplete acceptance explicitly.

## User-visible behavior

No production app, settings schema, UI, Core/library/pack, backend or macOS source
change. Test-only orchestration/docs. No public upload, production signer claim,
WindowsApps ACL change or positive Hello prompt. Test registration/leaf trust cleaned.

## Implementation details

prepare_msix_test emits .1/.2/.3 plus rejection-only .4 using one ephemeral
non-exportable test key, deleted in finally. test_msix requires a signed negative
candidate strictly newer than installed positive fixtures and matching identity /
publisher. Old three-package receipts fail before trust/install; explicit bundles
also require the extra candidate. Each negative feed has a fresh self-URI and .4
payload; same-version no-ops cannot masquerade as native rejection. Tamper also
uses .4. Existing bounded process wrapper handles expected verifier stderr/nonzero
status instead of PowerShell 5.1 turning it into premature termination.

Explicit SkipInstalledGuiProbe is LocalTest-only and records desktop/relaunch
false plus a not-run reason. SkipAutomaticUpdateProbe remains explicit/false.
Neither switch can qualify a complete native/production release; default tests
and rejection assertions are retained. No failing GUI assertion/timeout weakened.

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
- `/docs/changes/2026-11-13_wsa_ux_029_native_rollback_and_negative_fixtures.md`
- `/docs/qualification/WSA-UX-029_slice14.md`
- `/docs/qualification/WSA-UX-029_slice14_native_acceptance.md`
- `/docs/qualification/WSA-UX-029_slice14_native_followup.md`
- `/docs/qualification/WSA-UX-029_slice14_update_lifecycle.md`
- `/docs/qualification/WSA-UX-029_slice15.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/prepare_msix_test.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`

## Feature flags / settings impact

No product changes. Native G05 write/read/restore blocked 0x800706F7, changed=false,
restored=true, source/other settings unchanged. G05 read-only, A07 unavailable,
Hello user-deferred, full sprint/release false. No automatic ranking/upload changes.

## Logging / telemetry impact

Existing fixed check/identity/version/hash/HRESULT metadata and synthetic data.
No raw customer documents, credentials/private key, telemetry/upload. Ignored
package/profile/attempt outputs; previous synthetic profiles preserved separately.
User says UAC was visible/approved; later OS cancellation reports are preserved
without attributing them to the user or assuming an RDP cause.

## Validation performed

- Real native same-source .2-to-higher-.3 rollback and dirty draft hash/readback PASS.
- Actual installed GUI/Notepad PASS 55 in cfcf2121 and 833d5d6d attempts.
- Four-package 292715dbca6b series: newer wrong identity/publisher/offline/invalid
  feeds rejected by Windows; installed .3 unchanged.
- SignTool actual corrupted .4 rejects 0x80096010; corrected bounded wrapper also
  independently verifies that bad-hash result without PowerShell stderr stop.
- Self-contained four-package builds/signing PASS, private key removed.
- Existing release gates PASS 73, adding missing/old-negative fixture guards,
  expected verifier stderr behavior and production rejection of GUI skip.
- Subsequent GUI repeat timed out at known second-tab mouse selection; preserved
  failure. Remaining-check UAC reported cancellation before trust/install.
- Cleanup independently verified: no test registration/leaf trust/private key.

Exact package hashes, attempt IDs, software/native log locations, manual matrix
and single remaining-check command are in
../qualification/WSA-UX-029_slice14_native_acceptance.md. Previous automatic/manual
.2 evidence remains historical and is not converted into an all-passing run.

## Known limitations / follow-up

Native install rejection of corrupt .4, older/same-version and dedicated uninstall
stage still need the final operator command. Automatic/GUIs skipped in that facet
must stay false; G05 remains blocked. Existing successful cleanup removals are
not relabeled as the dedicated uninstall test. Native GUI timing and automatic
observation variability remain documented. Public signing/HTTPS, real backend,
human mixed-DPI/Narrator/audio and user-deferred Hello are separate open gates.

The remaining command opens one UAC in the Windows RDP session and runs the
isolated checks; no new permission is inferred from time/default options. In
practice it proves invalid/old package refusal and preservation when uninstalling.
All local code and fixtures are ready for that last external interactive action.
No unsafe bypass or official public release is introduced.

## Consolidation notes for `WordSuggestorDocs`

Later consolidate Windows-local native rollback/installed GUI/feed evidence with
remaining limits and failure history. Keep source-of-truth instructions here.
Shared Docs/macOS/backend/Core/pack unchanged; no macOS runtime/Sparkle attestation.
