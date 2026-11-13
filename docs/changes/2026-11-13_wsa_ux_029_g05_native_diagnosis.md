# WSA-UX-029: reproducible G05 native API diagnosis

## Summary

Implement precise native phase evidence and isolated capability/C++/input controls.
Execute four signed ordinary-user diagnostic series on the SAME VM. G05 remains
unresolved/read-only; no production write qualification or VM fault is claimed.

## User-visible behavior

No editor, Core, pack, login, consent or production update-default change. The
product G05 checkbox remains read-only. Maintainers can reproduce the failure
without running the full editor/GUI suite. Production rights remain unchanged.
The earlier Insider/unstable-VM inference is explicitly withdrawn: current
Microsoft release information lists this exact 26300.9974 / 26H2 build as GA.

## Implementation details

Native mutation/restore report fixed phase labels and HRESULTs. Options preparation
is inside failure handling. A LocalTest success now needs verified off AND on,
unchanged non-scheduling policy and restoration; legacy one-transition proof fails.
Optional schema-v1 receipt fields contain only stages/codes/bools/steps.

Existing package/prepare/test scripts gain explicit LocalTest-only capability and
native-control experiments. A matching nonce publisher is required. Marked receipts
cannot enter the default suite. Diagnostic completion forces full-release flags
false and partial exit 3. Production templates are unchanged. Four-version packaging
reuses the existing flow; only .1/.2 are installed during each diagnostic.

The optional C++/WinRT worker has no .NET dependency, verifies its OWN exact test
identity/family/publisher and ordinary-user token, then compares factory/manual
native options, original-data no-op, off/on and restoration. Later controls use
Package.Current and fresh URI objects. It cannot target production or another
package. The helper is only copied to explicit test payloads, statically linked,
and absent from normal/production packages. Parent process uses bounded JSON /
streams / timeout and kills only its owned helper on failure. Final closed protocol
v2 rejects unknown/content-bearing fields and contradictory evidence. Manual-route
restoration retains a pre-mutation options object and immutable fingerprint.

Diagnostic interval 0/6 comparison and zero-value control operate only on matching
LocalTest data and preserve an immutable original fingerprint. Mock tests cover
accepted and rejected zero writes with original policy restored, including a
rejected setter that changes scheduling flags without changing the interval. No broader OS
settings, service/registry repair, global update policy or TLS trust changes.

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
- `/docs/changes/2026-11-13_wsa_ux_029_g05_native_diagnosis.md`
- `/docs/qualification/WSA-UX-029_g05_native_diagnosis.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/build_update_policy_control.ps1`
- `/scripts/package_app.ps1`
- `/scripts/prepare_msix_test.ps1`
- `/scripts/release_common.ps1`
- `/scripts/settings_qualification.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/scripts/test_settings_platform.ps1`
- `/src/WordSuggestorWindows.App/Models/WindowsUpdateContracts.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsNativeUpdatePlatform.cs`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateQualification.cs`
- `/tests/WordSuggestorWindows.Native/UpdatePolicyControl.cpp`
- `/tests/WordSuggestorWindows.Smoke/UpdateSettingsSmoke.cs`

## Feature flags / settings impact

ProductionWriteQualified stays false. No environment/user preference bypass.
G05 remains partial, unattended update still separate, Hello user-deferred, sharing
source-gated. Full sprint/release flags false. Optional test flags cannot affect
normal production packaging, source feed timing or the standard release suite.
No settings schema, Core request/response or SQLite pack contract change.

## Logging / telemetry impact

No telemetry/upload, full exception messages, document text, window titles or
credentials added. New metadata is phase, HRESULT, actual capability booleans,
mode, step outcomes and interval. Native worker logs no URI values. Native
receipts/artifacts stay ignored; commit only source/docs and public metadata.
No private key, PFX, certificate, generated MSIX/exe/obj or machine-specific path.

## Validation performed

- .NET build: PASS zero warnings/errors; C++ control compiles with VS2022/SDK.
- test_app.ps1 -SkipBuild: PASS 105.
- test_settings_app.ps1 -Mode UpdateState: PASS 31, including negative native JSON
  bounds/privacy/contradictory-success cases.
- test_release_scripts.ps1: PASS 131, including test/production guards, two-mode
  proof requirements, interval control and restoration mocks.
  An earlier restricted execution failed the existing transport child-reaping
  assertion; rerunning with normal process permissions passed unchanged. This
  does not count as native G05 success or justify weakening that assertion.
- Real four-series native diagnostics: baseline vs declared capabilities, native
  C++ factory vs explicit object, Package.Current/fresh URIs, intervals 0 vs 6.
  All write/no-op routes report 0x800706F7. Exact phase C# write_policy; current
  C++ reports write_manual_noop. Both context/declared-capability states measured.
- Same installed final .2 / original interval 6: explicit zero setter also rejects
  with 0x800706F7 and original policy restored. No interval-only fix established.
- Native diagnostic completion is NOT a G05/production PASS. All four sessions
  independently cleaned: no package, private signing key, leaf trust or app/helper.
- Current OS/CPU/service/reboot/SDK metadata read only; no fault inferred from VM,
  pending-file marker or differing DLL file revisions. Current GA release checked
  after correcting the outdated January Insider reference.
- Exact native matrices, artifact IDs/hashes, commands and limitations are in
  /docs/qualification/WSA-UX-029_g05_native_diagnosis.md.
- No new full GUI/native-release regression claimed. Prior installed GUI 56 and
  combined 22/24 evidence keeps its original scope.

## Known limitations / follow-up

The actual G05 fix and successful off/on product path are NOT delivered. No safe
code change enabling writes is justified by these failures. Shared errors across
C#/C++/cmdlet/WinRT narrow the problem beyond .NET alone, not to a proven Windows
or virtualization defect. Local file-backed feeds remain common; equivalent HTTPS
and minimal optional-field subsets have not been tested. The final native controls
also use stricter schemas and stages than earlier receipts; retain each original
record instead of relabeling it as current proof.

Next investigate minimal policy/source inputs and the app-specific Windows Settings
control on THIS VM. In practice we must find a supported reversible way to change
WordSuggestor's update preference before enabling G05. No user should replace or
reinstall the VM based on this evidence. Another installation is optional later
cross-validation, not a present blocker. Production signer/HTTPS/public beta and
remaining human/backend acceptance still require their separate qualification.

## Consolidation notes for `WordSuggestorDocs`

Link Windows-owned results/runbook. Do not claim G05 closure or an Insider/VM fault.
The exact host build is current GA, and none of the measured variants fixed the
native setter. macOS/Core/pack/backend/shared Docs source unchanged; no parallel
backend protocol, engine fork or corresponding platform commit in this delivery.
