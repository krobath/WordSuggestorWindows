# WSA-UX-029: measured input/schema result and precise Settings UI diagnosis

## Summary

Verify the operator's completed native run against its JSON: all 21 reduced input
attempts remain blocked at 0x800706F7, across capability and 2018/2021 schema
variants. Record successful cleanup. Improve the still-unqualified read-only UI
probe and prepare a fresh, uniquely named UI-only test without repeating setters.

## User-visible behavior

Production G05 remains read-only. No editor, suggestions, account or updater
behavior change. Only explicit native-worker LocalTest packages display their
nonce, so maintainers can identify them in Windows Settings. Product labels and
identity stay unchanged. No automatic system UI clicking is introduced.

## Implementation details

The guarded display-name helper requires a matching LocalTest identity/publisher
and explicit test flag; packaging invokes it only with the diagnostic worker.
UI errors now preserve fixed phase/category/HRESULT and per-node error counts.
One stale provider node does not discard other controls. An absent target nonce
still suppresses control IDs/counts and any verified-control claim. Captions and
exception messages never enter the diagnostic output.

The existing installer harness adds UpdatePolicySettingsUiOnly, restricted to the
marked diagnostic plus read-only UI probe, incompatible with schema comparison.
It installs N, reads the page, then uses existing uninstall/trust cleanup. It never
runs native setter/Core/editor checks or marks a full release qualified. Final
console UI metadata includes phase, HRESULT and node error count.

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
- `/docs/changes/2026-11-14_wsa_ux_029_g05_measured_matrix_ui_probe.md`
- `/docs/qualification/WSA-UX-029_g05_input_schema_acceptance.md`
- `/docs/qualification/WSA-UX-029_g05_input_schema_diagnosis.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/package_app.ps1`
- `/scripts/release_common.ps1`
- `/scripts/settings_qualification.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`

## Feature flags / settings impact

ProductionWriteQualified remains false. Production capability declarations,
manifest identity/display defaults, HTTPS feed and O-052 backend contract unchanged.
No Core request/response, language pack or persistent settings format change.

## Logging / telemetry impact

No telemetry/upload, user document text, captions, exception messages or credentials
added. Commit only fixed result metadata/hashes. Raw receipts, generated binaries,
MSIXes, certificates and private keys remain ignored. The preparation key was
non-exportable and deleted. No production signing identity was provisioned.

## Validation performed

- Actual native run 97decc4ecc3744129e814c008aae924c: completed in ordinary-user
  context with clean install. 21/21 input cases rejected; original policy restored.
  Both native references and zero-interval control also remain blocked/unchanged.
  Runtime namespace readback verifies 2018 versus 2021, and all interval reads=6.
- Actual trust-cleaned receipt and independent package/key/leaf/process checks pass.
  G05 remains FALSE; used session b14042ba01ae must not be rerun.
- test_release_scripts.ps1: PASS 144, including production/context boundaries,
  nonce labels without identity mutation, stale node resilience, target gating,
  no private UI text, underlying HRESULT and UI-only context.
- Empty UI summary and nonce argument binding: PASS in a Bypass child. An initial
  direct dot-source was refused by execution policy and is not counted as PASS.
- Fresh four-version signed LocalTest preparation bc06ec5df05e: PASS. All four
  actual MSIX manifests verify unique diagnostic labels and unchanged identities.
  No install/trust, key removed; test certificate expires 2026-11-21.
- Fixture preparation publishes self-contained x64 app payloads. New installed
  UI-only probe NOT RUN; no separate app regression/C++ rebuild, full GUI,
  successful setter or official production qualification claimed.
- Git diff/links/file-list and generated artifact/secret review before commit.

## Known limitations / follow-up

The original UI failure did not retain its exception, so its cause is not proven.
Its false toggle field does not prove the control is absent. A later read-only
inspection with no Settings window is not retroactive evidence for that failure.
The next UI-only run must identify the correct test app page and either expose
its control or report the precise read failure. No new agent UAC was attempted
because that dialog was not visible through the agent execution path.

In product terms, the user's automatic-update choice still cannot be enabled
safely. After identifying Windows' own control, measure its behavior; HTTPS source
parity and off/on preference retention across N-to-N+1 remain untested. Do not infer
a VM fault, replace the VM, grant production privileges or edit system policy.

## Consolidation notes for `WordSuggestorDocs`

Link the local measured acceptance and retire the unused-session status. No macOS,
Core, backend or shared Docs source changes. Do not close G05 or the full sprint.
