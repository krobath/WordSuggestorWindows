# WSA-UX-029: audit public update APIs and preserve unknown pause state

## Summary

Implement and run metadata-only API diagnostics; distinguish missing, null,
active, expired and unknown pause values in safe update snapshots. Document the
separate CSP Disable contract without treating it as a measured device value.

## User-visible behavior

New explicit -UpdatePolicyApiAudit mode completes without GUI, Hello, package
query/activation, installation, trust or policy writes. Future safe diagnostic
snapshots label pause evidence accurately. No product toggle or release gate enabled.

## Implementation details

Get-SettingsUpdateApiAudit checks a fixed set of property/method names through
Windows ApiInformation and fixed cmdlet parameters/output-model metadata. Output
shape unavailable is separate from an absent property. Conflicting package,
runner or window modes are rejected before those probes run. Failures emit only
fixed category/HRESULT and remain unqualified.

Get-SettingsPauseSummary accepts null or recognized typed dates, classifies them
relative to the current time and emits no raw date. Missing and ambiguous values
remain not_reported/unknown. Existing native comparison and mutation guards stay
strict. Live SDK comparison found matching UUID/declarations for five relevant
interfaces in 22621/26100; this is not a native runtime qualification.

## Files changed

- `/scripts/settings_qualification.ps1`
- `/scripts/test_settings_platform.ps1`
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
- `/docs/qualification/WSA-UX-029_g05_passive_observation.md`
- `/docs/qualification/WSA-UX-029_g05_support_dossier.md`
- `/docs/qualification/WSA-UX-029_g05_api_semantics.md`
- `/docs/changes/2026-11-14_wsa_ux_029_g05_api_semantics.md`

## Feature flags / settings impact

New metadata-only diagnostic switch; no normal runtime setting changes. G05 and
ProductionWriteQualified remain false. No GetForSystem/Pause/Clear call, global
policy write, Core/pack, backend or macOS contract change.

## Logging / telemetry impact

Only fixed metadata names/booleans, pause state tags and bounded errors. Pause
dates, URI values, arbitrary strings, credentials and user content are omitted.
No telemetry or raw event trace. Original installed receipts remain unmodified.

## Validation performed

test_release_scripts.ps1: PASS 199, including seven pause classification/redaction
cases, live metadata-only audit/output-shape reporting and conflicting mode guards.
The prior 188 checks retain their scope. Actual metadata-only run returned
api_metadata_only/read_only=true/g05_qualified=false without native package calls
or user interaction. The local getter has PackageFamilyName and a nullable
PausedUntil output property, unlike the PackageFullName shown by current web docs.

Research uses Microsoft API/CSP/cmdlet documentation and local SDK metadata.
Specific Disable/Disabled/IsDisabled properties are absent from the two audited
public types on this OS; this does not inventory all internal disable mechanisms.
No installed write or GUI test repeated. No build needed for script-only changes.
Final diff, local links, exact inventory, secret/binary patterns and repo boundaries
reviewed before commit; macOS/Core/backend/shared Docs remain unchanged.

## Known limitations / follow-up

Metadata presence is not permission, a saved policy or root cause. Actual CSP
Disable value, historical initial pause values, HTTPS source equivalence and the
save failure remain unmeasured/unresolved. Next isolate a new save/source variable
or capture a specifically scoped save failure. Do not repeat the same GUI sequence,
silently elevate to system-priority APIs, or enable G05 from metadata alone.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-local semantics audit and its documentation discrepancy. Keep
the completed passive observation distinct from a proven effective update policy.
No shared docs, macOS, Core or backend implementation edited.
