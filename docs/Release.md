# Windows beta packaging and release

## Selected beta route: per-user EXE (2026-11-15)

The user selected conventional installation with explicit update prompts. Follow
[EXE release/signing/runbook](ExeBetaRelease.md); keep using package_app.ps1 with
-InstallerFormat Exe. MSIX remains available and is still the script's default.
The G05 investigation below is parked and is not an EXE release requirement.

Local unsigned isolated preview is qualified for evaluation, not publication.
Production app/setup/uninstaller require approved signing. The EXE metadata is
only release-manifest.draft.json: backend O-052 still accepts MSIX. Do not pass it
to the old uploader or bypass its rejection. The [backend handoff](handoffs/WSA-RT-032_exe_beta_backend_agent_prompt.md)
requests a compatible extension; align uploader and run real HTTPS/install gates
only after that contract is authoritative. No upload or public feed was changed.
The dated release records below retain their narrower MSIX evidence.

---

Historical dated evidence follows.

## G05 Settings page verification (2026-11-14)

The operator reached Installed apps; the old nonce-anywhere check was a false
positive for app details. List pages are now rejected; details need header/spec/
version proof. A guarded in-package launcher replaces the unreliable external URI.
Installed own-package launch now verifies app_details and its update control.
Build/app105/UpdateState38 retain their recorded scope. Release scripts now pass
199 checks, including read-only API metadata and explicit pause-state diagnostics.
The [manual control experiment](qualification/WSA-UX-029_g05_manual_control_roundtrip.md)
now verifies Off/Off BEFORE reopening and On/On AFTER reopening. Native values
were unchanged in both phases, with on-launch/background still true. Cause unknown;
The [passive observation](qualification/WSA-UX-029_g05_passive_observation.md)
completed: four Off/Off samples over 15.74 seconds, then On/On after HUMAN Settings
navigation; native values unchanged throughout. No package reactivation by the observer.
The used command is retired. The [API semantics audit](qualification/WSA-UX-029_g05_api_semantics.md)
now verifies public pause metadata and documents the separate CSP Disable contract.
Actual Disable state/save cause remain unqualified; no new GUI run or product write.
G05 remains read-only. See [measured read-only acceptance](qualification/WSA-UX-029_g05_packaged_settings_page.md).


## G05 Settings window correction (2026-11-14)

The probe admitted IntPtr.Zero and assumed Settings owned the top-level window.
Both are corrected; a real hosted Settings window and 769 UIA nodes read without
error on this VM. Release scripts: 151 PASS. The specific app page still needs its
fresh UI-only check; G05's separate 0x800706F7 remains unqualified/read-only.
See [reproduction, live proof and fresh test](qualification/WSA-UX-029_g05_hosted_window_fix.md).


## G05 measured input/schema result (2026-11-14)

The operator completed the isolated run: all 21 input attempts still reject with
0x800706F7, across baseline/capabilities and schemas 2018/2021. Original policy
and cleanup verified. Settings UI remains unqualified; diagnostic labels/error
metadata and a read-only UI-only follow-up are added. G05 stays read-only.
See [measured matrix and next test](qualification/WSA-UX-029_g05_input_schema_acceptance.md).


## G05 input/schema investigation (2026-11-13)

Seven native input controls, same-identity schema comparison and a read-only
Settings probe are implemented. Build/app105/UpdateState35/release135 pass.
The native attempt stopped at UAC BEFORE installation; the new comparisons are
NOT RUN and G05 remains unqualified/read-only. No VM cause is established.
See [exact evidence, research and continuation](qualification/WSA-UX-029_g05_input_schema_diagnosis.md).


## G05 isolated native diagnosis (2026-11-13)

