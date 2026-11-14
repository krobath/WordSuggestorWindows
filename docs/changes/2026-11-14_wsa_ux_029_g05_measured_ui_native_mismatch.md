# WSA-UX-029: record the measured Settings UI/native mismatch

## Summary

Verify installed two-phase run 373fb3ee03c04d28a168c8bd055545af. UI reads Off
twice before packaged activation/navigation and On twice afterwards. Native
configuration remains unchanged in both samples. Record evidence, not a cause.

## User-visible behavior

No product or diagnostic code change. The observation confirms the operator's
visible flip. G05 remains read-only; Off has not been verified as a saved policy
or actual update suppression. Retire the USED test session's former command.

## Implementation details

Record actual before/after UI/native values, matching details/control/launch
proof, final observed equality, hashes and cleanup. A read-only event-log check
uses explicit UTC XML bounds after rejecting an empty DateTime-filter result.
Three populated channels contain no package-associated warnings/errors and no
known RPC code in message/EventData payloads; XML Keywords are not error codes.
These logs do not establish that no error occurred elsewhere.

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
- `/docs/changes/2026-11-14_wsa_ux_029_g05_measured_ui_native_mismatch.md`

## Feature flags / settings impact

None. ProductionWriteQualified=false and release gates remain closed. No VM,
global policy, product identity, Core/pack, backend or macOS changes.

## Logging / telemetry impact

Only fixed configuration flags, safe metadata, hashes and aggregate event counts
documented. No raw event messages, other-app content, user text, credentials,
test binaries or certificates committed. Original receipts stay ignored/unaltered.

## Validation performed

Actual .1 run: clean install/ordinary user; verified app_details and exact toggle;
Off/Off before reopen, On/On after, native delta arrays empty in both phases;
final UI/native match original state. Partial/incomplete qualification remains
false. Trust removed=true and independent reads find no package/key/machine leaf;
used marker present. Raw receipt hash and package hash in the runbook.

UTC event queries: AppxPackaging 149 total / 9 package; DeploymentServer 32/25;
AppModel-Runtime 25/12. No package warnings/errors or known RPC-code payload
matches in these channels. No new build/tests required for documentation-only
changes; prior 175 script checks retain their simulation scope. Final diff,
local links, secret/binary patterns and repo-boundary review before commit.

## Known limitations / follow-up

Native flags already remained true BEFORE reopening: do not claim a measured
native Off policy was overwritten by activation. Internal disable mechanisms,
async saving, UIA provider behavior and responsible Windows components remain
unresolved. No new replacement fixture prepared in this evidence-only delivery.
Next design a bounded passive read after the human flip without package
reactivation, followed by human navigation within Windows Settings. That distinct
experiment remains NOT RUN and would not by itself qualify actual automatic
update suppression or N-to-N+1 preference retention. Do not repeat the used command.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-local measured mismatch and distinguish visual Off from saved
native policy. The separate 0x800706F7 setter failure is not correlated by these
event logs. No shared Docs/macOS/Core/backend implementation edited.
