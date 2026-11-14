# WSA-UX-029: correct zero-handle and hosted Settings window discovery

## Summary

Reproduce and correct the Settings probe's window/0x80070057 failure. Windows
PowerShell treats IntPtr.Zero as truthy; the old guard passed it to FromHandle.
On this VM Settings is hosted by ApplicationFrameHost, rather than owning its
main top-level handle. Correct binding is verified against the real desktop.
The separate G05 native setter 0x800706F7 is NOT fixed or qualified.

## User-visible behavior

Only qualification scripts change. Product editor/settings/Core/updater behavior
stays unchanged. A new read-only package-free window probe can verify the OS
window binding without UAC or installation. No automatic UI click is added.

## Implementation details

Find UIA top-level windows owned by verified Windows executable paths. Explicitly
reject IntPtr.Zero. Accept direct Settings ownership or a trusted frame host with
an actual Settings-owned descendant. Use the returned AutomationElement instead
of Process.MainWindowHandle/FromHandle. Keep nonce gating for app-page controls.
Record the direct/hosted route. Generic window inspection always reports no app
page/G05 qualification and rejects package-qualification arguments.

Prepared sessions now atomically create a used-session marker before installation,
after trust verification; markers reject reuse before bundle/trust processing.
This closes a freshness gap when UI-only profiles remain empty. Two previously
verified installed identities were retired with ignored local markers; nothing
was deleted or overwritten. A cancellation before trust still permits an unused
prepared identity. All ordinary-user and temporary leaf-trust gates remain.

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
- `/docs/changes/2026-11-14_wsa_ux_029_g05_hosted_window_fix.md`
- `/docs/qualification/WSA-UX-029_g05_hosted_window_fix.md`
- `/docs/qualification/WSA-UX-029_g05_input_schema_acceptance.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/settings_qualification.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/scripts/test_settings_platform.ps1`

## Feature flags / settings impact

No persistent setting, engine/pack/backend contract or production manifest change.
ProductionWriteQualified stays false. Test SettingsWindowProbe is read-only and
cannot qualify G05; existing UI-only installation retains all release gates.

## Logging / telemetry impact

Fixed window route/count, HRESULT/phase and qualification booleans only. No window
captions, app list names, document text, exception messages or credentials added.
Raw local receipts, generated MSIXes/certificates/keys stay ignored. No telemetry.

## Validation performed

- Operator UI-only run 6c87aa66a76a4b829e8b9d8eee064ab4: verified JSON reports
  window/ArgumentException/0x80070057 before node reading; cleanup confirmed.
- Actual PowerShell zero-handle truth conversion and FromHandle(0) failure
  reproduced on this VM. No inference from error text alone.
- Actual ordinary-user Windows desktop: hosted Settings window with nonzero
  handle and Settings process child. SettingsWindowProbe exits 0 with 1 hosted
  window; page_target_verified/g05_qualified remain false.
- Actual read of that window: 769 nodes, 0 errors, no HRESULT; absent nonce stays
  unqualified. No UAC/install/toggle, only a general Apps Settings page opened.
- test_release_scripts.ps1: PASS 151, including zero/direct/hosted/unrelated owner
  cases, no package qualification from binding mode and used-session rejection.
- Fresh signed four-version LocalTest ea09b46c0e99 prepared using existing Core/
  pack and packaging scripts. Manifest identity/publisher/labels read back; key
  deleted, no trust/install, used marker absent. Test certificate expires Nov 21.
- New installed app-page/control integration still NOT RUN. No full GUI or G05
  setter success claimed. Fixture preparation republishes self-contained payloads;
  no separate application regression or C++ rebuild was needed for script changes.
- Diff/link/file-list/secret/artifact review before commit. Other repos unchanged.

## Known limitations / follow-up

The corrected diagnostic binding does not resolve the updater's separate RPC
failure. The next fresh UI-only run must identify the real test app page and
whether Windows exposes its update control. No automatic toggle exists. Then
measure that control and HTTPS source behavior; preserve the user's off/on choice
through N-to-N+1 before enabling G05. No VM repair/elevation/registry workaround.
Previously used bc06ec5df05e and b14042ba01ae must not be rerun. Local prepared
artifacts are machine-specific and never production identities/releases.

## Consolidation notes for `WordSuggestorDocs`

Link the Windows-local reproduced bug and live proof. Do not call G05 resolved or
close the full sprint. No macOS, Core, backend or shared Docs source change.
