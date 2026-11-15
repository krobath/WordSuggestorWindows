# Windows EXE beta installation and updates

ID: WSA-RT-032_windows_exe_beta_distribution. Decision: 2026-11-15.

## Product decision and current status

The user has parked G05/App Installer policy investigation and prioritised an
ordinary installer and visual/functional comparison with macOS. Keep WPF,
WordSuggestorCore's CLI, the SQLite packs and existing features. Add an EXE
distribution profile to the existing packaging script. Do not rewrite the app or
change macOS, Core, pack format, backend contracts or payment/licensing behavior.

In practice, users run an installer, use WordSuggestor, and later receive an
offer to install a newer version. They can postpone it. Installation never
starts silently. G05 is specific to MSIX policy and is not an EXE beta gate.
The MSIX implementation and measured qualification history remain available.

Local preview install/upgrade/uninstall and updater contract tests pass. Public
EXE distribution is **not qualified**: approved production signing and a backend
contract extension are outstanding. There is no live EXE feed or production
upload. See the [backend handoff](handoffs/WSA-RT-032_exe_beta_backend_agent_prompt.md).

## Runtime, identity and storage

| Property | Direct beta | Local preview |
| --- | --- | --- |
| Installer | Inno Setup 7.1.1, per user, no normal administrator requirement | Same installer, unsigned and explicitly isolated |
| Minimum | Windows 11 build 22000, win-x64 payload | Same; tested on the current Windows 11 VM |
| Runtime | Self-contained .NET 10 WPF; bundled Swift/Core and da_DK SQLite | Same |
| Stable product ID | `WordSuggestor.Windows.Beta.Direct` | `WordSuggestor.Windows.Preview.<12hex>` |
| Program path | `%LOCALAPPDATA%\Programs\WordSuggestorBeta` | `%LOCALAPPDATA%\Programs\WordSuggestorPreview-<12hex>` |
| Data root | `%LOCALAPPDATA%\WordSuggestor` | `%LOCALAPPDATA%\WordSuggestor\LocalTests\<12hex>` |
| Public update checks | Explicit setting, default enabled | Disabled; no public feed requests |

`direct-install.json` in the program directory records distribution identity,
display version, four-part numeric version and publisher. It selects the runtime
route; it is not proof of signing. No MSIX package identity is assigned to an EXE.
The production publisher must be the approved certificate's exact subject DN and
must remain stable between releases. It has **not** been provisioned here.

Keep the established data layout: `settings/settings-v1.json`, editor drafts,
user words, learning and pending sharing under the existing data root. Bundled
packs remain under the program's `Packs`; installed/updated packs use
`%LOCALAPPDATA%\WordSuggestor\Packs`, with the existing legacy read fallback at
`%APPDATA%\WordSuggestor\Packs`. Diagnostics use `diagnostics/runtime` under the data root;
downloaded installers are under `updates`. Credentials keep the existing Windows
secure storage; the installer contains no credentials. Settings schema 9 gains
the optional `checkExeUpdatesAutomatically` field with default true; merge writes
and unknown-field preservation remain in place. No destructive migration.