Both installed baseline and capability variant fail at write_policy/0x800706F7;
Windows cmdlet, PowerShell WinRT and native C++ controls fail identically,
including factory/manual no-op and 0/6-hour comparisons. All
settings remain unchanged/restored and temporary trust/install cleaned. Broad
capabilities alone do not solve it, so production rights/template stay unchanged.
New diagnostic-only guards and actual off/on proof requirements are implemented;
build, app105, UpdateState31 and release131 pass. G05 stays read-only. The host is current GA Windows 11 26H2, not an
established Insider/VM fault; native/data diagnosis continues on this same VM.
See [measured diagnosis and exact isolated procedure](qualification/WSA-UX-029_g05_native_diagnosis.md).
Earlier acceptance records retain their scope; no G05/public release PASS implied.


## Measured native installer and GUI acceptance (2026-11-13)

The default combined user run completed: 22/24 native checks and all 56 installed
GUI checks pass. Installer dialog detection/clearance, real editor/Notepad,
manual update, dirty rollback, integrity negatives and uninstall data retention
are verified together. Cleanup independently confirmed. Exit 3 remains correct:
automatic update required dialog handling; G05 still returns 0x800706F7/read-only.
No unchanged rerun is needed now; investigate those two native checks next.
Production signing/HTTPS/public release and remaining human gates stay open;
Hello user-deferred. See [exact evidence and next work](qualification/WSA-UX-029_slice14_installer_dialog_acceptance.md).
Earlier dated sections below retain their historical scope.


## Package integrity follow-up (2026-11-13)

Current Settings and fresh build pass (0 warnings/errors); the reported
OpenInstaller call was already removed by 92f2ed5. New MSIXs enforce Windows
runtime content integrity. Native testing now proves corrupt-signature rejection,
older/same-version behavior and data preservation after uninstall. A payload-only
corrupt source was accepted, but all 441 installed content files matched the
trusted original; registration alone never qualifies integrity. Release tests: 94.
G05 stays blocked; this run deliberately skips GUI/automatic update. Full release
remains unqualified, Hello user-deferred, production signer/feed absent.
See [exact evidence and next steps](qualification/WSA-UX-029_slice14_package_integrity.md).
Earlier dated sections below preserve historical, narrower results.



Final software/desktop qualification and the updated local signing series are
tracked in [slice 15](qualification/WSA-UX-029_slice15.md). Use
[FinalAcceptance.md](FinalAcceptance.md) for the remaining installed/human gates.
These results do not provision a production signing identity or publish a feed.


Updated 2026-11-13, WSA-RT-017_windows_beta_publication_tooling.
This runbook describes implemented local tooling. Production signing, native
installation/update and public publication are not qualified by the cleanup.
See [measured results and remaining gates](qualification/WSA-RT-017_release_cleanup.md).

## Architecture and platform

Retain .NET 10 WPF, the versioned JSON Core CLI and unchanged SQLite packs. MSIX +
App Installer remains the selected distribution model: Windows owns signature
trust, transactional installation and update lifecycle. Direct distribution with
Velopack would require separate updater qualification; no concrete MSIX failure
justifies that migration. No custom unsigned production updater is introduced.

The x64 package declares Windows Desktop >=10.0.19041 (Windows 10 2004), with
Windows 11 also targeted. This is a technical manifest minimum, not a claim that
all those OS versions received hardware/manual acceptance. Publish is self-contained
win-x64 .NET 10; recipients do not install Swift or a .NET SDK. Build prerequisites
and pinned dependencies remain in README/global.json/dependencies.lock.json.
Installation is per-user. Normal app use and trusted installation need no admin;
enterprise sideloading/App Installer policy may require an administrator to enable
distribution. Never run the editor elevated to work around an unsupported target.

## Identity, versioning and data

- Beta identity: `WordSuggestor.Windows.Beta`; application ID `App`.
- Publisher: exact certificate Subject supplied to `-Publisher`. The template
  `CN=WordSuggestor` is not evidence of a provisioned production identity. Select
  and retain the approved publisher before the first public package.
- Separate display version, e.g. `0.2.0-beta.1`, build number `2001`, and four-part
  numeric package version `0.2.0.1`. Every published update/rollback increases the
  latter; do not reuse or overwrite historical packages.
