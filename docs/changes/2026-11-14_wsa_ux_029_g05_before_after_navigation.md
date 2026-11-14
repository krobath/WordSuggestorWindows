# WSA-UX-029: measure update state before and after Settings navigation

## Summary

The corrected human sequence still read On after the operator left the control
Off. The prior diagnostic measured only after reopening. Preserve UI and native
state before AND after packaged activation/navigation to locate that transition.

## User-visible behavior

Keep the same two explicit manual changes and Enter checkpoints. Print separate
before_reopen and after_reopen lines. A mismatch stops with both available
samples retained and a neutral error that does not attribute operator fault or
a Windows write failure. No automatic clicks, setters or global policy changes.

## Implementation details

`Get-SettingsManualPhaseEvidence` reacquires the verified package's exact control
twice 500 ms apart and attempts a native snapshot even if UI state differs or its
read fails. Record changed property names and safe summaries/fixed read errors.
Capture the first sample BEFORE invoking the packaged page launcher; preserve
bounded reopened-page evidence and then the second sample before validating the
requested state. `before_reopen` / `after_reopen` status suffixes distinguish
unexpected/unstable/read-failed phases. Unverified reopened pages cannot qualify
a step or erase the first sample. Existing step fields mirror the after sample.

## Files changed

- `/scripts/settings_qualification.ps1`
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
- `/docs/changes/2026-11-14_wsa_ux_029_g05_before_after_navigation.md`

## Feature flags / settings impact

Existing opt-in manual LocalTest diagnostic only. G05 remains read-only,
ProductionWriteQualified=false and production release gates closed. No runtime
app source, package schema, Core/language pack or backend contract change.

## Logging / telemetry impact

Redacted native summaries and changed field names, fixed states/categories/
HRESULTs and bounded page/launch metadata. No raw URI values, exception messages,
document text or credentials. No telemetry. Generated test packages/certificates
and original operator receipts stay ignored; raw receipts are not rewritten.

## Validation performed

`test_release_scripts.ps1`: PASS 175. Three new cases preserve Off/native changes
before a simulated On on reopen, preserve native/after data when the first UI
read fails, and reject an unverified reopened page without losing the first
sample. Existing off/on, native-read-failure, interruption, drift and redaction
checks pass. Simulations do not qualify installed behavior.

Run 538b428b00324ac68198a72b84ba90d7: verified details/control, clean install and
ordinary user; requested Off, reopened On; original UI/native state observed at
end. Operator confirms they left Off at Enter. Independent cleanup checks:
no package/key/machine leaf, used marker retained. Receipt hashes and evidence
limits recorded in the runbook. No successful Off checkpoint claimed.

Prepared signed LocalTest cd8326885d3a versions .1 through .4 through existing
scripts, using qualified Core/DA payloads. Read-only bundle checks pass; key
removed, unused, no installation/trust. Certificate expires 2026-11-21T16:42:21Z.
Installed two-phase diagnostic NOT RUN. No production signing/publication or new
full GUI regression. Final diff, local links, secret/binary patterns and repo
boundaries reviewed before commit; macOS/Core/backend/shared Docs unchanged.

## Known limitations / follow-up

The original native 0x800706F7 remains unresolved. Prior samples cannot determine
whether the visible flip reset before Enter, during packaged activation/navigation
or whether UI Automation disagreed. Next perform the same two human changes on
the new disposable app and compare both phase samples. Even a localized transition
does not identify a Windows component or prove actual update suppression/retention.
G05 must remain disabled until its product behavior is separately qualified.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-local corrected-sequence evidence and two-phase diagnostic.
Distinguish the earlier instruction misunderstanding from this later correctly
performed sequence. Do not infer a VM defect, success or an OS write cause.
