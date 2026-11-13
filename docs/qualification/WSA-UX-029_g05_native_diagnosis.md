# WSA-UX-029: G05 native write diagnosis and capability comparison

Date: 2026-11-13. Base b88e16b plus the working-tree changes documented here.
Windows-only app adapter, qualification scripts and local docs. No Core, pack,
macOS, backend, shared Docs or public distribution change. G05 is NOT fixed.

## Measured finding

Actual isolated diagnostic 066d81217d7e4f9d890a044f71244a2b completed on this PC
2026-11-13T20:21:50Z to 20:22:36Z. Same identity/publisher, .1 baseline then .2
with packageManagement/packageQuery. Both installed ordinary-user app probes
successfully read the policy and create options, then fail at write_policy with
0x800706F7. Neither changes settings. Independent restoration/fingerprint checks
pass. The broad capability addition alone does NOT resolve this host's failure.

| Route | Result | State after test |
|---|---|---|
| Packaged C# baseline, no management/query capabilities | write_policy / 0x800706F7 | unchanged/restored |
| Same identity .2, both capabilities declared | write_policy / 0x800706F7 | unchanged/restored |
| Windows Set-AppxPackageAutoUpdateSettings cmdlet reference | 0x800706F7 | unchanged |
| Separate Windows PowerShell WinRT projection/options reference | 0x800706F7 | unchanged |

This narrows the fault beyond the checkbox/controller and makes a C#-only
projection defect or missing declarations alone less likely. It does not prove
an OS bug: both reference paths share Windows deployment services and the same
local file-feed settings. HTTPS source behavior and another Windows installation were NOT tested.
Later native C++ controls are recorded below. The cmdlet and Windows PowerShell WinRT
references are unpackaged ordinary-user processes, not identical package contexts.
No native route succeeded, so off/on correctness cannot yet be qualified.

Native readback: packaged_app_context=true, read_code=local_test,
mutation_code=write_failed, mutation_stage=write_policy, changed=false,
restored=true, restore_stage=restore_verify, source_and_other_settings_preserved=true,
production_control_enabled=false. Both steps arrays stop at the failed first
transition (enabled=false); there is no claimed successful off/on roundtrip.

## Windows environment and sources

