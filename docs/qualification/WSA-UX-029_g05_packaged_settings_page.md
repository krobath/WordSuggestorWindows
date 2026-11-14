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

## Measured read-only acceptance, 2026-11-14

Operator run 13ecc2ce9cfe42f4ad6571d7617814ce completed 00:34:55Z to 00:35:30Z
(01:34:55 to 01:35:30 Copenhagen), tested MSIX 0.2.0.1 under nonce 654c593031fb.
The installed own-package launch is now VERIFIED, replacing the earlier NOT RUN
state. Actual results:

| Proof | Actual result |
|---|---|
| Correct app details page | app_details / target_verified=true |
| Header / specifications / version | all true |
| Update control | found=true, exactly 1 toggle |
| Toggle AutomationId | WordSuggestor.Windows.LocalTest.654c593031fb_byhrsbzr6zdww_ToggleSwitch |
| Window / launch | hosted / launch_completed, packaged_context=true |
| UI errors | 0 nodes, no phase/HRESULT |
| Installer checks | clean_install=true, standard_user=true |
| Cleanup | trust added=true then removed=true; no package/key/machine leaf remains |
| Full/G05 release qualification | false, expected for read-only diagnostic |

Tested MSIX SHA-256: 951facd5904ffc268c71f11ecce52bd3a8821df0290495d04fc7a8065e0bff5b.
Raw main receipt SHA-256: 5dbb37ccddc30919216ae681af7f4d6881e1f4741fd5eefbeb0d138826f8bb91. Ignored raw receipt/root retained.
The used-session marker is present. Session 654c593031fb is USED: its former
command is retired and must not be rerun. User profiles/receipts are preserved.

This verifies navigation and control discovery, not the control's effect. No
TogglePattern.Toggle, mouse click, setter or off/on roundtrip was performed.
The earlier external-URI/list false positive is superseded; hosted window proof
and the previous 21 updater failures retain their separate scope.

## Product status and next decision

The next isolated test should have a human switch ONLY the disposable test app's
Windows-owned update control off and back on, while scripts read UI state and the
OS update settings before/after. This will show which settings Windows actually
changes, without invoking the failing setter or automating UI clicks. The user's
starting state and any native-property differences must be reported accurately.
Do not treat control discovery or a visually flipped switch as proof of automatic
update behavior or persistence across N-to-N+1. The original 0x800706F7, HTTPS
comparison and preference retention remain open. G05 remains read-only. No VM
repair, production privilege increase, backend or macOS change is implied.
