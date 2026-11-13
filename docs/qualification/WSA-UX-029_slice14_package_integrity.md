# WSA-UX-029: package integrity and reported build failure

For the later user-run popup/GUI failure and the NEW prepared rerun, use
[installer dialog qualification](WSA-UX-029_slice14_installer_dialogs.md).
Integrity results below are historical measurements, not the current rerun command.

Date: 2026-11-13. Base Windows commit d539adc. Windows-only packaging,
qualification tooling and implementation documentation; no Core, pack, macOS,
backend or shared Docs edits. No production signing or publication.

## Reported Settings build failure

The pasted CS1061 references the previous SettingsFormController.OpenInstaller
call. Commit 92f2ed5 already removed that call in favor of the guarded update
coordinator. No OpenInstaller call exists in current src/tests. Do not restore an
unguarded launcher to satisfy historical output. Current build_app and a second
build with a fresh ArtifactsPath both pass with zero errors/warnings. The Settings
smoke project also rebuilds successfully and UpdateState passes 26 checks.
The pasted HelloNative run failed during compilation and proves no native Hello
result. Positive PIN/biometric qualification remains user-deferred; not invoked.

## Original failure retained

User-run e281a55453d24952a915c8b2fed4a068 installed/updated/rolled back and checked
extended retained data, then failed with 'Tampered MSIX accepted'. Cleanup receipt
reports test trust removed. Do not relabel this run as PASS.

Byte 100 was inside compressed WinRT.Runtime.dll data (first ZIP entry; data
starts at byte 47). Extraction of that entry fails; other entries match the signed
original. Deployment activity ab689e29-7207-0003-26e2-19ac0772dd01 records successful
registration of .4, not a same-version no-op. Event 157 reports validated core
metadata and deferred payload verification. The removed installed DLL could not
be read afterwards, so its exact bytes were not established by that run.

