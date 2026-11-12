# Windows update Settings contract

WSA-UX-029 slice 8, 2026-11-12. Scope: General G04–G06. See
[qualification and manual matrix](qualification/WSA-UX-029_slice8.md).
Retain WPF/.NET 10, existing MSIX/App Installer and Core CLI/SQLite language packs.
This adds Windows adapters and orchestration; no backend protocol/ranking changes.

## Authority, package and runtime

`WindowsUpdateService` obtains the current package identity/version from Windows,
not an editable manifest file. Loose developer builds have no installed authority.
The canonical beta package name is `WordSuggestor.Windows.Beta`; publisher is the
actual running package's publisher, which must match feed metadata. This slice
neither provisions a production identity nor changes the manifest/release model.

Both WPF projects target `net10.0-windows10.0.22621.0`, with
`SupportedOSPlatformVersion=10.0.19041.0` and official managed Windows SDK
`10.0.22621.57` pinned in projects and `dependencies.lock.json`. This provides
typed WinRT projections. No shell/PowerShell process performs product mutations.
The existing normal `bin/<Configuration>/net10.0-windows` layout remains stable;
`--artifacts-path` uses the SDK layout. Existing build/package scripts are reused.
Compile SDK is not a new Windows minimum: the app manifest minimum remains 19041.
Policy mutation API requires Windows 11 build 22000; Windows 10 behavior and
minimum-version execution remain untested. Release runtime remains self-contained
x64 .NET 10. Normal usage/tests must not require elevation.

`WindowsNativeUpdatePlatform` reacquires the running package via
`PackageManager.FindPackageForUser` before `CheckUpdateAvailabilityAsync` because
Microsoft documents an access-denied issue with direct `Package.Current` use.
The immutable snapshot contains identity/source/versions, on-launch/background,
interval, prompt/block/downgrade/repair flags, policy priority and URI collections.
Unknown is not false; mixed scheduling flags render indeterminate. The UI never
persists a pretend effective automatic-update boolean in JSON.

Production editing is explicitly **read-only** through the source-level
`UpdatePolicyQualification.ProductionWriteQualified=false` gate. There is no
environment/user-setting bypass. Existing Windows automatic scheduling continues.
The flag may change only in a reviewed code release after positive ordinary-user
packaged read/change/read/restore, pause/system-policy and failure acceptance.

## Policy change path and limitations

The implemented native setter captures the original OS policy, checks package,
source and snapshot freshness, clones `AutoUpdateSettingsOptions`, changes only
`OnLaunch`/`AutomaticBackgroundTask`, then independently rereads and verifies.
Failed/cancelled attempted writes restore and verify the original settings;
uncertain restoration reports `restore_failed`, never success. Coordinator and
UI reread actual state; checkbox click, Space and accessibility Toggle share the
same path and requested values are not displayed as saved values prematurely.

Only `AppInstallerManager.GetDefault()` is used. System-managed policy is read-only;
no elevation or `GetForSystem` workaround exists. Microsoft documents that Default
overrides can be replaced by a subsequent App Installer update. The app therefore
does not promise durable opt-out across package/feed updates. Pause is not exposed
by the selected typed `AppInstallerInfo`; the outer qualification script checks
the full OS snapshot and rejects paused fixtures. Those limitations are reasons
to keep production editing gated. Snapshot comparison is not an OS-wide atomic
compare-and-swap against other processes. Rollback failure stays explicit.

## Manual check and installer handoff

Canonical source:
`https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta.appinstaller`.
Versioned MSIX stays under the same directory with a safe `.msix` filename. There
is no alternative production feed or direct-download execution path here.

`WindowsUpdateService` checks bounded public metadata: HTTPS, no redirects/cookies,
8-second timeout, maximum 128 KiB, DTD disabled, fixed origin/source, exact current
publisher/name/x64, canonical four-part version and matching root/package version.
Only one MainPackage plus optional unambiguous UpdateSettings are accepted; related
payloads, downgrade/activation blocking and duplicate settings are rejected.
The app never treats XML inspection as package signature verification.

