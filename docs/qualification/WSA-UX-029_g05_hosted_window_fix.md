# WSA-UX-029: Settings window binding correction

Date: 2026-11-14 (Europe/Copenhagen). Base 5ee92be; initial working tree clean.
Windows-only qualification scripts. No product updater, Core, pack or macOS change.

## Actual installed UI-only result

The operator's 6c87aa66a76a4b829e8b9d8eee064ab4 run completed 2026-11-14 00:37:21
to 00:38:45 Copenhagen (2026-11-13T23:37:21Z to 23:38:45Z). Clean install and
ordinary-user checks pass. Tested .1 under LocalTest nonce bc06ec5df05e. The actual
UI receipt says ui_probe_unavailable / window / ArgumentException / 0x80070057,
node_read_errors=0, page_target_verified=false, update_toggle_found=false. The
failure precedes node/control reading. It is distinct from the native setter's
0x800706F7 and does NOT establish that Windows lacks an update control.

completed=true, test_trust_cleaned=true, release_qualified=false. Independent
package/key/TrustedPeople checks are all absent. The used profile is preserved.
The raw local receipt SHA-256 is c5d6e743cc4afb8766c40b5d0daa2a95171d9cb5e120cdd972f5b3705ca34f3f; raw artifacts remain ignored.
Session bc06ec5df05e is USED and its former command is retired.

## Reproduced test-code defect

Windows PowerShell reports all of the following on this machine:

| Expression / operation | Observed result |
|---|---|
| `[bool]([IntPtr]::Zero)` | true |
| `-not [IntPtr]::Zero` | false |
| `[IntPtr]::Zero -eq [IntPtr]::Zero` | true |
| `AutomationElement.FromHandle(IntPtr.Zero)` | ArgumentException / 0x80070057 |

The old '-not $process.MainWindowHandle' guard therefore admitted a zero handle.
This is a proven bug in our probe and reproduces exactly the reported phase/error.
The old receipt did not record the actual handle, so it is not retroactively
upgraded into a full call trace. It provides no explanation of 0x800706F7.

Read-only native inspection also confirms the actual window arrangement on this
VM: SystemSettings has MainWindowHandle=0, while a nonzero ApplicationFrameWindow
belongs to ApplicationFrameHost and contains a UIA child owned by SystemSettings.
[Microsoft documents that MainWindowHandle may be zero](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.mainwindowhandle?view=net-10.0),
while [FromHandle takes a window handle](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.fromhandle?view=windowsdesktop-10.0).
No VM repair, framework migration or administrative workaround is justified.

## Corrected binding

- Explicitly compare handles with IntPtr.Zero; never cast them to a boolean guard.
- Find desktop UIA top-level windows owned by processes whose executable paths
  match Windows' SystemSettings or ApplicationFrameHost binaries. No caption search.
- Accept a direct Settings owner, or a verified frame-host owner ONLY if its
  descendants include a verified Settings process. Reject zero/unrelated owners.
- Use that existing AutomationElement, avoiding Process.MainWindowHandle and the
  FromHandle call entirely. Re-read fresh window candidates; tolerate stale nodes.
- Keep nonce verification before reporting page controls. The recorded window
  route is direct/hosted; window discovery alone never qualifies an app page/G05.

The existing test_settings_platform script adds SettingsWindowProbe: read only,
no package, install, UAC, Hello or toggle. It refuses package-qualification flags
and emits fixed counts/routes plus page_target_verified=false/g05_qualified=false.
The application-specific UI-only path keeps existing installation/cleanup gates.

## Actual validation after correction

| Check | Actual result |
|---|---|
| test_settings_platform -SettingsWindowProbe on this VM | exit 0; 1 window; hosted=1; all_handles_nonzero=true |
| Read the live hosted window's descendants | 769 nodes; 0 node errors; no HRESULT |
| Absent test nonce in that live read | page_not_verified; no app-page or G05 qualification |
| test_release_scripts.ps1 | PASS 151 |
| New installed package's page/control | NOT RUN yet |

The read-only live checks ran in the ordinary user desktop context outside the
restricted tool desktop. No UAC, installation or setting mutation. A general Apps
Settings page was opened for window discovery; only route/count/error metadata
was retained, never captions or document text. This is real window-binding proof,
not a claimed successful G05 setter or a full UI-only installation acceptance.

Regression checks cover zero handles despite truth conversion, direct ownership,
frame-host child requirements, unrelated owner rejection and no G05 qualification
from generic window inspection. Production updater write gate stays false.

## Prepared-session freshness

UI-only tests may leave an empty app profile, so profile emptiness alone cannot
prove an identity is fresh. Prepared test sessions now use an atomic CreateNew
used-session.json marker before the installation phase, AFTER trust verification.
Existing markers reject the attempt before bundle processing/trust. Cancellation
before trust leaves the prepared identity available if never installed. Verified
previous installed nonces b14042ba01ae and bc06ec5df05e were explicitly retired
with markers; their profiles/receipts were preserved. A regression covers rejection.

## Fresh app-page follow-up

Prepared ea09b46c0e99 is fresh and uninstalled. Four signed fixtures were prepared
through the unchanged packaging flow, with existing Core/pack bytes and unique
nonce labels. MSIX manifest readback verifies identity/publisher and labels.
The ephemeral private key is deleted; no machine trust or installation was added.
The new installed application-page check is still NOT RUN.

From an ordinary PowerShell in the Windows checkout:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 `
  -PreparedLocalTestSession .\.artifacts\msix-tests\prepared-ea09b46c0e99 `
  -SkipBuild -UpdatePolicyDiagnostic -UpdatePolicySettingsUiProbe `
  -UpdatePolicySettingsUiOnly -AllowTemporaryMachineTrust
```

Approve the local UAC; do not click any system controls. This runs only the
application-page inspection and existing install/cleanup, not the 21 setter
attempts. Return the G05 Settings UI line and final receipt path. Exit 3 is a
partial diagnostic by design. Never rerun the used bc06/b140 identities.

## Product status and next decision

The diagnostic window-binding defect is corrected and tested live. The actual
app page/Windows-owned update control still needs the fresh installed read-only
check. Then decide how to test that control itself. No automatic clicking exists.
The native setter's 0x800706F7 remains unexplained; its prior 21 negative results,
HTTPS-source comparison and off/on preference retention keep their original scope.
G05, official release, Hello and other human/backend gates remain unqualified.
macOS, Core, backend and shared Docs source are unchanged.

Local ignored evidence: g05-window-binding/settings-window.json, g05-window-nodes.log,
g05-window-release.log, g05-hosted-ui-prepare.log and the two corresponding MSIX
attempt/preparation roots. The fresh test packages/key/certificate are never Git
content. Windows-local docs own this evidence; shared Docs may link it later.