- `%LOCALAPPDATA%\WordSuggestor`: settings/settings-v1.json (current schema 9),
  editor/workspace-v1.json (drafts/schema 1 + backup), Packs (installed packs),
  UserWords (Core-owned overlay), learning and diagnostics. Bundled Core/Packs stay
  inside the read-only payload; installed packs and user changes stay outside it.
- Existing atomic settings migrations/backups and future-schema protections remain.
  Credentials stay in the existing Windows secure credential mechanism; no tokens
  or keys are in release metadata. Drafts are local content, not diagnostic logs.
- MSIX unvirtualizedResources/filesystem exclusions retain established local data
  over upgrades/uninstall; verify this on an actual signed install. Text files
  remain at user-chosen paths. Installed LocalTest identities use their own
  LocalTests/<nonce> root and package-scoped single-instance coordination.

## Build, signing and package verification

Run from `WordSuggestorWindows`. No production secrets are needed for development:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_qualification.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_run_scripts.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_release_scripts.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_clean_checkout.ps1
powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1
```

The last command builds an **unsigned** staging MSIX and emits neither feed nor
manifest. `-SkipCoreBuild -CoreCliPath <verified-cli>` reuses a verified Core;
`-FrameworkDependent` is development-only. Unique ignored `.artifacts/release`
directories contain payload, MakeAppx log and build-info.json with Core/pack hashes.
Never commit binaries. Isolated payload smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_package.ps1 -AppDirectory <output-app-directory>
```

This copies outside the workspace and removes source/SDK discovery from the app
environment. It proves payload execution, not installed identity/trust/update.
Installed apps now resolve only bundled Core, never a sibling source checkout.

Provision the approved non-exportable code-signing identity with private-key
access in the signing user's Windows Certificate Store through the organization's
secure process. Keep passwords/PFX/private keys out of chat, source and logs. The
current script uses SignTool `/sha1` against CurrentUser\My; a remote signing
service needs a reviewed adapter rather than pretending a local certificate exists.
The chain must be trusted on recipients, and the Subject must match the stable
publisher. Example, replacing placeholders locally:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1 -PackageVersion 0.2.0.1 -DisplayVersion 0.2.0-beta.1 -BuildNumber 2001 -Publisher '<approved Subject>' -CertificateThumbprint '<store thumbprint>'
```

SignTool signs SHA-256 with HTTPS timestamping, then `verify /pa /all /v` and Windows
Authenticode trust must both pass. Publisher equality and certificate SHA-256 are
recorded as the authorized publisher attestation. Linux does not revalidate the
Windows trust chain. Only trusted signing produces the three-file bundle. Wrong
identity/publisher, invalid trust, hashes or feed policy abort before upload.

## App Installer and backend contract

Authoritative server contract is read-only O-052 in WordSuggestorBackend at
`2f67ddc6f5382b286e3039677a05677fe4d7d16f` and its Windows x64 example.
No alternate endpoint/schema/catalog is introduced.

Canonical feed:
`https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.appinstaller`

Package example:
`https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta_0.2.0.1_x64.msix`

App Installer 2018 has one MainPackage and UpdateSettings, matching identity,
publisher/x64/package version and exact canonical HTTPS paths. OnLaunch checks
every six hours (`ShowPrompt=true`, `UpdateBlocksActivation=false`), plus
AutomaticBackgroundTask; ForceUpdateFromAnyVersion=false. Extra/duplicate policy
or dependency elements are rejected. OS scheduling/network policy affects timing.
Start-menu activation participates in installation/update lifecycle; loose exe
launch is not automatic-update evidence.

Settings manual check validates feed/association before a confirmed normal-close
handoff to `ms-appinstaller`. Save/cancel is preserved. Offline errors are calm.
The G05 in-app policy editor remains read-only until its separate trusted fixture
qualification passes; existing OS automatic scheduling is unaffected. See
[WindowsUpdates.md](WindowsUpdates.md). Direct operator check:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\check_update.ps1 -AppInstallerUri 'https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.appinstaller'
```

