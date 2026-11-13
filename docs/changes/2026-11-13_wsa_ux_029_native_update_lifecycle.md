# WSA-UX-029 native update lifecycle qualification

## Summary

Native local signing/installation tests now prove automatic and actual manual
.1-to-.2 updates and retained user data. Record package-in-use rollback blockers
and improve normal test shutdown plus independent manual qualification.

## User-visible behavior

No production app/UI/default/identity change. Test-only scripts/docs change.
No force shutdown, WindowsApps ACL modification, signing bypass, publication,
backend upload or positive Hello test. Local fixture package/trust removed.

## Implementation details

Use the existing app_process exact-path/start-time toolbar close helper after
input-idle and between phases, including immediately before offline rollback
fixtures. Remove the duplicated close-every-window helper. Never confirm a save
prompt or terminate an editor. Windows still rejected .3 as in-use in completed
attempts, so the new immediate pre-fixture close is not claimed natively qualified.

SkipAutomaticUpdateProbe is an explicit LocalTest-only test mode. It omits the
shell automatic lane while enabling independent manual/rollback/UI/negative tests,
records automatic_update=false/not-run reason and cannot yield a complete native
gate. Production fixtures reject it before mutation. Default combined behavior
and all release/uploader expectations remain unchanged.

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
- `/docs/changes/2026-11-13_wsa_ux_029_native_update_lifecycle.md`
- `/docs/qualification/WSA-UX-029_slice14.md`
- `/docs/qualification/WSA-UX-029_slice14_native_followup.md`
- `/docs/qualification/WSA-UX-029_slice14_update_lifecycle.md`
- `/docs/qualification/WSA-UX-029_slice15.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`

## Feature flags / settings impact

No app flags/schema changes. G05 remains partial/read-only, A07 source-gated,
Hello user-deferred; full sprint/release acceptance false. New switch belongs
only to isolated qualification, not product settings or production distribution.

## Logging / telemetry impact

Existing synthetic native check statuses/receipt metadata and ignored attempt
records. No raw user documents, secrets/private keys or new telemetry. Prior
synthetic profiles preserved in their attempt retained-profile directories.

## Validation performed

- 8825 attempt: automatic .1-to-.2, actual installed Core after update, hashes and
  extended data readback PASS; rollback .3 rejected 0x80073D02. Manual command here
  ran after automatic .2 and was same-version, not a separate manual upgrade.
- 4220 attempt: automatic .2 observed; normal close did not finish, safely stopped.
- 2293 attempt: automatic 60s observation expired, actual manual .1-to-.2 and
  data/installed Core PASS; rollback still rejected 0x80073D02.
- db267 attempt: Windows UAC canceled, no trust or install. Read-only session
  inspection locates the test process in active RDP; visibility question pending.
- Affected release gates PASS 69 (new production-rejection test for manual mode).
- Existing normal-close/start-time/cancel safety tests PASS 14, synthetic fixture.
- Cleanup independently checked: test package/trust/private key absent.

Exact attempt IDs, command/log locations, test boundaries and manual matrix:
../qualification/WSA-UX-029_slice14_update_lifecycle.md. No product source changed;
prior source regression remains evidence for its historical snapshot.

## Known limitations / follow-up

No full native PASS or rollback/policy/installed GUI/negative-case qualification.
Automatic update was observed on real local feeds but not within every 60s window;
OS scheduling causes were not proven. All attempt records stay incomplete/false.
The latest close sequence and isolated manual lane need native rerun once UAC
visibility/readiness is confirmed. Do not infer approval from a default/elapsed time.
In practice the next run proves rollback with a higher package number and retained
unsaved drafts, then installed UI/negative cases. It does not enable G05, public
beta or sharing. Production signer/HTTPS and human/backend/Hello gates remain open.

## Consolidation notes for `WordSuggestorDocs`

Later summarize actual automatic/manual updates and retention, with explicit
rollback/timing limits and links to Windows-local evidence. Do not promote full
sprint/release or infer macOS runtime testing. Shared Docs, macOS/backend/Core
and language-pack source remain untouched.
