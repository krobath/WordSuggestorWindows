# WSA-UX-029: local Windows update-policy support dossier

Date: 2026-11-14, Europe/Copenhagen. Evidence prepared locally; no external report
submitted. G05 remains read-only. No system policy, privilege requirement or
distribution contract changed. Do not treat this as a confirmed Windows defect.

## Separate symptoms

1. **Native write failure:** C#, independent packaged C++ and Windows PowerShell
   references fail SetAutoUpdateSettings at write time with 0x800706F7. The
   [measured matrix](WSA-UX-029_g05_input_schema_acceptance.md) has 21/21 named
   input variants rejected, across baseline/management-query capabilities and
   App Installer schemas 2018/2021. Original state restored. Local file-backed
   feeds are common; equivalent HTTPS has NOT been tested.
2. **Settings UI/native mismatch:** In the [passive installed run](WSA-UX-029_g05_passive_observation.md),
   Windows Settings reads Off at eight UI read points over 15.74 seconds, while
   native settings remain unchanged with both update flags true. After HUMAN
   navigation back to the same app's Advanced options, it reads On. The observer
   does not launch/activate the package after the human flip. The unobserved gap
   still prevents attributing the transition specifically to navigation.

These are related investigation topics, not a demonstrated shared failure. No
RPC error was captured for the Settings toggle in the inspected event channels.
There is no proof that a saved native Off policy was overwritten by activation.
Normal WordSuggestor editor/Core startup is bypassed by diagnostic activation.

## Read-only environment follow-up

Windows DisplayVersion 26H2, build 26300.9974; the user identifies the host as a VM.
No evidence establishes virtualization as the cause. Versions read after the run:

| Component | File/package version |
|---|---|
| Microsoft.DesktopAppInstaller | 1.29.380.0 |
| SystemSettings.exe | 10.0.26100.9949 |
| ApplicationFrameHost.exe | 10.0.26100.9818 |
| AppXDeploymentClient.dll | 10.0.26100.9278 |
| AppxPackaging.dll | 10.0.26100.8875 |

Different component revision numbers alone do not establish corruption or an
incompatible installation. No repair/reinstall/update was attempted from this data.
Usernames, machine names, SIDs, private keys, raw event logs and document text are
not required for this dossier.

## Reproduction sources and boundaries

- [Native C++ control](../../tests/WordSuggestorWindows.Native/UpdatePolicyControl.cpp):
  independent WinRT route in a matching installed LocalTest context, including
  no-op, off/on and original-state restoration. Uses no language engine or UI.
- [Build helper](../../scripts/build_update_policy_control.ps1): VS 2022 C++ x64;
  default SDK 10.0.22621.0. Source and measured historical binary receipts must
  be distinguished from any future rebuild.
- [C# diagnostic](../../src/WordSuggestorWindows.App/Services/WindowsUpdateQualification.cs):
  guarded diagnostic entry before normal app startup. A full repository fixture
  includes Core assets, but they are not invoked by these update-policy probes.
- [Existing preparation](../../scripts/prepare_msix_test.ps1) and
  [qualification](../../scripts/test_msix.ps1): always generate a fresh isolated
  identity; never reuse the used IDs or profiles in evidence. Native mode needs
  -UpdatePolicyDiagnostic, schema comparison also -CompareUpdatePolicySchemas.
  Passive mode needs the UI-only flags and -PassiveSettingsUpdateObservation.
  The modes are separate and cannot qualify production.

No new ready-to-run installation command is issued here: repeating completed
matrices or the same GUI sequence will not fill the remaining source/priority
questions. A future independently scoped experiment must define the variable it
changes, use fresh fixtures, retain original-state/cleanup receipts and preserve
the existing release gates.

## Authoritative references and remaining questions

[Microsoft's overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview)
documents Windows Settings and PowerShell as configuration routes and describes
priority overrides. It does not diagnose this failure or prove that our getter
exposes every internal disabled-state mechanism.

[GetDefault](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.getdefault?view=winrt-26100)
documents that a later app update may overwrite default-priority values.
No N-to-N+1 update occurred during this passive test, so that statement does not
explain the measured result by itself.

[GetForSystem](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.getforsystem?view=winrt-26100)
explicitly requires administrator privileges and applies stronger priority. It
is not an automatic replacement for the normal-user product API. No elevation,
policy override, ClearAutoUpdateSettings or undocumented registry write is used.

The scoped public search on 2026-11-14 found no exact authoritative match linking
SetAutoUpdateSettings/Set-AppxPackageAutoUpdateSettings and 0x800706F7 to this UI
symptom. This is a search limit, not evidence that the issue is unknown to Microsoft.

The next useful work is to resolve native read-priority/disabled-state semantics
and source-type equivalence, or collect a targeted save-call failure using an
explicitly scoped diagnostic method. Broad traces or raw logs may contain other
users/apps' data and are not an acceptable substitute. An external issue can
reference this dossier only after reviewing/redacting any added local evidence;
this work does not send it or imply Microsoft has confirmed a bug.
