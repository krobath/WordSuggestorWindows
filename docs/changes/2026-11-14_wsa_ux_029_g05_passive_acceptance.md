# WSA-UX-029: record completed passive Settings observation

## Summary

Verify operator run c1bb06333862449ca783ba299ccae40d. Four passive samples read
Off/Off over 15.74 seconds without observer package reactivation. Human reopening
of Advanced options reads On/On. Native configuration remains unchanged throughout.

## User-visible behavior

The diagnostic completed correctly; the partial-qualification warning is expected.
G05 remains read-only. A visible Off state has not been qualified as an effective
saved disabled-update policy. Retire the used command rather than repeat this run.

## Implementation details

Evidence-only delivery. Record actual offsets/timing, UI/native mismatch, hashes,
observed restoration and cleanup. Keep the unobserved gap before manual reopening
explicit: the evidence does not prove navigation caused the transition or exclude
a longer timer. Create a local technical support dossier for the native setter
failure and the separately bounded Settings symptom; no external report is sent.
Read component versions and narrowly scoped, redacted UTC event metadata only.

## Files changed

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
- `/docs/qualification/WSA-UX-029_g05_support_dossier.md`
- `/docs/changes/2026-11-14_wsa_ux_029_g05_passive_acceptance.md`

## Feature flags / settings impact

None. No setter, privilege escalation, policy override or registry write. Full
release qualification and G05 remain false. No app/Core/pack/backend change.

## Logging / telemetry impact

Only safe metadata, flags, public hashes, component versions and aggregate event
counts documented. No raw event exports, captions, credentials, other-app content
or user text. Raw qualification receipts remain ignored and unaltered. No telemetry.

## Validation performed

Actual installed .1 run: clean install/ordinary user, four Off/Off samples at
784/5759/10746/15740 ms, manual navigation On/On, final On/On; four successful
native reads with no changes, final native equality. status=observation_completed,
completed=true, ui_state_restored=true, native_snapshot_restored=true; G05/full
release remain false. Trust removed=true. Independent checks: no package/key/
machine leaf, used marker retained. Hashes are recorded in the runbook.

UTC events: AppxPackaging 145 total/5 package; DeploymentServer 34/23;
AppModel-Runtime 15/8. No package warnings/errors or known RPC-code payload
matches in those channels. Versions read: Windows 26H2/26300.9974 and App Installer
1.29.380.0, plus four Windows file versions in the dossier. These observations
do not establish root cause, corruption or absence of other-provider errors.
Scoped primary-source research found no exact authoritative match for the linked
symptoms; priority APIs do not justify an untested admin workaround.

No new build/test required for documentation-only changes. Prior 188 script
checks retain their simulation scope. Final diff, local links, inventory,
secret/binary patterns and repo boundaries reviewed before commit.

## Known limitations / follow-up

Do not repeat the completed GUI sequence. Next resolve save/read-priority and
disabled-state semantics, source-type equivalence or an explicitly scoped save-call
trace/reproducer. The dossier gives maintainers the existing isolated native
sources and evidence limits. No broad trace or external publication was performed.
Native 0x800706F7, effective update suppression and N-to-N+1 retention remain open.

## Consolidation notes for `WordSuggestorDocs`

Link completed Windows-local acceptance and local support dossier. Distinguish
successful diagnostic completion from a saved policy/G05 release PASS. macOS,
Core, backend and shared Docs implementation remain unchanged.
