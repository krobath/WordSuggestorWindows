# WSA-UX-029: reject Installed apps false positives and launch from package

Date: 2026-11-14. Base 7751849, initially clean. Windows-only diagnostic change;
no production update-policy gate, Core/pack/backend or macOS behavior change.

## Actual operator result

Run 3924c93ed8ac46abbf7af2552aaec14a completed 2026-11-14T00:02:59Z to 00:03:36Z
(01:02:59 to 01:03:36 Copenhagen), .1 under nonce ea09b46c0e99. The operator saw
Apps > Installed apps and no UAC prompt. The old probe returned target_verified=
true, no_update_toggle_exposed, hosted, zero node errors, no HRESULT.

That target flag checked only whether the nonce occurred anywhere. A row in the
Installed apps LIST could satisfy it. It did not establish the application's
DETAILS page. The old raw receipt is retained unchanged, but that flag is NOT
accepted as app-page proof or proof that Windows has no update control. This is
our diagnostic false-positive defect, separate from the corrected window binding
and from the updater's unchanged native 0x800706F7 failure.

The trust-ready receipt reports added=true, and trust-cleaned reports removed=
true for public thumbprint 537B5A758C298FEBEC5E90430DC569F30C3EE352. Independent
checks find no package, signing key or machine TrustedPeople leaf. Trust/install
DID occur; absence of a visible prompt does not invalidate those measurements.
Why no prompt was seen is not established. A read-only check found current UAC
policy values 1/5/1 (EnableLUA/ConsentPromptBehaviorAdmin/PromptOnSecureDesktop);
no policy was changed, and these current values are not a trace of the run.
The raw main receipt SHA-256 is 7d419da97a7e7d257272880fe14d96179a08d344573181cb052db68211dcccb5. Raw receipts remain ignored.
Prepared ea09b46c0e99 is USED; never rerun it or delete its marker/profile.

## Primary-source finding and limits

Microsoft's [URI reference](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings)
documents appsfeatures-app with a package-family query. However,
[MicrosoftDocs issue 3688](https://github.com/MicrosoftDocs/windows-dev-docs/issues/3688)
contains first-hand reports that external calls open Installed apps despite that
query, and reports of a caller's own packaged context opening its individual page.
The documentation and reported behavior conflict. This supports testing an
in-package launch, NOT declaring it a proven workaround on this VM. This issue
is about Settings navigation, not a matched report of 0x800706F7.

## Implemented caller-context launch

The existing qualification activation now accepts --qualify-settings-page with a
strict hex run ID. It requires an actually installed matching LocalTest identity
and publisher and an ordinary-user token. Production/unpackaged/malformed calls
are rejected before a launch. It runs before normal shell/Core/account startup.

Inside that signed package, Windows.System.Launcher.LaunchUriAsync opens the fixed
ms-settings:appsfeatures-app URI WITHOUT an external family query. Ten-second
cancellation and a fixed, bounded ten-field launch receipt report acceptance or
failure. The script validates run/family/name/version, status, context, bounds and
no extra content fields. This receipt always says g05_qualified=false: OS launch
acceptance does not prove which page was displayed. The activation helper only
selects between its two fixed diagnostic modes; no arbitrary command/URI input.

No fallback to the known-unreliable outside-package URI. The installer UI-only
flow passes its existing validated runner and retains install/uninstall/trust
cleanup. This mode invokes no update-policy setter, Core suggestion or editor test.

## Details-page verification

Nonce presence alone never qualifies. The probe now requires all of:

- the nonce and exact unique diagnostic display label;
- that visible label as a Text element outside List/ListItem/DataItem ancestry;
- visible Specifications/Specifikationer text plus the expected package version;
- absence of the actual Installed apps list anchor observed on this VM.

Unknown/deep ancestry and missing anchors stay unqualified. A recognized list is
wrong_settings_page / installed_apps_list, with control counts/IDs suppressed.
Only a verified app_details page can report an update toggle or
no_update_toggle_observed in the read tree; that is not a universal absence claim. Status and all anchor booleans are logged without raw captions.
These diagnostic anchors are qualified for the observed Windows UI; other layouts
or OS languages remain fail-closed, not a product compatibility assertion.

## Validation

- build_app.ps1: PASS 0 warnings / 0 errors.
- test_app.ps1 -SkipBuild: PASS 105.
- test_settings_app.ps1 -Mode UpdateState -SkipBuild: PASS 38. New cases reject
  production Settings activation, unpackaged caller and malformed run ID.
- test_release_scripts.ps1: PASS 161. Reproduces nonce-in-list false positive,
  checks header/spec/version conjunction and list veto; launch receipts reject
  cross-run, extra content and contradictory success. Existing expectations are
  strengthened: stale nodes may preserve nonce reads but cannot fabricate a page.
- Actual live Installed apps UIA tree: wrong_settings_page, installed_apps_list,
  target_verified=false, node_errors=0, toggle_count=0. No UI click, install or
  setting mutation was needed for this classification.
- New four-version signed LocalTest 654c593031fb prepared through existing scripts.
  MSIX readback verifies identity and the packaged DLL contains the new activation
  mode. Preparation key removed; no installation/trust/used marker. Existing
  qualified Core/pack bytes reused. No generated artifacts committed.
- NEW installed in-package URI launch / verified details page: NOT RUN. The compile,
  negative context tests and payload inspection do not qualify that integration.
- No new full GUI regression, successful G05 setter or official production release.

Ignored local evidence: g05-page-{build,app,state,release,live}.log,
g05-packaged-page-prepare.log and the corresponding MSIX attempt/preparation roots.
The previous 21 native setter failures and real hosted-window proof retain their
scope; they are not relabeled by this correction.

## Ready isolated follow-up

Run from an ordinary PowerShell in the Windows checkout:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 `
  -PreparedLocalTestSession .\.artifacts\msix-tests\prepared-654c593031fb `
  -SkipBuild -UpdatePolicyDiagnostic -UpdatePolicySettingsUiProbe `
  -UpdatePolicySettingsUiOnly -AllowTemporaryMachineTrust
```

If Windows asks for UAC, approve the test helper; a visible prompt is not a PASS
criterion. Trust-ready/cleaned receipts are authoritative for temporary trust.
Do not click system controls. Return the G05 Settings UI line and final record
path. This attempts navigation and reading only, then uninstalls/cleans trust;
it does not repeat the 21 policy attempts. Partial exit 3 is expected by design.

## Product status and next decision

The next installed run must establish whether in-package launch reaches the
correct details page. If it still opens the list, it now reports that accurately
instead of claiming an absent update control. Only after real page proof can we
choose how to measure the Windows-owned control. No automatic toggle exists.
The original setter's 0x800706F7, HTTPS comparison, off/on and N-to-N+1 preference
retention remain open. No VM fault, system repair, production privilege increase,
backend protocol change or macOS change is implied. G05 remains read-only.
