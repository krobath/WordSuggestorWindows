# WSA-UX-029: measured G05 input/schema results and UI follow-up

Date: 2026-11-14 (Europe/Copenhagen). Base 1c0b9eb. Initial Windows working tree
clean. Native run supplied by the operator and verified against the local JSON.
No further installation, UAC prompt or repeated matrix was initiated by the agent.

## Measured native result

Run 97decc4ecc3744129e814c008aae924c completed 2026-11-14 00:18:38 to 00:19:33
Copenhagen (2026-11-13T23:18:38Z to 23:19:33Z). LocalTest identity/publisher nonce
b14042ba01ae. The earlier agent attempt bdd73080 never reached installation; this
interactive run DID establish trust and install successfully. Thus the previous
UAC obstruction is separate from the still-reproducible native setter failure.

| Variant | Installed version | Management / query declared | Runtime feed schema | Native input cases |
|---|---|---|---|---|
| baseline | 0.2.0.1 | false / false | 2018 | 7 rejected / restored |
| capabilities | 0.2.0.2 | true / true | 2018 | 7 rejected / restored |
| schema_2021 | 0.2.0.3 | true / true | 2021 | 7 rejected / restored |

All 21 named input attempts failed at write_policy with 0x800706F7. The C# adapter
also failed at write_policy for each variant. All three native C++ factory/manual
original-data no-op controls failed, with matching packaged context and original
state restored. Windows cmdlet and PowerShell WinRT references independently
returned 0x800706F7/unchanged=true. The same .2 zero-interval control also failed,
original_hours=6/requested_hours=0/restored=true. All three input-hours readbacks
were 6; the actual runtime XML namespaces confirm the requested schema comparison.

These measurements show that NONE of the tested capability, optional-field,
fallback-URI, repair, prompt, version or schema variants resolves this failure.
They do not exclude all data/source/OS causes. Local file-backed feeds remain
common; equivalent HTTPS has NOT been tested. No VM fault or Windows regression
is established. G05 remains unqualified/read-only; no production rights are added.

The three app assembly hashes in the actual receipts are:

- 0.2.0.1: 59C91A78745068B8AFB6D65C2E10A4849B7CCBFD698BCBE89C97AD852C29BF70
- 0.2.0.2: 0019578CB0A4287629101B048D5607776771AC2095141182CD3782306F909301
- 0.2.0.3: 88B617203786726A2C1B26165FD2AE13933912FCE1D3643F4EB18EFF98710304

The immutable local receipt SHA-256 is bafc7d493b90defa2f4a244df367c361c15db12325efd0a7f35d58ac0c6ad24b. Raw receipt/logs remain ignored
under .artifacts/msix-tests/97decc4ecc3744129e814c008aae924c. User-provided console
output agrees with that receipt; no raw user terminal transcript is committed.

## Cleanup and interpretation

completed=true, test_trust_cleaned=true. all_checks_passed/release_qualified=false
by diagnostic-mode design; completion is not native G05 success. Clean install
and standard-user checks pass. The trust-cleaned receipt reports removed=true.
Independent checks on this machine: no package registration for b14042ba01ae,
no corresponding CurrentUser signing key, no LocalMachine TrustedPeople leaf,
no WordSuggestor/helper process. Original profile and used receipts are preserved.

Prepared-b14042ba01ae is now USED. NEVER rerun it or remove its profile to bypass
the fresh-identity checks. The earlier unused-session instructions are historical.

## Windows Settings probe: still unqualified

The actual UI result is ui_probe_unavailable, page_target_verified=false,
update_toggle_found=false. This is a probe failure, NOT proof that Windows has no
such control. No Windows toggle was invoked. The old catch suppressed the phase
and HRESULT, and the old test payload displayed the same generic name as the beta.
The actual exception during that run cannot be reconstructed from the old record.

Additional read-only inspection after uninstall found a Settings process with no
main window; attempting FromHandle(0) rejects with 0x80070057. The real probe
already skips zero handles, so this observation is NOT claimed as its root cause.
No user Settings page was opened or changed during that inspection.