Installer PE numeric FileVersion is explicitly the four-part package version;
the updater rejects an EXE whose embedded numeric version disagrees with the feed.
The display version is a separate textual product version, following Inno's
[version metadata contract](https://jrsoftware.org/inno-setup/documentation/topic_setup_versioninfoversion.htm).

The installer replaces program files and shortcuts, preserves AppData on upgrade
and uninstall, and refuses a lower numeric version. Roll back by rebuilding known
good code with a higher numeric version. Equal-version installer repair is
allowed; the updater only offers a strictly newer version. Closing/running-app
checks use a distribution mutex. The installer never forces app termination.
The app's existing dirty-document/save/cancel path owns update shutdown.

Production EXE registers the existing OCR callback only when no different HKCU
handler exists; uninstall removes it only while its command still belongs to
this installation. Preview never claims the protocol. Production OCR callback
registration is not natively qualified yet. MSIX protocol handling is unchanged.
Normal standard-user external writing restrictions remain; elevated targets are
not supported. Notepad is the qualified simple adapter, not a claim about all apps.

## Build and local installation

Run PowerShell from the `WordSuggestorWindows` repository. Existing bootstrap and
build prerequisites remain in [README](../README.md). No backend secrets are
needed for preview. The normal command builds Core; qualified cached Core may be
reused with the existing `-SkipCoreBuild -CoreCliPath <path>` options.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package_app.ps1 `
  -InstallerFormat Exe -LocalPreview -PreviewId 123456abcdef `
  -PackageVersion 0.2.0.1 -DisplayVersion 0.2.0-beta.1
```

Choose a fresh 12-character lowercase hexadecimal preview ID for each independent
test. Retain the same ID for N and N+1. Omit `-PreviewId` for a new random ID.
Open the returned `InstallerPath`, follow its normal wizard, and start from the
Start menu. The preview name includes its ID. It has its own drafts/settings and
does not replace the normal app. Unsigned preview is for local evaluation, not
an approved public installer. Do not import a test certificate to run it.

`bootstrap_installer.ps1` downloads a hash-pinned, Authenticode-verified compiler
from the vendor into ignored `.artifacts/tools`, using per-user installation. It
refuses to replace a separate existing Inno installation; pass
`-InstallerCompilerPath <ISCC.exe>` to use an explicitly chosen compiler instead.
Inno's [license](https://jrsoftware.org/files/is/license.txt) permits commercial
use subject to its terms; the vendor requests commercial users support development.
The compiler and installer binaries are not committed to this repository.

Public builds require an approved private-key certificate in CurrentUser\My;
keys/passwords must be provisioned securely outside git/chat. Use its thumbprint
and exact subject, not a local test identity:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package_app.ps1 `
  -InstallerFormat Exe -PackageVersion 0.2.0.3 -DisplayVersion 0.2.0-beta.3 `
  -CertificateThumbprint <approved-thumbprint> -Publisher <exact-subject-DN>
```

The script signs the application EXE, installer and uninstaller with SHA-256 and
an HTTPS timestamp. It verifies the final installer using SignTool
`verify /pa /all /v`, Windows trust, exact publisher and code-signing EKU checks
through the existing release verifier. Public unsigned/test-named builds and
active publication are rejected. Confirm approved signer trust and perform a
fresh signed install/upgrade before declaring production qualification.

## Update behavior and client contract

The production EXE checks after startup and every six hours when the user's
automatic-check preference is enabled. A prompt appears only when a normal
WordSuggestor editor or Settings window is active. Floating toolbar use never
brings up an unsolicited prompt over another application's typing. A version is
offered once per running session. Settings also supports manual checks regardless
of the automatic-check preference.

After explicit approval: recheck the offer; download the complete installer;
validate length, SHA-256, trusted Windows Authenticode signature and exact
publisher; ask the app to close normally; reverify while holding a read-only file
handle; launch the normal installer wizard. A changed offer needs new confirmation.
Cancellation, offline feed, malformed metadata, wrong publisher and failed trust
stop the handoff calmly. One-use, time-limited tickets prevent duplicate launches.

Planned fixed URLs (not currently deployed):

- Feed: `https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.update.json`
- EXE pattern: `https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta_<package_version>_x64.exe`
- Beta entry point: `https://wordsuggestor.com/beta`

The feed is a UTF-8 JSON object with **exactly** these ten keys, no duplicates:

```json
{
  "schema_version": 1,
  "platform": "windows",
  "architecture": "x64",
  "channel": "beta",
  "package_version": "0.2.0.3",
  "version": "0.2.0-beta.3",
  "filename": "WordSuggestor.Beta_0.2.0.3_x64.exe",
  "size_bytes": 123456,
  "sha256": "0000000000000000000000000000000000000000000000000000000000000000",
  "publisher": "CN=REPLACE_WITH_APPROVED_PRODUCTION_SUBJECT"
}
```

This is an illustrative client fixture, not publication metadata. Supply real
bytes/hash/publisher. No URLs, shell arguments, tokens, document text or account
data in the feed. Numeric version has four 0..65535 components without leading
zeroes. Size is 1..1 GiB, digest lowercase hex64; filename exactly derives from
numeric version. Feed bounded to 32 KiB; check timeout 15 seconds including body,
download timeout five minutes; redirects/cookies disabled. TLS remains normally
verified. The EXE's own trusted signature is required in addition to HTTPS/hash.

## Backend/publication boundary

O-052 at backend `2f67ddc` accepts Windows MSIX. Existing MSIX uploader and server
validation must keep rejecting EXE until the backend owner publishes a compatible
extension. Packaging therefore emits only `release-manifest.draft.json` with
`backend_contract_status=extension_required`; it is deliberately not named
`release-manifest.json`. It records artifact bytes/hash and signing attestation,
not an approved new backend schema. No EXE uploader or parallel endpoint is invented.

Next: the macOS/backend agent extends the existing catalog/publisher safely using
the [handoff prompt](handoffs/WSA-RT-032_exe_beta_backend_agent_prompt.md). Then
adapt the Windows uploader/manifest to that authoritative extension, run its
staging/pending/active gates, and verify HTTPS downloads. Existing pinned SSH host
key, fresh staging ID and validate/register/publish protections must be retained.
Activation must use package-first/feed-second/catalog-last. Withdrawal removes
the offer/link; retained versioned packages do not force updates. Reissue known
good code with a greater numeric version for rollback. Do not alter macOS
downloads/Sparkle or the existing `.appinstaller` feed.

## Validation and beta review

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_settings_app.ps1 -ExeUpdates
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_release_scripts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_app.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_settings_app.ps1 -Mode UpdateState
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_settings_app.ps1 -Mode Layout
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_exe_installer.ps1 `
  -PreviousBundleDirectory <N-bundle> -CandidateBundleDirectory <N1-bundle> `
  -AllowLocalPreviewInstallation
```

Run native installer qualification as a standard user with fresh isolated
preview IDs. It installs, probes bundled Core/DA and user words, upgrades, checks
settings retention, rejects downgrade and uninstalls while retaining test data.
`-WhatIf` makes no installation; public identities are rejected by this test.
Qualification JSON explicitly does not assert production release readiness.

See [the sprint and measured matrix](sprints/WSA-RT-032_windows_exe_beta_distribution.md)
for results and the human beta review. Native production signing, real HTTPS
notification/download/install, production OCR callback, multiple monitors,
physical DPI and screen-reader listening remain distinct qualification work.
Windows Hello remains user-deferred. None is silently counted as PASS.
