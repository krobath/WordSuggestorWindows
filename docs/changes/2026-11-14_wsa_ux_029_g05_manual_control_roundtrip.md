# WSA-UX-029: prepare a human-controlled Windows update-setting experiment

## Summary

Prepare the next installed diagnostic after verified Settings control discovery.
The human changes only a disposable test app's update control and restores it;
scripts reopen its verified page and compare UI/native state. Installed run NOT RUN.

## User-visible behavior

An explicit manual diagnostic prints two instructions and waits for Enter after
each human change. No automatic clicking, native setter or product feature change.
G05 remains read-only and production release qualification remains false.

## Implementation details

`-ManualSettingsUpdateRoundtrip` requires the existing prepared LocalTest,
ordinary-user, UI-only diagnostic context. `Get-SettingsUpdateToggleState` requires
the exact package-family control on verified app details and reads TogglePattern
state only. The sequence is initial/opposite/initial with page reopening and
stable-state checks. An interrupted experiment records observed restoration;
there is no hidden UI mutation to make cleanup appear successful.

Native state comparisons ignore only last-check timing; summaries omit raw URI
values and exception messages. A failed native read while off records its HRESULT
and still allows the human restoration step. Policy-source drift is reported
separately from restored UI. The existing trust/install cleanup remains in place.
The receipt mode distinguishes manual experiments from read-only UI discovery.

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
- `/docs/qualification/WSA-UX-029_g05_packaged_settings_page.md`
- `/docs/qualification/WSA-UX-029_g05_manual_control_roundtrip.md`
- `/docs/changes/2026-11-14_wsa_ux_029_g05_manual_control_roundtrip.md`

## Feature flags / settings impact

New diagnostic switch only. ProductionWriteQualified remains false. The operator
may change only the fresh disposable test app's own Windows update setting; no
global policy, product identity or normal suite behavior change.

## Logging / telemetry impact

Record fixed states, boolean policy values, bounded interval, recognized policy
source, changed property names and fixed error category/HRESULT. No raw UI
captions, URI values, user text, credentials or exception messages. No telemetry.
Generated MSIX files and public test certificates remain ignored.

## Validation performed

`test_release_scripts.ps1`: PASS 171 checks. New simulations cover two page
reopenings, honest policy-source drift, interruption without automatic UI restore,
unavailable native reads while off, guards and redaction. These are script tests,
not real installed UI roundtrip proof.

Prepared four signed test versions through existing scripts under c371a4bbd8f8.
Preparation key_removed=true; independent reads find no installed package,
signing key or machine TrustedPeople leaf, and no used marker. The local public
certificate expires 2026-11-21T00:51:07Z. Qualified Core/DA payload reused.
Final diff, local links, artifact and repo-boundary checks performed before commit.
No new app-source build/full GUI regression required for this script-only change.

## Known limitations / follow-up

The human installed off/on experiment, actual update suppression/resumption and
preference retention across N-to-N+1 remain NOT RUN. Real TogglePattern state
access remains to be tested. An unchanged visual state does not resolve the
native 0x800706F7 failure. Next run the documented command, perform the two human
changes, and review UI restoration and native deltas separately. Do not enable
the product checkbox or attribute the failure to virtualization from this work.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-local runbook and distinguish script readiness from installed
results. No macOS/Core/backend/shared Docs edits or distribution contract change.
