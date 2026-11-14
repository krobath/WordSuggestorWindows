# WSA-UX-029: observe Settings without package reactivation

## Summary

Implement the next distinct diagnostic after the measured UI/native mismatch:
four passive samples after the human flip, followed by human navigation inside
Windows Settings and optional manual restoration. No diagnostic reactivation.

## User-visible behavior

Three numbered prompts: change the uniquely named test app's control; leave
Settings untouched during four scheduled samples; manually navigate Back to
Installed apps and the SAME app's Advanced options; finally verify/restore the
starting state. The observer never clicks, navigates, launches or changes policy.
The harness uses the guarded packaged launcher once before the first prompt.

## Implementation details

`-PassiveSettingsUpdateObservation` requires the existing prepared LocalTest,
ordinary-user UI-only diagnostic context and excludes the manual roundtrip.
`Invoke-SettingsPassiveUpdateObservation` has no runner/URI argument. It reuses
the guarded exact-control/native phase reader, schedules four samples at 0/5/10/15
seconds, records actual elapsed time and separately reads the manually reopened
page. The waiting schedule is bounded; OS read duration is not hard-timed out.

Record whether known UI states changed, whether the first requested state was
observed, read completeness and native delta evidence. If all native reads fail,
native-change evidence is null rather than false. A wrong manual page fails
closed while preserving samples. Final UI/native equality is reported separately;
the observer never restores settings itself. G05/release qualification stay false.

## Files changed

- `/scripts/settings_qualification.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/README.md`
- `/docs/README.md`
- `/docs/Plan.md`
- `/docs/Architecture.md`
- `/docs/ParityMatrix.md`
- `/docs/UiParityPlan.md`
- `/docs/ManualSmoke.md`
- `/docs/Release.md`
- `/docs/FinalAcceptance.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/docs/qualification/WSA-UX-029_g05_manual_control_roundtrip.md`
- `/docs/qualification/WSA-UX-029_g05_passive_observation.md`
- `/docs/changes/2026-11-14_wsa_ux_029_g05_passive_observation.md`

## Feature flags / settings impact

New explicit LocalTest diagnostic switch, not a product feature. No native setter,
production identity, global policy, Core/pack contract or backend change. Existing
UI-only and two-phase diagnostic modes keep their separate scope/guards.

## Logging / telemetry impact

Fixed states, time offsets, actual elapsed milliseconds, safe native summaries,
changed property names and fixed exception category/HRESULT only. No URI values,
raw exception messages, document content, credentials or telemetry. Prepared MSIX,
test certificates and raw receipts remain ignored.

## Validation performed

`test_release_scripts.ps1`: PASS 188. New guards cover isolated context, production,
elevation, unverified initial page and scheduling bounds. Eight simulations cover
unchanged native state, timed UI reset, source drift, all native reads failing,
unavailable UI, wrong manually reopened page, interruption and starting Off.
Any observer call to activation/navigation helpers throws in those simulations.
Timing is mocked; no real installed passive run or actual update behavior claimed.

Fresh signed four-version fixtures prepared through existing packaging scripts
using qualified Core/DA payloads. Verified LocalTest f970146e46d9 is unused,
bundle checks pass; certificate expires 2026-11-21T21:05:22Z. See the runbook. No installation or trust during preparation;
test key removed. No runtime app-source change/new full GUI regression required.
Final diff, local links, secret/binary patterns and repo boundaries reviewed
before commit. macOS/Core/backend/shared Docs remain unchanged.

## Known limitations / follow-up

Installed passive observation remains NOT RUN. Next the operator performs the
three printed checkpoints on only the fresh test app. A reset without activation
would focus investigation on elapsed-time behavior; a reset after manual Settings
navigation would focus on refresh/navigation. Either result still needs native
interpretation and does not resolve the setter's 0x800706F7 or qualify actual
update suppression, N-to-N+1 retention or production signing/publication.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-local passive runbook and retain the measured predecessor's
scope. Distinguish READY scripts/simulations from the NOT RUN installed experiment.
No shared Docs, macOS, Core or backend implementation edited.
