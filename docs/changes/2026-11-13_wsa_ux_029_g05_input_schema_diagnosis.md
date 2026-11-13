# WSA-UX-029: G05 native input and feed-schema controls

## Summary

Extend the existing isolated G05 investigation with seven native input reductions,
a feed-schema comparison and a strictly read-only Windows Settings UI probe. No
production write qualification is enabled. Native execution evidence is recorded
in the associated qualification document, independently of software checks.

## User-visible behavior

G05 remains read-only. Product editor/settings/engine behavior is unchanged.
Maintainers can compare the native options and 2018/2021 schema on one fresh
LocalTest identity. The optional Settings probe opens the test app's Windows
Settings page; it does not click, toggle, reset, uninstall or terminate Settings.
An unverifiable target is explicitly unqualified, not assumed to be the test page.

## Implementation details

The native worker retains own-identity/publisher/family and ordinary-user guards.
After original-state restoration, it tries no_prompt, minimal, minimal_no_version,
update_uri, repair_uri, both_uris and no_repair. Each attempt is followed by full
policy/flag restoration verification; the first restoration failure stops testing.
Native schema v3 allows only those seven ordered cases and fixed metadata. The
parent rejects unknown content/fields and contradictory success. Existing stream
bounds and timeout remain. No successful variation alone qualifies a product toggle.

The existing test_msix flow modifies only private runtime feed copies, can update
the same test identity to the already prepared higher third package using the
other schema, and keeps all full-release flags false. New flags are rejected
outside the marked diagnostic. The UI probe verifies the nonce in the OS Settings
UI Automation tree before returning limited control metadata. No captions stored.

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
- `/docs/changes/2026-11-13_wsa_ux_029_g05_input_schema_diagnosis.md`
- `/docs/qualification/WSA-UX-029_g05_input_schema_diagnosis.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/settings_qualification.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateQualification.cs`
- `/tests/WordSuggestorWindows.Native/UpdatePolicyControl.cpp`
- `/tests/WordSuggestorWindows.Smoke/UpdateSettingsSmoke.cs`

## Feature flags / settings impact

No persistent preference, settings schema, Core/pack contract or backend protocol
change. ProductionWriteQualified remains false. Test-only flags are documented
in the local qualification runbook. No production capability or feed-default change.

## Logging / telemetry impact

Fixed case names, acceptance booleans, HRESULTs, restoration and UI verification
metadata only. No document text, window captions, credentials or URI values added.
No upload/telemetry. Signing keys and generated packages/certificates remain ignored.

## Validation performed

- Build: PASS zero warnings/errors with normal process permissions. An initial
  sandboxed build exited 1 without compiler diagnostics; no source workaround.
- C++ x64/static CRT: PASS compile.
- test_app.ps1 -SkipBuild: PASS 105.
- test_settings_app.ps1 -Mode UpdateState -SkipBuild: PASS 35.
- test_release_scripts.ps1: PASS 135.
- Actual attempt bdd73080436f4996a10b8f033c75b188 stopped at RunAs cancellation
  before trust/installation; input/schema/Settings UI native tests NOT RUN.
  All release flags false; no package, key, leaf trust or app/helper remains.
- Standalone unpackaged native guard: PASS exit 3/context=false/0x80073D54.
- A cleanup.request now covers cancellation before a process object is returned;
  the failed session has that marker. Final PowerShell syntax checks PASS.
- Prepared four signed isolated packages, key removed; no production qualification.
- No new full GUI regression or production release qualification claimed.

## Known limitations / follow-up

A native setter fix is not established by automated software checks. Investigate
only the measured remaining differences on this VM. GetDefault preferences can
be overwritten by a later package update according to Microsoft; even a working
setter still needs N-to-N+1 preference-retention qualification. HTTPS input parity
and remaining release/human/backend/Hello gates keep their separate status.

In practice the next successful investigation must make the user's automatic-update
choice work safely and survive an app update before exposing G05. Do not replace
this with a machine-wide policy, elevation workaround or unsupported registry edit.
No VM fault is inferred from RPC_X_BAD_STUB_DATA alone.

## Consolidation notes for `WordSuggestorDocs`

Link Windows-local measured evidence and research references. Do not close G05 or
claim a VM/OS cause. macOS, Core and backend source remain unchanged.