`WindowsUpdateCoordinator` serializes operations, cancels on dispose and rejects
late results. An offer requires both metadata and OS availability to agree. Offline,
invalid, missing feed, wrong association and disagreement remain non-destructive
status states. Checking alone does not launch or close anything.

The separate installer action asks a native confirmation (default No), rechecks
feed and OS availability, then creates an immutable two-minute single-use ticket.
`WindowsUpdateHandoff` closes the owning main window through the normal existing
save/discard/cancel path. Cancel or wrong owner aborts. Only the main window's
`Closed` event can open the fixed `ms-appinstaller:?source=<canonical HTTPS>` URI.
No forced kill, arbitrary command/URI, self-installation or process restart is
used. App Installer owns signature/trust checking, installation and update behavior.
Expired tickets or launch failure show bounded localized messages.

## Native fixture qualification

Do not bypass trust or test against the production package. Obtain an already
installed, separately trusted isolated candidate fixture from the existing local
release workflow, with:

- Name `WordSuggestor.Windows.LocalTest.<8–32 lowercase hex nonce>`.
- Publisher `CN=WordSuggestor Local Test <same nonce>`; family from Windows.
- Application ID `App`, x64, this candidate binary and an isolated App Installer
  association; ordinary-user activation and original policy captured externally.

LocalTest packaging/trust helpers were already uncommitted work before this slice
and are excluded from this commit. A clean checkout of this slice does not claim
to provision that fixture. No new parallel packaging or trust flow is introduced.
Fixture provisioning is an outstanding prerequisite. Never enable Developer Mode,
change machine policy or add root trust automatically to force qualification.

From the Windows repository root after building smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_settings_platform.ps1 -PackagedUpdateProbe -TestPackageFamilyName '<installed isolated family>'
```

The script verifies exact package/publisher, rejects elevation/pause, captures the
full native snapshot, and COM-activates only `<isolated family>!App` with a random
run ID and `--qualify-update-policy`. The app rejects production/loose context,
changes only its own fixture flags, reads back and finally restores exact original
flags, including initially mixed states. Results are bounded identity/version/
digest/status metadata at `%LOCALAPPDATA%\WordSuggestor\LocalTests\<nonce>\diagnostics\update-policy-<runId>.json`.
The runner checks schema, run ID, packaged context and outer before/after fingerprint
before PASS. Exit 0 = qualified fixture roundtrip, 3 = blocked, 1/exception = failure;
unknown, timeout or failed restoration must not qualify production editing.
The smoke `UpdateNative` mode is a prerequisite reminder returning 3, not the
packaged mutation test. Reports never unlock the production source flag.

## Data, security and release ownership

Scheduling belongs to Windows, not roaming/local Settings JSON. Existing user data,
Credential Manager account storage, local learning, user words/packs and log paths
are unchanged. Diagnostics use status codes/HRESULTs, not raw errors, document
content, tokens or signing material. UI tests use synthetic adapters and launchers.
No automatic Hello prompt is introduced; user deferred hardware acceptance.

Keep stable beta name/publisher and strictly increasing numeric package versions.
Use the existing [release runbook](Release.md), backend O-052 and production trust
requirements. MSIX signature verification, real clean install/N→N+1/data retention,
invalid-signature refusal, relaunch, feed publication and rollback remain independent
release gates. Rollback rebuilds known-good code with a higher package number.
No unsigned production update or public test version is authorized by this slice.

## Primary platform references

- [SetAutoUpdateSettings and API/platform requirements](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.setautoupdatesettings).
- [Default policy priority and later App Installer replacement](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.getdefault).
- [Package update availability and reacquisition requirement](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.package.checkupdateavailabilityasync).
- [App Installer update settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings).

The gated design is this repository's implementation decision. API documentation
does not prove packaged authority on this machine; only native acceptance can do so.
