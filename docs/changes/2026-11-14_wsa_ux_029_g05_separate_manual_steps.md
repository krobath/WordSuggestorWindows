# WSA-UX-029: separate the two manual update-control changes

## Summary

The first manual run observed On at the requested Off checkpoint. The operator
clarified that they had already switched Off and back On before the first Enter.
Record the attempt as inconclusive and make the two checkpoints explicit.

## User-visible behavior

Print step 1/2 and step 2/2, with a clear instruction to leave the control in the
requested state while pressing Enter. Do not restore the original state until
the second prompt. No automatic clicks or update-policy setters are added.

## Implementation details

The manual probe now records unexpected_state_on_reopen or unstable_state_on_reopen
instead of leaving a failed step at awaiting_manual_change. The mismatch message
does not claim that Windows rejected a write. Assign the diagnostic receipt mode
before running the probe so failures also retain the manual mode. Keep the original
operator receipt unchanged and document its earlier generic mode.

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
- `/docs/changes/2026-11-14_wsa_ux_029_g05_separate_manual_steps.md`

## Feature flags / settings impact

The explicit manual LocalTest diagnostic remains opt-in. No product flag,
production identity, update policy, Core/pack or backend contract change.
ProductionWriteQualified and release_qualified remain false.

## Logging / telemetry impact

Fixed step numbers, state/error codes and redacted metadata only. No sensitive
text, credentials, raw exception messages or telemetry. Generated signed test
packages, certificates and raw receipts stay ignored.

## Validation performed

`test_release_scripts.ps1`: PASS 172, including a regression for the first Enter
finding the original state again, with honest status/restoration and G05 false.
Original run 3dab31945b9c4756aac3123fdcb98d40: clean install/ordinary user and
details/control discovery passed. Final UI and native settings matched the
initial state. Trust-cleaned removed=true; independent package/key/machine-trust
checks found none, with used marker retained. Corrected installed Off checkpoint
and two-step roundtrip remain NOT RUN. See the qualification runbook for hashes,
operator clarification and fresh-session instructions.

Fresh signed LocalTest af00e7ebeb63 fixtures prepared through existing scripts
with qualified Core/DA payload reuse. key_removed=true, used marker absent,
no installed package, signing key or machine trust; expires 2026-11-21T16:24:41Z. No new product-source change or full GUI regression.
Final diff, local links, binary/secret patterns and repo boundaries reviewed
before commit. macOS/Core/backend/shared Docs remain clean and unchanged.

## Known limitations / follow-up

This operator-sequence mismatch does not prove that Windows cannot save Off.
Next run the fresh command, leave the disposable test app Off at the first Enter,
then restore On only at the second prompt. Review measured native deltas and
restoration before deciding on any fallback. The native 0x800706F7 failure and
actual update/preference retention remain open; no production release qualified.

## Consolidation notes for `WordSuggestorDocs`

Keep the Windows-local measured attempt and next two-checkpoint procedure linked.
Do not promote the first run to a Windows write failure or a successful roundtrip.
No shared documentation or macOS implementation edited.