Windows shares original package files/blocks between installations, as described
in [Microsoft's MSIX deployment architecture](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes).
Reuse of original blocks is a possible explanation for accepting the source;
it is not proof that this old run installed safe bytes. New tests must establish
that invariant rather than assume it from registration success.

## Implementation and integrity invariant

The package template now declares uap10:PackageIntegrity / Content Enforcement=on,
supported from Windows 10 19041 (the existing manifest minimum). The common local
release gate rejects missing, off/default, duplicate or wrong-namespace policies;
previous signed fixtures need rebuilding. See [Microsoft runtime tamper protection](https://learn.microsoft.com/en-us/windows/msix/desktop/tamper-protection)
and [PackageIntegrity schema](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap10-packageintegrity).
This hardens the actual app package; it does not enable G05 or change identities,
minimum OS, data paths or update consent. The minimum is a technical manifest
requirement; this run does not qualify every supported Windows release.

The native test uses a signed .4 newer than the installed .3:

1. Alter AppxSignature.p7x with a valid ZIP CRC. Full SignTool must fail; native
   Add-AppxPackage must reject and leave the installed version unchanged.
2. Test older-version rejection and unchanged same-version installation while
   the verified .3 is still installed.
3. Independently alter compressed DLL payload while preserving footprints. Full
   SignTool must fail. Native deployment must either reject without changing the
   installation, or install only bytes identical to the trusted original: compare
   SHA-256 and size of EVERY block-map-listed payload file before any launch.
   Changed/missing/escaped/duplicate content fails closed. Signature corruption
   is never allowed by this alternate outcome. Registration alone is insufficient.
4. Uninstall only the owned LocalTest package. Check registration removal, exact
   retained sentinel hashes and semantic Core/store readback after uninstall.

Assert-InstalledMsixPayload assumes its original has already passed the existing
Test-ReleaseBundle -VerifySignature trust gate. The public uploader still requires
full archive SignTool trust/signature and release hashes BEFORE staging; it never
publishes a bad source archive even if Windows could reuse original content.
No installed-file writes, WindowsApps ACL changes, alternate updater or signing
bypass. Corruption fixtures never execute unverified payload.

## Software results

| Command (repository root) | Result |
|---|---|
| scripts/build_app.ps1 | PASS, 0 warnings/errors |
| scripts/build_app.ps1 -ArtifactsPath .artifacts/repro-user-build | PASS, fresh obj/bin, 0 warnings/errors |
| scripts/test_settings_app.ps1 -Mode UpdateState | PASS 26; rebuilt smoke project, 0 warnings/errors; no Hello |
| scripts/test_app.ps1 -SkipBuild | PASS 98 |
| scripts/test_release_scripts.ps1 | PASS 94, including 21 new integrity checks |
| scripts/prepare_msix_test.ps1 -SkipCoreBuild -CoreCliPath <previous verified CLI> | PASS four self-contained x64 packages; ephemeral private key removed |

PowerShell scripts above run via powershell -NoProfile -ExecutionPolicy Bypass -File.
Ignored raw logs: .artifacts/integrity-{release,update-state,app,preparation,native}.log,
.artifacts/user-output-{repro-build,fresh-build}.log. Unit ZIP fixtures are synthetic,
not signed/native acceptance. No new full clean-source regression claimed.

## New signed fixtures and native result

Prepared session: .artifacts/msix-tests/prepared-2b1b771681d8/prepared-session.json.
Identity WordSuggestor.Windows.LocalTest.2b1b771681d8; publisher
CN=WordSuggestor Local Test 2b1b771681d8. Public leaf thumbprint
97AFEB9C4B9C220D15746FD8DBF2B194D02FACAB; seven-day, non-exportable private key
removed by preparation. This is never the production signing identity.

| Version / purpose | MSIX SHA-256 |
|---|---|
| 0.2.0.1 install | 698aec6636700d646dd18e5a52b96e99ed8f4ecdd1500fcad925455cabe4a3be |
| 0.2.0.2 update | 6c0a60985d0321bd3cb9b787b04be9a19802a0d72a284377da1a03f74be5f6fe |
| 0.2.0.3 higher-version rollback procedure | 260a6be3d6654599c4b5661349a1aa503bb133a168367206467e64b094bfe9a7 |
| 0.2.0.4 negative fixtures only | 5b0747c0623c6783749e3b43ca13a36c9091b81744f95d0071d845aa96df59dc |

Native attempt: 64c8c10eb8654aa69d42ebee180c9e4b. Receipt in the matching ignored
msix-tests directory; log .artifacts/integrity-native.log. One authorized UAC
succeeded. completed=true; 19 checks true, four false; all_checks_passed=false and
release_qualified=false. The script selects partial exit 3; the invoking tool
reported a nonzero wrapper status (1). This is explicitly not a green release gate.

| Actual Windows integration check | Result in this attempt |
|---|---|
| Trusted SignTool chain / matching publisher / ordinary-user clean install | PASS |
| Packaged Core/DA and user-word probe | PASS |
| Manual .1 to .2 update | PASS |
| Settings, tabs/drafts, choices, learned words, pending handoff | PASS hashes and semantic readback; no uploads |
| Higher-version rollback procedure .2 to .3 with dirty drafts | PASS |
| Wrong identity/publisher, offline and malformed newer feeds | PASS rejected |
| Corrupt signature footprint .4 | PASS SignTool failure AND native rejection; .3 unchanged |
| Ordinary downgrade .3 to .1 | PASS rejected |
| Same-version .3 | PASS version unchanged |
| Corrupt payload source .4 | Full SignTool rejects; native deployment accepts .4, then ALL 441 installed block-map-listed files match signed original SHA-256/size |
| Uninstall | PASS registration absent; retained hashes and semantic check-dirty readback |
| Automatic update / installed desktop / installed relaunch | NOT RUN, deliberate isolated facet; three false flags |
| G05 native policy write/read/restore | BLOCKED 0x800706F7, fourth false flag; production stays read-only |
| Cleanup | PASS registration absent, CurrentUser signing key absent, temporary LocalMachine TrustedPeople leaf absent, no app process |

The payload outcome is accepted_verified_original_payload, not a claim that the
source archive is valid or that Windows rejected it. No altered payload was
launched. Original-block reuse remains an explanation consistent with Microsoft
documentation and byte equality, not a directly observed cache trace. Runtime
policy is manifest-validated and present in native installed fixtures; modifying
protected WindowsApps files to trigger runtime detection was not attempted.

## Reproduce and remaining acceptance

Run from the WordSuggestorWindows root, first prepare a NEW isolated series:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/prepare_msix_test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test_msix.ps1 -PreparedLocalTestSession <new-printed-session-directory> -SkipBuild -AllowTemporaryMachineTrust -SkipAutomaticUpdateProbe -SkipInstalledGuiProbe
```

The first command builds Core by default. The verified local run reused the prior
signed bundle's unchanged pinned Core CLI through SkipCoreBuild/CoreCliPath.
Never delete/reuse a prior nonempty LocalTest profile to rerun qualification;
keep its evidence and prepare a fresh nonce. Older fixtures without enforced
runtime integrity now fail before trust/install. Both Skip switches are LocalTest
only and leave checks false. Omit them for the separate full native run in a clean
account/VM; this attempt does not replace earlier installed GUI 55/automatic-update
results and does not freshly attest those lanes on the hardened manifest.

Next, verify the complete install/update/desktop flow together on the intended
Windows test setup and resolve the native G05 OS/API blocker. Human Narrator,
mixed DPI/multi-monitor, interrupted update and unknown-signer acceptance remain
open. Windows Hello positive tests remain user-deferred. Official signing,
canonical HTTPS feed and public beta publication are still absent. The backend
sharing receiver belongs to the macOS agent and real sharing stays source-gated.
No payment, macOS, Core, pack or backend protocol change follows from this work.
