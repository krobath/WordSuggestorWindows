# WSA-UX-029 native retention fixture: execute the verified extracted payload

## Summary

The user-approved native rerun passed installation and the corrected installed
Core receipt gate. Correct the next test setup failure without changing the
protected WindowsApps ACLs or production app.

## User-visible behavior

No app/UI/default/identity or language behavior changes. Only test orchestration
and evidence change. Test package/trust were cleaned up. No public publication,
real account sharing or Hello prompt.

## Implementation details

An unpackaged synthetic-data runner cannot start the Core executable directly
inside WindowsApps (observed Win32 error 5). Get-RetentionPayload unpacks each
already signature/hash-verified MSIX with existing MakeAppx into the run's ignored
folder. It requires the extracted Core executable and bundled pack hashes to match
the actual installed files, then uses that copy for seed/readback. Core/DA/user
word still require actual installed app receipts. No execution ACL/identity bypass
or product fallback. Extracted learned-overlay readback is documented separately
from installed UI proof. Native execution of the new hash check awaits a rerun.

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
- `/docs/changes/2026-11-13_wsa_ux_029_retention_fixture_payload.md`
- `/docs/qualification/WSA-UX-029_slice14.md`
- `/docs/qualification/WSA-UX-029_slice14_native_followup.md`
- `/docs/qualification/WSA-UX-029_slice15.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_msix.ps1`

## Feature flags / settings impact

None. G05 remains read-only, A07 unavailable, Hello user-deferred, full sprint and
release acceptance false. No package re-sign/rebuild; existing final fixture bytes.

## Logging / telemetry impact

Fixed Core/pack hash-match status, synthetic test data only. No raw lexical data,
credentials or private key. Generated extracted payload/profile/evidence ignored.
Previous synthetic failed-run profile preserved, ordinary user profile untouched.

## Validation performed

- Native attempt 3076da95dba94228970ac531d5e08f3b: trusted signing, ordinary-user
  clean install and installed Core/DA/user-word receipt PASS; fixture seed FAIL
  on protected-path execution. Overall not qualified, package/trust cleanup PASS.
- Corrected script: existing release tools PASS 68 including AST syntax gate.
- Independent real MakeAppx extraction of signed .2 MSIX: Core hash matches
  build-info, real Core/store seed/check/dirty/check-dirty PASS.
- Next attempt da237d1c8fb64cacb92b8e71f2515ddf: Windows reports UAC canceled,
  no certificate added/package installed. Full installed rerun remains pending.

Exact logs, attempt records and resume command are in
../qualification/WSA-UX-029_slice14_native_followup.md. No Core/library/pack,
backend or macOS source edits. Full software regression from slice15 remains
historical; this change ran the affected release/fixture gates only.

## Known limitations / follow-up

Await the user's pending new-UAC/park choice. The next native run must still prove
N to N+1, automatic/manual updates, hash/readback retention, higher-version rollback,
native policy and negative cases. No interrupted/blocked run counts as PASS.
In practice these checks establish that installed users retain their work and
local words while updating. Production signing/public release and backend sharing
remain disabled/unqualified, with their separate external/human requirements.

## Consolidation notes for `WordSuggestorDocs`

Later link Windows local native evidence, preserving failure history and the
separation of installed proof from extracted fixture readback. Shared Docs,
macOS/backend/Core sources remain untouched and macOS runtime is not attested.
