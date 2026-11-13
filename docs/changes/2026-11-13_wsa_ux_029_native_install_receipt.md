# WSA-UX-029 native installation follow-up: validate the actual app receipt

## Summary

Run the user-approved temporary-trust test; retain the observed clean-install and
Core evidence, correct the activation/result handoff check and keep update gates
open pending a native rerun after the second UAC was canceled.

## User-visible behavior

No production app behavior, UI, data schema, publisher or update-default change.
Only qualification tooling/docs change. A local test install succeeded and was
removed; the temporary non-CA leaf trust was also removed. No official beta or
public feed was published. Windows Hello remains parked.

## Implementation details

First native run used the final signed LocalTest .1/.2/.3 series. Trusted SignTool /
Authenticode/publisher checks and ordinary-user .1 installation passed. The
installed Core produced its positive exact-identity/version result, but the old
harness rejected the process/result handoff before completing its probe gate.
Its precise race/process status was not captured; do not claim the entire native
flow passed. Second diagnostic launch was canceled in UAC without trust/install.

Program's test-only --activate-app now dispatches COM activation without fetching
a short-lived external process exit status. test_msix requires a fresh reserved
GUID receipt, bounded wait and strict exact schema/identity/version/positive Core
result with no error. Wait-ReleaseProbeResult bounds output to 16 KiB and rejects
unknown/content-bearing fields. Fixed category/HRESULT/probe exit/presence metadata
helps diagnose future failures. Activation/PID alone is never sufficient.
Existing package/trust safety guards, updater/publication gates and cleanup remain.

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
- `/docs/changes/2026-11-13_wsa_ux_029_native_install_receipt.md`
- `/docs/qualification/WSA-UX-029_slice14.md`
- `/docs/qualification/WSA-UX-029_slice14_native_followup.md`
- `/docs/qualification/WSA-UX-029_slice15.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/release_common.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`

## Feature flags / settings impact

None. A07 source gate false, G05 read-only/partial, sprint/release unqualified.
No positive native Hello test or account/backend action.

## Logging / telemetry impact

Fixed activation PID/exception category/HRESULT and Boolean probe state only.
No raw app output, documents, account credentials/private keys. Local fixture
paths and raw evidence remain ignored. Synthetic first-run data preserved in that
attempt's retained-profile; ordinary user data untouched.

## Validation performed

- Native first run: trusted signing and clean per-user install PASS; actual Core
  positive schema1 receipt for exact LocalTest .1. Harness overall FAIL, update
  and rollback NOT RUN. Package/trust cleanup independently verified.
- Native second run: UAC canceled, no trust/install; no positive qualification.
- Build PASS zero warnings/errors; release checks PASS 68 (56 existing +12 new),
  app regression PASS 98, Settings contract/safety PASS 41.
- Receipt tests reject identity/version/schema/error/content/size/path failures,
  malformed data and missing-result timeout; real child delayed receipt accepted.

See ../qualification/WSA-UX-029_slice14_native_followup.md for exact attempt IDs,
raw-log locations, manual matrix and resume command.

## Known limitations / follow-up

Await the pending user choice before another UAC attempt. The new receipt path
still needs native rerun; N to N+1, automatic update, policy mutation, rollback and
installed retention/uninstall sequence remain unqualified. First-run uninstall
and removal of trust completed, but are not full update retention acceptance.
Production signer, real sharing receiver and human mixed-DPI/screen-reader gates
remain external. In practice the next native run proves whether installed users
keep settings/words/documents across beta updates; it enables no public release
or sharing by itself.

## Consolidation notes for `WordSuggestorDocs`

Later summarize Windows-only native evidence and remaining acceptance. Preserve
initial failures and cleanup evidence. No macOS/backend/shared Docs/Core/pack edits;
these Windows checks do not attest macOS runtime or public Sparkle behavior.