## Implemented diagnostic corrections

- ONLY explicit LocalTest payloads containing the native diagnostic worker get a
  unique WordSuggestor LocalTest <nonce> app/publisher display label. A guarded
  helper requires the matching test identity and publisher. Package Identity,
  production display names/templates and all update URLs are unchanged.
- UI reads preserve fixed failure phase, underlying HRESULT/category and bad-node
  counts, while omitting captions and exception messages. One stale UI node no
  longer discards all other nodes. A missing nonce still prevents exposing control
  IDs/counts or claiming a verified target. No clicks/toggles/reset/uninstall.
- The EXISTING test_msix flow gains UpdatePolicySettingsUiOnly. It requires the
  marked diagnostic plus its read-only UI probe, rejects schema comparison, and
  installs only N before probing and cleaning up. It records the actual UI-tested N version/hash separately from the candidate
  metadata and invokes no Core/editor/native setter checks and cannot qualify the normal suite or a production release.
  This avoids repeating the already-measured 21 attempts just to inspect the UI.

No C#, Core, pack, macOS, backend or production updater behavior is changed.
Keep the existing WPF architecture and three-layer boundary.

## Software validation

- test_release_scripts.ps1: PASS 144. Covers production/context rejection, display
  identity preservation, stale-node resilience, nonce gating, metadata privacy,
  nested HRESULT/phase and the UI-only context guard. No UAC/install/network.
- Empty UI summary and PowerShell nonce argument binding: PASS in a Bypass child.
  An initial direct dot-source was refused by execution policy and is not a PASS.
- Previous build/app105/UpdateState35 remain their recorded checks. Fixture
  preparation successfully publishes self-contained x64 app payloads. No separate
  build_app/app-state regression, C++ rebuild or full GUI regression was rerun.
- New nonce label and UI-only installed integration: NOT RUN yet. Preparing signed
  packages is separate from clean install/UI qualification.

## Next step in product terms

First identify the Windows-owned control on a freshly named isolated test package.
The next read-only test should tell us whether Windows exposes that control and,
if inspection fails, exactly which phase and HRESULT failed. Do not infer a control
is absent from ui_probe_unavailable. Only then decide how to measure the OS control
itself. No automatic clicking is implemented here.

The remaining source comparison needs a separate correctly hosted HTTPS LocalTest
feed, never synthetic releases on the official beta feed. A future working G05
path must also preserve the user's off/on preference through N to N+1: Microsoft
[documents that GetDefault settings may be overwritten by an update](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.getdefault?view=winrt-26100).
No GetForSystem/elevation/registry workaround is enabled.

Official signing/publication, unattended updates, user-deferred Hello and remaining
human/backend gates keep their separate status. Shared Docs may link these Windows
results; do not close the sprint or infer a macOS change.

## Ready read-only UI test

Fresh prepared session bc06ec5df05e is ready. All four signed bundle manifests were
read back from the MSIX ZIP and verified to have the exact isolated identity and
unique app/publisher/visual labels. Preparation/independent checks confirm no
installation, no remaining private key or temporary machine trust. Public test
certificate expires 2026-11-21. Do not trust it for production.

From an ordinary PowerShell in the WordSuggestorWindows checkout:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 `
  -PreparedLocalTestSession .\.artifacts\msix-tests\prepared-bc06ec5df05e `
  -SkipBuild -UpdatePolicyDiagnostic -UpdatePolicySettingsUiProbe `
  -UpdatePolicySettingsUiOnly -AllowTemporaryMachineTrust
```

Approve the local test PowerShell UAC. This read-only UI variant installs only N,
opens the uniquely named test app's Settings page, reads the control metadata and
uninstalls/cleans temporary trust. Do not click system controls during the probe.
A completed diagnostic still exits 3 and does not qualify G05. Return the G05
Settings UI line and final record path. This NEW UI-only installed test is NOT RUN
as of this commit. Do not rerun the USED b14042ba01ae series.
