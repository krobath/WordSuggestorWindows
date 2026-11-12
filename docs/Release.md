# Windows beta packaging and release

## Settings/manual update integration — 2026-11-12

The existing MSIX/App Installer release model is retained. Settings now validates
canonical feed metadata and OS association/availability before offering a separate
confirmed normal-close handoff to `ms-appinstaller`. Signing, publisher stability,
HTTPS publication and N→N+1 remain release gates. This slice does not publish,
change backend O-052, add an updater or qualify a signed beta.

In-app policy editing is source-gated read-only until native packaged change/read/
restore passes. Windows' existing configured automatic updates still run. Default
App Installer policy overrides can be replaced by the next App Installer update;
they are not a durable promise across releases. See
[Windows update contract and native fixture procedure](WindowsUpdates.md) and
[slice-8 evidence](qualification/WSA-UX-029_slice8.md).

Updated 2026-11-08, WSA-DX-004. Current output is a signing-ready MSIX test artifact;
it is not a signed public release. The user has confirmed that no signing service
is currently configured. A service is optional, but trusted package signing is
required for normal distribution. No private key or password belongs in this repo.

## Decision

| Model | Assessment |
|---|---|
| MSIX + App Installer | Selected: OS-owned signed package verification, installation transaction, repair/uninstall, protocol registration, separate beta identity, HTTPS metadata and background/on-launch checks. |
| Velopack direct distribution | Viable alternative where MSIX policy blocks deployment; needs its own signing, updater lifecycle and qualification. Current full-trust WPF/child-process requirements do not yet justify that additional flow. |

No homemade updater or unsigned production channel is implemented. This follows
[Microsoft's App Installer settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings)
and [Velopack's signing requirements](https://docs.velopack.io/packaging/signing).

## Build an artifact

From the Windows repo:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1
```

The existing bootstrap builds optimized Core; publish produces an x64
self-contained .NET 10 app, bundles Core/Swift runtime DLLs and resources plus the
Danish pack, derives tile assets from the existing product icon, and validates the
manifest/package with Windows SDK MakeAppx. Unique output directories are under
`.artifacts/release`. Generated binaries are ignored. `build-info.json` records
Core/pack SHA-256 identities; `makeappx.log` records SDK validation.

For a previously verified binary, use `-SkipCoreBuild -CoreCliPath <absolute-exe>`.
`-FrameworkDependent` is a development option, not the qualified standalone output.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_package.ps1 -AppDirectory <artifact-app-directory>
```

This copies the payload outside the source workspace and runs desktop smoke with
Swift/.NET SDK paths removed from the app environment. It tests the payload, not
MSIX installation, signature trust or updates. Keep these results distinct.

## Signing and channels

`WordSuggestor.Windows.Beta` is the beta package identity. A stable channel must use
a separate identity/feed and be qualified separately. Publisher must exactly match
the signing certificate subject. Signing uses a certificate already present in
the signing user's Windows certificate store, never an exported private key:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1 -Version 0.2.0.1 -Publisher '<certificate subject>' -CertificateThumbprint '<thumbprint>' -FeedBaseUri 'https://<release-host>/windows/beta/'
```

The script signs with SHA-256 and HTTPS timestamping, then runs SignTool trust
verification. It generates `.appinstaller` only after successful trusted signing.
Unsigned staging emits an explicit `.unsigned.msix` and no feed. HTTP, credentialed
URLs, query/fragment feed URLs and feed-without-signature requests are rejected.
No feed is published by these scripts.

Signed immutable versioned packages must be uploaded before atomically replacing
the feed. Protect release-host write access. Windows checks signatures and package
block hashes; a SHA file alone is not update authentication. Feed checks are
non-blocking on launch (six-hour interval) plus OS background checks. Start-menu
package activation participates; directly launching a loose exe bypasses that
installed-app lifecycle. Manual update:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\check_update.ps1 -AppInstallerUri 'https://<release-host>/windows/beta/WordSuggestor.Beta.appinstaller'
```

Do not force-close an editing session. Save documents, exit, update, then relaunch
from the Start menu. App Installer owns installation; user data is outside payload.

## Data, uninstall and rollback

Keep `%LOCALAPPDATA%\WordSuggestor` across upgrades. The manifest declares
`unvirtualizedResources` and filesystem virtualization rules so settings, packs and
Core user words remain at the established location and survive uninstall. On
Windows 11 the exclusion is scoped to that app-data folder; Windows 10 uses the
older filesystem declaration. Registry virtualization remains enabled; the OCR
protocol is also declared in the package manifest. Verify on a signed install.
See [Microsoft's virtualization contract](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization).

Settings remain schema 1 with atomic replacement/recovery backup. Older code must
not overwrite a future settings schema. Core protects unknown user-word schemas.
Never bundle user data in an MSIX. Saved text files remain where the user chose.

Stop a defective rollout by reverting/withdrawing its feed entry. Preferred
rollback is the last known-good payload republished with a higher four-part
package version and the same publisher/identity. Automatic downgrades are disabled
(`ForceUpdateFromAnyVersion=false`). Keep N-1 signed package/source hashes and data
backups; avoid destructive data migrations. Reinstalling an old version or merely
changing a download URL is not a tested rollback procedure.

## Required release-owner tests

On a disposable VM/test account with a trusted signing identity: install signed
N, save settings/add user word/save a document, install N+1, reopen and verify
data/shortcuts/OCR callback, close and relaunch, reject a tampered/untrusted package,
withdraw the feed release, republish known-good payload as N+2, verify data again,
uninstall and confirm intentional user-data retention. Also test offline feed,
interrupted installation, standard-user privileges and real 125/150/200% mixed-DPI
monitors. These gates are pending until a signing identity/feed and those test
environments are available. No signing trust store was modified during development.
