# WSA-UX-029: G05 input and feed-schema investigation

UPDATE 2026-11-14: the interactive run has now completed. Session b14042ba01ae
is USED. See [actual measured results](WSA-UX-029_g05_input_schema_acceptance.md).
The unused/not-run statements below describe earlier preparation only.

Date: 2026-11-13. Base b70c61d plus this delivery. Windows-only diagnostic
extension. The starting working tree was clean. Keep WPF, Core and pack contracts;
no product write permission, public release or backend change is included.

## Purpose

Determine whether optional input fields or the feed schema explain the native
0x800706F7 setter failure. G05 means the user's automatic-update preference; it
remains read-only until actual native off/on, restoration and update retention
are qualified. A completed diagnostic is never a G05 PASS.

## Research incorporated

- [RPC error codes](https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1700-3999-)
  identify 1783/0x6F7 as RPC_X_BAD_STUB_DATA. Generic
  [RPC buffer protection](https://learn.microsoft.com/en-us/windows/win32/rpc/mccp-buffer-protection)
  describes inconsistent lengths/pointers as possible triggers, not a diagnosis
  of this API or VM. No matching public SetAutoUpdateSettings/HRESULT issue found.
- [GetDefault](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.getdefault?view=winrt-26100)
  explicitly allows a later app update to overwrite these preferences. A future
  working setter must also prove the user's choice survives N to N+1. Do not
  replace it with GetForSystem as an untested normal-user permission workaround.
- [Current auto-update guidance](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview)
  recommends schema 2021 for prompt settings; the separate
  [2018 reference](https://learn.microsoft.com/en-us/uwp/schemas/appinstallerschema/element-s3-onlaunch)
  also describes those attributes. Compare actual behavior instead of declaring
  the existing feed invalid from that documentation discrepancy.
- [Interval contract](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.autoupdatesettingsoptions.hoursbetweenupdatechecks?view=winrt-26100)
  allows both 0 and 6. Prior measurements already rejected both.
- The [Settings URI contract](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings)
  documents ms-settings:appsfeatures-app?<PFN>. This delivery only reads that
  page and never toggles, resets or uninstalls through the UI.

## Diagnostic implementation

The optional native worker, absent from production payloads, retains its exact
own-package/publisher/family/non-elevated checks. After its original no-op/off/on
control and VERIFIED restoration, it tries seven fixed cases: no_prompt, minimal,
minimal_no_version, update_uri, repair_uri, both_uris, no_repair. Minimal options
retain source, both scheduling flags and interval; minimal_no_version also omits
Version. URI cases append the original local source as update/repair fallback.
They do not introduce a new host or send traffic to a public endpoint.

Each case records acceptance, fixed stage/HRESULT and original-state restoration.
It may temporarily change optional settings of its disposable package; it is not
a product no-op or successful off/on proof. All original policy fields/flags are
checked and restored after EACH case. Restoration failure stops the sequence and
fails the outer native gate. Closed native schema v3 permits at most seven cases
in fixed order; arbitrary fields, user text and inconsistent outcomes are rejected.
Existing 8KiB stream bounds, timeout and owned-process cleanup remain in force.

The existing test_msix diagnostic can compare 2018 and 2021 using .1/.2 then the
higher .3 package under ONE isolated identity. The runtime namespace is recorded
for every variant, and comparison refuses legacy fixtures without the seven-case
native worker. Only private runtime feed copies
are modified. Signed bundle manifests, production templates and the default full
suite are unchanged. Optional comparison/UI/schema parameters are rejected outside
the marked prepared diagnostic. Full release flags remain false/partial exit 3.

The read-only Settings probe validates the installed LocalTest family and ordinary
user first, then opens the documented URI. It only reports UI control metadata if
the unique package nonce is visible within the OS Settings UI Automation tree.
If the target cannot be verified, it records that limitation and performs no UI
mutation. No window captions, account names, URI values or document text logged.
The Settings window is not forcibly closed or its process terminated.

## Reproduction

From the Windows repo, after its documented prerequisites:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare_msix_test.ps1 -UpdatePolicyCapabilityExperiment -BuildUpdatePolicyNativeControl
$testSession = Read-Host "Paste the NEW prepared session directory"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 -PreparedLocalTestSession "$testSession" -SkipBuild -UpdatePolicyDiagnostic -UpdatePolicyDiagnosticHours 6 -CompareUpdatePolicySchemas -UpdatePolicySettingsUiProbe -AllowTemporaryMachineTrust
```

Ordinary-user execution only. The existing temporary TrustedPeople leaf-trust UAC
approval is required; no root certificate or production identity is trusted. Never
reuse an installed/used nonce or delete its retained profile. No Hello probe.

## Software checks

- build_app.ps1: PASS zero warnings/errors with normal process permissions; an
  initial restricted build exited 1 without compiler diagnostics.
- Native C++ x64/static-CRT control: PASS compile.
- test_app.ps1 -SkipBuild: PASS 105.
- test_settings_app.ps1 -Mode UpdateState -SkipBuild: PASS 35, including closed
  input-result schema and contradictory/private metadata rejection.
- test_release_scripts.ps1: PASS 135, including diagnostic-only guards and
  rejection of production targets before opening Settings.

## Native measurements

Actual attempt bdd73080436f4996a10b8f033c75b188 started 2026-11-13T22:40:08Z,
finished 22:42:13Z. Windows RunAs returned cancellation before trust-ready and
before installation. The recorded outer exception was InvalidOperationException /
0x80131509; this is the UAC launch failure, NOT the native setter's 0x800706F7.
On 2026-11-14 the operator confirmed that the UAC dialog was NOT visible on the
VM. This does not mean the operator rejected it, and does not identify a VM or
native setter fault. No further agent-launched UAC attempt was made. Next run the
same prepared, never-installed session from an interactive ordinary-user PowerShell
on the VM, so the trust prompt can be handled locally.

| Native item | Actual result |
|---|---|
| Temporary leaf-trust helper handshake | NOT ESTABLISHED; Windows reported cancellation |
| Clean install / baseline input matrix | NOT RUN |
| Capability input matrix | NOT RUN |
| 2018 vs 2021 feed comparison | NOT RUN |
| Windows Settings page / update toggle | NOT RUN |
| G05 off/on/restoration qualification | STILL FALSE |
| Standalone unpackaged worker guard | PASS: context=false, identity/0x80073D54, exit 3, no input cases |

Independent after-attempt checks: package_registered=false, private_signer_present=
false, temporary_trust_present=false, trust_ready=false, no app/helper process.
completed/all_checks_passed/release_qualified are false. The isolated profile has
not been used by an installed app. An explicit cleanup.request was also written
to the failed session so any late helper immediately removes its temporary leaf.
The source now sends that request even when RunAs fails before returning a process.
No root trust, Windows policy, service, VM or production setting was changed.

Prepared session b14042ba01ae, test publisher CN=WordSuggestor Local Test
b14042ba01ae, public leaf thumbprint 962E851EFCE582D276120889D04E9766E635F6B0.
The ephemeral private key was removed; signature_verified stays false until the
native trust gate. Four versioned signed bundles were prepared through the normal
package script; no generated artifacts are committed. Exact bundle contracts:

| Version | MSIX SHA-256 |
|---|---|
| 0.2.0.1 | a85720a17d76193d9f86acba4591beafd50ddbdd7c2ac7669903189bed3b001b |
| 0.2.0.2 | 53ea5a3c9df193060881eaf22113b63a44034e85dc826f8de616d57431bce549 |
| 0.2.0.3 | 887c1751d26bdb3a2bc4c998a8b218f6c77a693fd6b29d4b2d19cf0ddc389c3f |
| 0.2.0.4 | 77fe45846bf70dffc47848fc6bf4f0189b0c499db410026e6a4a3c67107cbb17 |

Ignored local evidence: .artifacts/g05-input-{build,cpp-build,state,app,release,
prepare,native}.log and the matching msix-tests attempt/prepared directories.
The four installed diagnostic series from the previous record are not
reclassified by these new software checks. No new setter success is claimed.

## Next decision and limits

Use the actual matrix below to choose the next reduced reproduction. A successful
input variation is only a lead: the real user toggle must still preserve policy,
restore safely and survive an app update. HTTPS parity, official signing, unattended
update completion, Hello and human accessibility acceptance remain separate gates.
Do not infer a VM fault or request replacement from the shared HRESULT alone.

## Consolidation

Windows local docs own this evidence. Shared Docs may link the measured outcome;
macOS, Core, pack and backend code/contracts remain unchanged.

## Historical preparation snapshot, before the measured 2026-11-14 run

Prepared session b14042ba01ae remains unused: no installed package, no profile
files, no signing key or machine leaf trust, all four bundles present. Public
certificate expires 2026-11-20. Repository clean at 6e15f0f before this evidence
update. The previous failed attempt has its cleanup request; a new invocation
creates a NEW attempt directory and does not reuse that failed trust handshake.

This preparation snapshot is historical. The interactive run completed afterwards;
b14042ba01ae is USED. Its former command is retired. Use only the fresh read-only
UI follow-up documented in [the measured acceptance record](WSA-UX-029_g05_input_schema_acceptance.md#ready-read-only-ui-test).
