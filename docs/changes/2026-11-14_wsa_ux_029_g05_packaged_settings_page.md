# WSA-UX-029: reject app-list false positives and launch Settings in package context

## Summary

Verify the operator's completed UI-only run and correct a diagnostic false
positive: nonce presence in Installed apps did not prove the app details page.
Add guarded own-package Settings activation, strengthen page proof and prepare
fresh signed fixtures. G05's native setter and production write gate stay blocked.

## User-visible behavior

No normal product UI/Core/updater change. The diagnostic app-page test now opens
Settings from inside the signed LocalTest app, not external PowerShell. It never
clicks or toggles OS controls. List pages report wrong_settings_page, not a
claimed missing update control. A successful URI launch alone is not page proof.

## Implementation details

The qualification startup handles a second fixed mode before normal shell/Core/
account initialization. Actual installed test identity/publisher, run ID and
ordinary-user checks guard the constant URI. A bounded timeout and closed launch
receipt report OS acceptance only, always g05_qualified=false. Activation helper
has only two fixed modes; production and unpackaged targets are rejected.

The parent validates receipt size, identity/family/version/run, context/status,
booleans and exact fields; no external URI fallback. Details-page verification
requires a visible exact nonce label outside list ancestry, visible specification
section and expected version, with an Installed apps anchor veto. Unknown layout
or incomplete proof stays unqualified. Raw captions are never recorded. New
metadata separates target-name presence from actual page verification.

MicrosoftDocs issue 3688 reports the same external-URI/list behavior and an
in-package alternative; it is evidence for a controlled test, not a verified fix
on this VM. Keep the contradictory documentation/report explicitly linked.

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
- `/docs/changes/2026-11-14_wsa_ux_029_g05_packaged_settings_page.md`
- `/docs/qualification/WSA-UX-029_g05_hosted_window_fix.md`
- `/docs/qualification/WSA-UX-029_g05_packaged_settings_page.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/settings_qualification.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/src/WordSuggestorWindows.App/Services/WindowsUpdateQualification.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/UpdatePackageActivation.cs`
- `/tests/WordSuggestorWindows.Smoke/UpdateSettingsSmoke.cs`

## Feature flags / settings impact

Only isolated diagnostic startup/modes and scripts change. No production package
identity, capability grant, feed/default, settings schema, Core/pack or backend
protocol change. ProductionWriteQualified remains false. Hello remains deferred.

## Logging / telemetry impact

Fixed page/launch status, booleans, hashes, counts and HRESULTs only. No document
text, node captions, raw exception messages, credentials, tokens or telemetry.
Raw receipts/generated MSIXes and signing material remain ignored. Preparation
uses the existing non-exportable test key and deletes it; no production signer.

## Validation performed

- Actual 3924c93ed8ac46abbf7af2552aaec14a JSON/trust receipts: completed ordinary-user
  install, old nonce-only target=true on operator-observed Installed apps, no
  node errors; trust added and removed. Independent package/key/trust absent.
  Original receipt is preserved; its target flag is not app-details acceptance.
- build_app.ps1: PASS 0 warnings/errors.
- test_app.ps1 -SkipBuild: PASS 105.
- test_settings_app.ps1 -Mode UpdateState -SkipBuild: PASS 38; new production,
  unpackaged and malformed Settings-page activation guards.
- test_release_scripts.ps1: PASS 161; stricter list/header/spec/version proof,
  existing stale-node behavior, closed launch receipt and production guards.
- Live existing Installed apps window: correctly wrong_settings_page,
  installed_apps_list, target=false, no node errors/control count exposure.
- Four fresh signed LocalTest 654c593031fb payloads: prepared and MSIX-readback
  confirms identity and new diagnostic launch mode in the packaged assembly.
  No install/trust/used marker; key removed. Existing Core/pack bytes reused.
- NEW installed packaged-context launch/details page: NOT RUN. No G05 setter,
  full GUI or official production release qualification claimed.
- Final PowerShell syntax, diff, file-list/link and artifact/secret review.

## Known limitations / follow-up

The absence of a visible UAC prompt is not itself a failure when trust/install
receipts pass. Its visual cause is unknown; no UAC/VM policy change was made.
Prepared ea09b46c0e99 is USED and retired. Use the new signed session only.

In practice the next UI-only run must establish that the real app details page
opens from the app context. If Windows still opens the list, report that honestly
rather than weakening page proof. Only then choose how to test the Windows-owned
update control. Native 0x800706F7, HTTPS parity and off/on retention across updates
remain open. No automatic toggle, system repair, elevation or registry workaround.

## Consolidation notes for `WordSuggestorDocs`

Link Windows-local corrected evidence and research. Do not close G05 or infer a
VM fault. macOS, Core, backend and shared Docs source remain unchanged.