`release-manifest.json` uses schema_version=1 and a nested release containing
platform/windows, architecture/x64, channel/beta, version, build_number,
package_version, package_type/msix, status, minimum_os, status_message,
release_notes, known_issues, artifact/update_feed filename/size_bytes/sha256 and
signing publisher/identity/signature_verified/verification_result/verified_at/
verification_tool. No URLs, server paths or secrets are chosen in this manifest.
Exactly three files belong in the bundle: manifest, versioned MSIX, canonical
App Installer filename. The backend's original portable validator is SHA-256
pinned in `tests/verify_backend_manifest.py`; Python 3 and the pinned backend blob
are test prerequisites. Tests export it read-only from the sibling backend repo
if no cache exists. Offline: place that exact blob at
`.artifacts/backend-reference/2f67ddc/scripts/beta_release_publish.py`; a different
hash fails, and missing reference fails rather than silently skipping validation.

## Native test qualification without production credentials

Prepare reviewable signed test artifacts first, without installation/device trust:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\prepare_msix_test.ps1 -WhatIf
powershell -ExecutionPolicy Bypass -File scripts\prepare_msix_test.ps1
```

Preparation reuses package_app and emits N=0.2.0.1, N+1=.2, rollback=.3 and a
newer .4 rejection-only package with one
isolated identity, bundled Core/pack and an ephemeral seven-day non-exportable
key. Finally the private key is deleted. Only the public certificate remains.
The bundles are signed but untrusted, signature_verified=false. They cannot pass
normal trusted verification or the production uploader. No PFX/password is used.

After explicit operator approval on the disposable test environment:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_msix.ps1 -PreparedLocalTestSession '<printed prepared-session directory>' -SkipBuild -AllowTemporaryMachineTrust
```

Old three-package prepared receipts must be regenerated; the native script rejects
them before trust/install. A new negative feed must test a genuinely newer package
and have its own self-URI, rather than exercising an installed-version no-op.

