# WSA-UX-029: read-only update API and pause semantics audit

Date: 2026-11-14, Europe/Copenhagen. Implementation and live metadata audit PASS;
G05/native write failure NOT resolved. No installation, trust, UI interaction,
package enumeration, policy mutation or production change by the audit.

## Concrete findings

The [completed passive test](WSA-UX-029_g05_passive_observation.md) proved visual
Off was stable during the sampled interval and On was seen after human Settings
navigation, with unchanged native flags. It did not establish the meaning of
every disable/pause mechanism. Subsequent read-only metadata inspection shows:

| Surface on this Windows host | Observed contract |
|---|---|
| AppInstallerInfo | OnLaunch, AutomaticBackgroundTask, PausedUntil, PolicySource present |
| AutoUpdateSettingsOptions | scheduling properties present; PausedUntil absent |
| Both types, specifically tested names | Disable, Disabled and IsDisabled absent |
| AppInstallerManager | default/system, Set, Pause and Clear methods present |
| Local Get cmdlet | PackageFamilyName present; PackageFullName absent |
| Local Set cmdlet | PauseUpdates, HoursToPause, UseSystemPolicySource present |
| Local Get cmdlet output model | PausedUntil and PolicySource properties present; tested disable names absent |

This checks specific metadata names, not all Windows internal mechanisms or
authority to call a method. The output-model reflection is bounded to the locally
installed cmdlet's AppInstallerInformation type; missing/renamed shape is reported
as unavailable, not interpreted as absent state. No cmdlet was executed by the
audit; command discovery/reflection does not query any installed app's settings.

The five relevant IDL interfaces (IAppInstallerManager, its statics,
IAutoUpdateSettingsOptions, its statics and IAppInstallerInfo2) have identical
UUIDs and normalized declarations in the installed 10.0.22621.0 and 10.0.26100.0
SDK headers. This is static contract evidence, not a native runtime fix or a proof
against every SDK/linker/marshalling issue.

## Authoritative semantics and documentation differences

Microsoft's [EnterpriseModernAppManagement CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/enterprisemodernappmanagement-csp#deviceappmanagementsystempackagefamilynameappupdatesettingsautoupdatesettingsdisable)
documents a device-scoped System package AutoUpdateSettings/Disable boolean,
separate from OnLaunchUpdateCheck and AutomaticBackgroundTask. This management
setting does NOT prove that Windows Settings uses that node, that this machine
has such a policy, or that an ordinary application should write it. No MDM/CSP
query or write is performed here; its actual value is NOT MEASURED.

[AppInstallerInfo.PausedUntil](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appinstallerinfo.pauseduntil?view=winrt-26100)
is a nullable DateTimeOffset: null means not paused; otherwise it gives the
resume time. Pause is not a permanent Disable boolean. The documented
[AppInstallerInfo](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appinstallerinfo?view=winrt-26100)
and [options](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.autoupdatesettingsoptions?view=winrt-26100)
properties align with the specifically audited public names on this host.

The [Get cmdlet page](https://learn.microsoft.com/en-us/powershell/module/appx/get-appxpackageautoupdatesettings?view=windowsserver2025-ps)
currently shows PackageFullName. Local command metadata instead exposes
PackageFamilyName for filtering. Existing Windows scripts use the actual local parameter;
that discrepancy is not the cause of the measured successful reads.

[GetForSystem](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.appinstallermanager.getforsystem?view=winrt-26100)
requires administrator rights and applies stronger priority. Method presence is
not permission or justification for replacing the ordinary-user product route.
The inspected policies reported Default; that is not a full audit of all global
or device-management policy. No elevation/priority override, Pause or Clear call.

## Implemented diagnostics

`test_settings_platform.ps1 -UpdatePolicyApiAudit` runs only metadata inspection
and exits before the existing Hello/package/window probes. Combining those modes
or supplying a package/runner is rejected. Example from the Windows repo root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_settings_platform.ps1 `
  -UpdatePolicyApiAudit -OutputDirectory .\.artifacts\g05-api-audit
```

The fixed schema records type/property/method/parameter availability and cmdlet
output-shape availability. Missing API/inspection failures remain unqualified with
fixed category/HRESULT; no raw error text, app paths, package identities, URIs,
dates or user content. Live run returned api_metadata_only, read_only=true,
g05_qualified=false. Exit 0 means metadata inspection completed, not G05 passed.

Safe native summaries now include a `pause` object with field_present and state:

| Snapshot PausedUntil value | Diagnostic state |
|---|---|
| Property missing | not_reported |
| Explicit null | not_paused |
| Recognized typed future date | paused |
| Recognized typed elapsed date | expired |
| Unknown value or timezone-unspecified DateTime | unknown |

Raw pause dates are not logged. UTC/Local typed DateTime is normalized through
DateTimeOffset; arbitrary strings are not parsed. Full native comparisons still
retain pause changes in memory and ignore only LastCheckedForUpdates. Existing
mutation guards are not loosened; the PS fixture guard still rejects non-null
pause values. No product write gate is enabled.

## Validation and evidence limits

- Live metadata-only run PASS without GUI, Hello, package query, UAC or trust.
- test_release_scripts.ps1: PASS 199, including missing/null/future/elapsed/UTC/
  ambiguous/private pause values, output-shape availability, live mode and
  conflicting package/window options. Prior 188 checks retain their scope.
- Ignored receipts: `.artifacts/g05-api-audit/update-api.json`,
  g05-api-audit.log and g05-api-release.log. Original installed receipts unchanged.
  Live update-api.json SHA-256: a1b59c482e307afb30a4eccf066b102c64a0105418be01353187e72bc539906c.
- No new installed/native write test, source-type comparison or GUI repetition.
  The old safe summaries omit pause values; empty native delta arrays alone do
  not establish what the original pause value was. Do not fill historical receipts
  using the new classifier or infer a measured effective Disable value.

The audit closes metadata questions, not the failure. Next work must isolate the
save failure or source equivalence using a new, explicitly scoped experiment;
there is no reason to repeat the same toggle sequence without new measurements.
G05, effective update suppression and N-to-N+1 preference retention remain open.