This PC reports CurrentBuild 26300, UBR 9974, DisplayVersion 26H2. The user
confirms it is a Windows 11 VM. Microsoft's CURRENT
[Windows release information](https://learn.microsoft.com/en-us/windows/release-health/windows11-release-information)
lists EXACTLY Windows 11 26H2 / 26300.9974 as General Availability, released
2026-11-10. The initial inference from a January Insider-family announcement
was outdated and is withdrawn. No evidence identifies virtualization or an
unsupported OS as the cause. No replacement/reinstallation or second VM is
required at this stage. Diagnosis continues on this same current Windows VM.

[SetAutoUpdateSettings documentation](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.setautoupdatesettings?view=winrt-26100)
requires packageManagement and packageQuery. The general
[capability guidance](https://learn.microsoft.com/en-us/windows/uwp/packaging/app-capability-declarations)
notes that full-trust desktop requirements differ from AppContainer apps. Thus
missing capabilities were an experimental hypothesis, not an established cause.
[Microsoft's error table](https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1700-3999-)
identifies 0x6F7 as RPC_X_BAD_STUB_DATA. The shared HRESULT does not distinguish
permissions, malformed/native data, source type or a Windows implementation fault.
Deployment event metadata in the precise test window contained test-identity
install/update/removal activity but no matching RPC-error or AutoUpdate event;
no raw event messages were logged or committed. No public known-issue match found.

## Implemented changes

- WindowsUpdateMutationResult adds optional fixed Stage metadata. Native mutation
  labels options creation, manager creation, flag assignment, write, readback,
  verification and recovery. Restore labels its read/options/write/verify phase.
  Options preparation now falls inside the existing failure handling. No raw
  exception messages, URI values or document/credential content are logged.
- The LocalTest qualification now requires both enabled and disabled scheduling
  states, each with actual readback and an unchanged non-scheduling fingerprint,
  followed by restoration. The external verifier rejects single-transition or
  unverified evidence. Optional v1 diagnostic fields add read/mutation/restore
  codes/stages, declared-capability booleans and at most two fixed steps.
- package_app -TestUpdatePolicyCapabilities is LocalTest-only; the common helper
  requires matching nonce identity/publisher. The production template is unchanged
  and declares neither broad capability. No product write gate was enabled.
- prepare_msix_test -UpdatePolicyCapabilityExperiment uses the existing signing/
  package flow, emitting .1 without these capabilities and .2/.3/.4 with both,
  then deleting the ephemeral key. Only .1/.2 are installed in this diagnostic;
  the extra signed packages reuse the established four-version flow.
- test_msix -UpdatePolicyDiagnostic requires that marked prepared experiment,
  runs packaged probes on .1 and .2, verifies their actual capability declarations,
  and reuses the isolated native reference routines. Experiment receipts cannot
  enter default native release qualification. No editor/GUI/Hello test is started.
  completed=true only means diagnostic completion; all_checks_passed=false and
  release_qualified=false are forced, with partial exit 3 by script design.
- Later final software edits tighten off/on evidence validation, preserve a
  read/policy-guard stage when mutation is unavailable, and validate variant labels.
  These do not create a new native success; the measured native binaries retain
  their hashes below. No second native run was performed merely to relabel failure.

## Exact artifacts and cleanup

Used preparation .artifacts/msix-tests/prepared-3067d6ee94d7/prepared-session.json.
Identity WordSuggestor.Windows.LocalTest.3067d6ee94d7; publisher
CN=WordSuggestor Local Test 3067d6ee94d7. Public leaf thumbprint
9626136AEDE1A64C2F062E7B79EAF94DA48B2EC2. One authorized temporary leaf-trust
helper ran; it never trusts a root. The non-exportable private key was deleted
at preparation. Independent post-run checks: no registered test package, no
matching CurrentUser signing key, no matching LocalMachine TrustedPeople leaf,
zero WordSuggestor processes. Original source/settings and prior profiles preserved.

| Version | MSIX SHA-256 |
|---|---|
| .1 baseline | a8e022a3fbc7b2cc56078031a90c83bde187240f5a68cdb44c38a85634d0a7fd |
| .2 capabilities | fe8647edcc10d1e79d69aeab6dd932ffc73c192a7052b93f94e0c16542d2a8c2 |
| .3 unused | 9c13adeff41a24a77cfcd26899d088a5062945d4d1f021dc408568026792db58 |
| .4 unused | 2f6bf655244c16c481574df264249b14ba561b0fc9e2403c531c57d08808caaf |

Measured .1 app assembly hash 62462359CE12329A9725D3B155C4541A97FD41268AA3C064455B7FE901D6923D;
.2 hash 7FC0AE531053E431643155863636C6E2056CE6ADE0D4B32C5F16764983B3C5B3.
Ignored log/receipt root .artifacts/msix-tests/066d81217d7e4f9d890a044f71244a2b
contains policy-baseline, policy-capabilities, trust-cleaned and overall JSON.
Other raw logs .artifacts/g05-{build-final,app,update-state-final,release-final,
preparation,native}.log. Only fixed result metadata/hashes enter Git.

## Software validation

| Gate | Result |
|---|---|
| build_app.ps1 | PASS zero warnings/errors, repeated after final diagnostic fallback |
| test_app.ps1 -SkipBuild | PASS 105 |
| test_settings_app.ps1 -Mode UpdateState -SkipBuild | PASS 31, including native-result privacy/contradiction tests |
| test_release_scripts.ps1 | PASS 131, including production/experiment boundaries, two-mode proof and zero-interval restoration tests |
| Four signed x64 experiment packages | PASS MakeAppx/signing through existing flow, key removed |
| Ordinary-user native baseline/capability/reference comparison | COMPLETED, four blocked write routes, exact unchanged/restoration evidence |
| G05 production write qualification | FALSE |

No new GUI/full regression or official production qualification is claimed.
An earlier sandboxed release-script run failed the existing transport child-reaping
assertion. The final run with normal process permissions passed all 131 checks,
including that unchanged assertion; no test expectation was weakened.
The prior combined 22/24 native and installed GUI 56 evidence remains valid within
its [recorded scope](WSA-UX-029_slice14_installer_dialog_acceptance.md).

## Reproduce the isolated diagnostic on this VM

First follow README's documented SDK/Swift/Core/pack prerequisites in a local
checkout. Then, from WordSuggestorWindows as an ordinary user:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare_msix_test.ps1 -UpdatePolicyCapabilityExperiment -BuildUpdatePolicyNativeControl
$testSession = Read-Host "Paste the NEW Prepared local test session path"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 -PreparedLocalTestSession "$testSession" -UpdatePolicyDiagnostic -UpdatePolicyDiagnosticHours 6 -AllowTemporaryMachineTrust
Write-Host "Test exit code: $LASTEXITCODE"
```

Approve only the documented temporary LocalMachine TrustedPeople UAC request.
The diagnostic never runs the app elevated or starts Hello. Use a new nonce on
the target PC: absolute-path prepared receipts from this machine are not portable,
and this used nonce/profile must not be deleted/reused. No production credentials
are required. Diagnostic-only exit 3 cannot qualify the default release suite.
Inspect the four route results, stage, actual declarations and cleanup, not just
process activation. Do not add capabilities to the production template as a fix.

## Next decision

Continue reducing options/source inputs on this same VM. A native C++ control
now compares original-data no-op and off/on calls without .NET; its measurements
are appended below. Another installation is optional later cross-validation,
not an established prerequisite or evidence against this VM. Do not change the
backend O-052 contract or publish test releases to investigate this issue.
No Windows reinstall, broad capability grant, registry/service repair, unsigned
updater or elevated production workaround was performed. User data is untouched.

G05 remains read-only; unattended update completion is a separate open gate.
Hello user-deferred, official signer/HTTPS publication and human/platform checks
remain open. This delivery implements diagnosis and executes it; it does not
claim the intended G05 product fix or full sprint/release completion.

## Later same-VM C++ and input controls

All later runs used fresh nonces, the same ordinary-user VM, temporary leaf trust
and mandatory cleanup. No second VM or OS change was requested after correcting
the initial outdated Insider inference.

| Actual run | Additional controlled comparison | Measured outcome |
|---|---|---|
| 2e29478e6a4941eaa0e6c813821de242 / nonce 9adadab20b0b | Packaged C++/WinRT, original-options no-op, both capability variants | context matches, no-op fails 0x800706F7, restored=true |
| 0869671087d349cf9bfe0975b6b88bbf / nonce 95f4b002e74a | Native factory vs explicitly constructed options, both capability variants | factory and manual no-op attempts report 0x800706F7; restored=true |
| f865cfdb02ba4e5cae0bf5e098f21ea5 / nonce 8282e662532e | Package.Current info route, freshly constructed URI values, 6-hour interval matching release defaults | same C#/factory/manual failure; both native input-hours values read back as 6 |
| Same f865 installed .2 | Windows cmdlet requests interval 0 while preserving other current values, then verifies original 6-hour policy | rejected 0x800706F7, accepted=false, restored=true |

The first C++ versions labeled a no-op attempt as a whole. The final helper
separately labels factory/manual option creation and the actual write; its closed
schema v2 identifies info_route=current_package. Actual f865 receipts have matching
packaged context, both factory and manual HRESULT 0x800706F7, noop/off/on all false,
manual_options=true and restored=true. No .NET runtime is involved in the C++ call.
This rules out a purely .NET-only explanation, not all possible input/Windows
faults. A 0 interval alone is not the explanation: the same failure occurs at 6.

The C++ reference is compiled by the existing installed VS2022/Windows SDK tools
through build_update_policy_control.ps1, using static CRT/x64, and included ONLY
when the explicit LocalTest native-control parameter is supplied. It rejects
unpackaged identity, wrong family/publisher and elevation before any mutation.
Actual unpackaged execution returned context=false / 0x80073D54 / exit 3 with no
setting change. Native diagnostic output is fixed metadata only, bounded to 8KiB
per stream / 15 seconds by the parent; no captions, URIs or document text logged.
The parent rejects extra/content-bearing fields and contradictory success data.
The helper must prove no-op/off/on plus restoration; C++ success would still be a
reference result, never an automatic production-gate enablement.

The final source also hardens manual-route restoration, separates creation/write
phase names, and validates outcome consistency. Current schemas and phase names
are tested in software; the earlier native receipts retain their exact emitted
schemas/labels and binary hashes. No old result is retroactively upgraded to PASS.

The test-only interval parameter is rejected outside diagnostic mode. The 0-value
control saves an immutable original fingerprint and restores/verifies the original
policy if the setter accepts 0; unit tests cover acceptance, rejection and a rejected setter that changes only
scheduling flags. Final restoration compares the original scheduling booleans
as well as the non-scheduling fingerprint.
It never acts on production or a nonmatching test family. No runtime feed, global
update interval or existing installed user package was changed permanently.

All four measured nonces (3067d6ee94d7, 9adadab20b0b, 95f4b002e74a, 8282e662532e)
were independently checked after testing: no package registration, no signing key,
no temporary TrustedPeople leaf, no WordSuggestor/helper process. Their original
profiles and receipts remain preserved. All are USED and cannot be rerun unchanged.
Later ignored logs: g05-native-cpp, g05-native-cpp-final, g05-native-six-hours,
g05-cpp-build-current, g05-build-current, g05-update-state-last and
g05-release-final-reviewed under .artifacts. Final source software: build 0 warnings /
errors, app 105, UpdateState 31 and release 131 PASS. C++ compile also succeeds.
No new GUI/full release PASS is claimed.

Additional read-only environment checks do not establish a VM fault. Guest CPU
reports architecture 9/address width 64. Windows Update and component-servicing
reboot-required markers are false; a pending-file-operation marker is true, which
alone does not justify a reboot diagnosis. Deployment DLL file versions are
26100.9278 (client) / 26100.9818 (server); different component file revisions alone
are not proof of incompatibility. Relevant SDK22621/26100 interface GUIDs and ABI
method signatures match, apart from the novtable annotation syntax. No framework
migration, DLL replacement, registry repair, service restart or VM change made.

Next narrow source/optional-policy inputs and compare the app-specific Windows
Settings control on THIS VM before attributing the fault to the environment.
The local file-backed source remains common to all probes; equivalent HTTPS
behavior has not been tested. The diagnostic is a reproducible negative result,
not a completed G05 fix. A user-visible fallback or write enablement must wait for
an actual supported, reversible path and the corresponding product review.