Build the smoke runner first if needed using build_app.ps1. Test trust is a
machine-wide leaf exception even though the signer/installation is isolated.
Microsoft documents the App Installer LocalMachine TrustedPeople requirement in
[its troubleshooting guide](https://learn.microsoft.com/en-us/windows/msix/app-installer/troubleshoot-appinstaller-issues).
Never import this certificate as a root. The harness verifies actual trust before
updating local attestations, checks N to N+1/extended stores/dirty recovery and .3
rollback, and records any false native automatic-update/policy gate as exit 3.
Software probes alone cannot enable G05 or qualify an official release.
See [the measured preparation result](qualification/WSA-UX-029_slice14.md).

Use a disposable ordinary-user test account/VM with an active unlocked desktop.
Without trusted signing, the explicit local fixture procedure is:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_msix.ps1 -LocalTestIdentity -AllowTemporaryMachineTrust
```

Default invocation exits **3 BLOCKED** without touching certificates/packages.
The explicit switch authorizes the local harness to create a non-exportable
seven-day test signing key, elevate only a leaf-trust helper and temporarily add
its public non-CA code-signing certificate to LocalMachine\TrustedPeople.
It never adds a root. Helper TTL/cleanup removes only trust it added; the caller
removes its own test key. Record/check cleanup even on interruption. Do not run
this on an ordinary development machine merely to clean Git status.

Identity `WordSuggestor.Windows.LocalTest.<12 lowercase hex>` and exact matching
`CN=WordSuggestor Local Test <same nonce>` use local file feeds, isolated user data,
and cannot pass the production uploader. Existing installations/nonempty fixture
data are rejected; cleanup uninstalls only a package this run actually installed.
Qualification probes write only new reserved diagnostics/qualification-<guid>.json
files. No raw exceptions/document text enter qualification records.

For trusted production-identity bundles in a clean test account:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_msix.ps1 -PreviousBundleDirectory <signed-N-bundle> -CandidateBundleDirectory <signed-N1-bundle> -NegativeBundleDirectory <signed-newer-rejection-bundle>
```

The harness attempts clean ordinary-user install, packaged Core/DA + user word,
automatic and manual N-to-N+1 update, settings/pack/user-word hash retention,
installed relaunch/desktop smoke, offline/invalid/wrong identity/publisher,
signature-footprint/older-version rejection, same-version unchanged and uninstall retention.
A separate payload-corruption case requires native rejection or exact SHA-256/size
equality of every installed block-map-listed file against the trusted original,
before any launch. A corrupt source archive still fails the production uploader.
New bundles must declare uap10:PackageIntegrity/Content Enforcement=on; old signed
fixtures without it must be rebuilt. Runtime detection by modifying WindowsApps
files is not claimed. See the current evidence linked above.
Same-version reinstall is not described as a Windows rejection. Native evidence
is the ignored msix-qualification.json, not mocks. Additionally qualify editor
draft/session retention across the actual update, unknown signer rejection,
interrupted update and supported OS/DPI/accessibility hardware manually. A signed
candidate's payload can be rechecked with `test_release_candidate.ps1`.
Hello PIN/biometric acceptance stays user-deferred; software mocks do not close it.

The complete harness may pause with ACTION MSIX for up to 120s while an App
Installer window is visible. Cancel the WordSuggestor fixture dialog; the test
never clicks consent/update buttons. Version observation with any such dialog
stays automatic_update=false; manual qualification continues only after stable
idle. GUI MSIX starting marks the hands-off phase. EnvironmentBlocked.AppInstaller
stops before GUI; NotepadForegroundUnavailable distinguishes denied focus from an
unsupported native edit target. Per-boundary fixed metadata is in the qualification
JSON. See the newest procedure above; do not rerun a used nonce.

## Private staging, pending registration and activation

Configure the approved SSH host/user, key filename, server backend checkout path,
dedicated known_hosts file and independently verified ED25519 SHA256 fingerprint
in the operator session/secure configuration. No guessed host key, TOFU, passwords
or embedded server deployment paths. Example configuration uses local variables:

```powershell
$transport = @{
    HostName = $env:WS_RELEASE_HOST
    UserName = $env:WS_RELEASE_USER
    KnownHostsFile = $env:WS_RELEASE_KNOWN_HOSTS
    HostKeyFingerprint = $env:WS_RELEASE_ED25519_PIN
    IdentityFile = $env:WS_RELEASE_SSH_KEY_PATH
    BackendDirectory = $env:WS_RELEASE_BACKEND_DIRECTORY
}
& .\scripts\publish_beta_release.ps1 @transport -BundleDirectory $bundle -WhatIf
& .\scripts\publish_beta_release.ps1 @transport -BundleDirectory $bundle -Action Validate
& .\scripts\publish_beta_release.ps1 @transport -BundleDirectory $bundle -Action RegisterPending
```

`$bundle` is the reviewed three-file signed pending bundle directory. WhatIf checks
structure, hashes and pin configuration, previews exactly three files and a fresh
ID, and makes no network connection/signature claim. Real actions always verify
SignTool/trust, identity, feed, hashes and bounded metadata before SSH. Strict
ED25519 host-key checking, BatchMode, no forwarding/password prompts, direct
process arguments, bounded output/deadlines and sanitized failures are enforced.

Every attempt creates a new UUID staging ID under
`/data/docker_data/volumes/wordsuggestorbackend_beta_staging/<id>.uploading`, uploads
exactly the three files, atomically renames to `<id>`, then runs the existing
`./scripts/publish_beta_release_on_docker01.sh validate --staging-id '<id>'` and
optionally `register` or `publish`. No direct public-directory writes. A pending
row has no public download link. Exit codes 2–9 are checked against structured
server JSON; SSH 255, malformed/contradictory replies and wrong targets stop.
Partial/failed staging remains private and is never reused. After timeout inspect
server state before retry; an already dispatched remote operation may finish.

Only after exact candidate qualification and content review, change that bundle's
manifest status to active (retain the signed MSIX/feed bytes), then:

```powershell
& .\scripts\publish_beta_release.ps1 @transport -BundleDirectory $bundle -Action Publish -QualificationPath $qualification
```

The attested report must match exact identity/publisher/version/package/feed hashes
and all required true gates. The uploader rechecks the actual extracted payload,
requires increasing public version, and tests Mac beta DMG links/signed appcast
before/after. `-FirstRelease` permits only HTTP 404 for a nonexistent Windows feed,
never an unavailable server or mismatched existing feed. Publish uses a fresh ID
and the server owns package-first/feed-second/catalog-last. It verifies canonical
result URLs and downloads/hashes both public artifacts afterward. Review /beta
Windows x64 link and actual App Installer activation as final human acceptance.
No public upload/publication or live Mac regression was performed in this cleanup.

## Withdrawal, feed stop and rollback

Through the existing approved server deployment/SSH process, in its backend
checkout (these are server commands, not a new Windows transport path):

```sh
./scripts/publish_beta_release_on_docker01.sh set-status --platform windows --architecture x64 --channel beta --status withdrawn --status-message 'Release withdrawn while incident is reviewed.'
./scripts/publish_beta_release_on_docker01.sh disable-feed --platform windows --architecture x64 --channel beta
./scripts/publish_beta_release_on_docker01.sh restore-feed --platform windows --architecture x64 --channel beta --backup-id '<reviewed feed backup id>'
```

Withdrawal removes the page link but does not stop already-associated Windows
clients. Disable-feed is separate. Review server history before restore. Preserve
immutable packages, user data and other targets. Binary rollback builds known-good
code with a higher numeric version and the same identity/publisher, then repeats
qualification and normal publication. Never force a downgrade/delete history.
Backend deployment/rollback stays in the macOS/backend runbook and is unchanged.

## Troubleshooting and release limitations

Use a short checkout path for self-contained packaging. A deeply nested QA export
can exceed Windows/NuGet extraction path limits for runtime satellite DLLs and
then fail publish with MSB3030. Package the identical reviewed source at a shorter
checkout path; do not omit runtime resources or accept a partial publish. The
cleanup observed this failure, then verified the complete payload from the normal
repository path with the same source and already built optimized Core.

Manifest/hash/policy failures stop locally; rebuild/review metadata rather than
bypassing validators. Signature failures require the approved signer and trusted
chain, not a new publisher or an unsigned feed. Host-key failures require an
out-of-band pin review. Failed staging needs a fresh ID; retain sanitized result
code for the server owner. A post-publication hash/Mac failure requires feed stop
and withdrawal through the existing server controls.

Only Notepad external insertion is currently end-to-end qualified. Word/Outlook,
browser fields/Google Docs, elevated targets, mixed-DPI hardware and human screen
reader review retain their documented limits. Release gates still require native
signed installation/update, real trusted identity and approved SSH configuration.
No payment/licensing changes or macOS/Core/backend source edits are part of this
Windows cleanup. Preparation has not changed certificate trust or published a feed.

## Isolated G05 capability diagnostic

Use prepare_msix_test -UpdatePolicyCapabilityExperiment and test_msix
-UpdatePolicyDiagnostic only for the separate comparison described in the newest
qualification link above. The package_app -TestUpdatePolicyCapabilities switch
rejects production context. Marked experiment receipts cannot enter the default
suite. Diagnostic completion always retains all_checks_passed=false and
release_qualified=false. Neither added capabilities nor a successful fake adapter
can enable the product G05 write gate. Off AND on readback plus original policy
restoration are now required for any future native pass; legacy one-step evidence
is insufficient. The measured 26300.9974 host fails all four write routes.

For the native C++ control, add -BuildUpdatePolicyNativeControl to the marked
preparation command (VS2022 C++/SDK prerequisites). Its helper is prohibited in
production packaging and refuses production/unpackaged/elevated callers at runtime.
-UpdatePolicyDiagnosticHours 6 tests the release interval without changing the
normal full-suite timing or feed. Only diagnostic mode accepts this parameter.
Current Windows 11 26H2/build 26300.9974 is GA; no VM replacement is a prerequisite.
All measured write/no-op routes still fail, so G05 remains unqualified/read-only.
