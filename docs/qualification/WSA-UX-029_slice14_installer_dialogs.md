# WSA-UX-029: isolate installer dialogs before GUI qualification

## Later measured outcome: fixture USED, GUI PASS 56

User run cfe870ce945c442eb1eec39d93a50a31 completed 22/24 native checks and all 56
installed GUI checks; dialog observation and subsequent idle are verified. G05
and unattended automatic update remain false; cleanup verified. See
[measured acceptance and next work](WSA-UX-029_slice14_installer_dialog_acceptance.md).
The preparation/software/standalone-blocked results below retain their historical
scope. The prepared bb5e06fb7203 nonce is USED; do not rerun it.

Date: 2026-11-13. Base Windows commit 1083002. Windows test orchestration and
local docs only; application payload, Core, packs, backend, macOS and shared Docs
unchanged. This follow-up prepares a rerun; it does not claim native acceptance.

## Observed failure and scope

User attempt d532d9ba6ad44e0daa9d0b56859881b0 used LocalTest.a0ea1a8cfbf2.
Automatic update was false; manual update, retained data, higher-version .3
rollback and dirty drafts passed. G05 remained blocked. GUI stopped at 'Notepad
native adapter available'. The screenshot showed an App Installer update dialog
for .2 still open after rollback had reached .3. The previous harness continued
from its 60-second automatic observation into manual/rollback/GUI without ensuring
that installer UI was gone. The original run has no foreground telemetry proving
the dialog caused its failure; preserve it as incomplete, not retrospectively PASS.
Its receipt reports temporary trust cleaned.

The installed App Installer observed here is 1.29.380.0. Its official process
package family is Microsoft.DesktopAppInstaller_8wekyb3d8bbwe. This is process
identity, not a match on a localized window caption or a document title.

## Implemented orchestration

- DesktopEnvironmentProbe uses native visible top-level windows, runtime process
  package identities, and hosted children for legacy ApplicationFrameWindow.
  Hidden/cloaked windows are excluded; multiple installer children count once.
  Probe output is only schema, visible installer count, installer-foreground flag
  and numeric foreground PID. It never reads window titles/document text or
  invokes buttons, closes windows, kills processes or changes OS settings.
- Read-MsixDesktopResult validates the bounded closed JSON schema. Probe process
  execution uses the existing bounded direct-process transport and inherited
  repository .NET environment. A probe failure stops qualification.
- Wait-MsixInstallerIdle runs before installation, automatic launch, manual update,
  rollback and installed GUI, and after manual update. It requires at least 0.5s
  observed idle, with at most 120s per boundary for operator handling. Any visible
  Microsoft App Installer window blocks, including a background window; unrelated
  windows are never manipulated. ACTION MSIX asks the operator to cancel the
  WordSuggestor fixture dialog. The harness then observes closure automatically.
- Timeout records the exact desktop_blocked_phase and fails before the next phase.
  Qualification JSON retains per-boundary dialog_observed/idle/foreground PID.
- Automatic update remains false if an installer dialog is observed, even if the
  package version has already advanced. Initial and late boundary observations
  also count. Operator closure cannot convert a check into automatic-update PASS.
  The approved manual Add-AppxPackage lane continues only after idle. No release
  feed policy, ShowPrompt setting, timeout requirement or existing assertion is
  weakened; there is no blind confirmation of update consent.
- GuiSmoke checks again at startup and during initial Notepad readiness. It waits
  up to three seconds for native foreground + supported focused text target bound
  to the expected HWND/PID. Existing nullable UIA focus assistance is preserved;
  actual GUI-thread focus in WindowsNativeTextAdapter is authoritative. Failure
  distinguishes AppInstaller blocking, foreground unavailable and text target
  unavailable, without typing first. Diagnostics contain flags/PID, not captions
  or document text. Cleanup Ctrl+S additionally requires verified native target;
  exact-window close rechecks owner PID and never types into a save prompt.

Microsoft specifies that [GetPackageFamilyName](https://learn.microsoft.com/en-us/windows/win32/api/appmodel/nf-appmodel-getpackagefamilyname)
returns a process's package family. Windows can refuse a foreground request even
when its documented prerequisites appear met; therefore the test rechecks
[actual foreground](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow)
rather than trusting [SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow).

## Validation results and limits

| Check | Result |
|---|---|
| build_app.ps1 | PASS, 0 warnings/errors |
| test_app.ps1 -SkipBuild | PASS 105, including seven new classifier/native-probe checks |
| test_release_scripts.ps1 | PASS 112, including 18 new closed-schema, automatic classification, dialog-close and timeout checks |
| Real native probe through msix_desktop and local .NET environment | PASS fixed JSON; zero visible installer windows at observation |
| Standalone GUI attempt with current diagnostics | NOT PASS: NotepadForegroundUnavailable, focus request denied, Explorer PID 7096 foreground, no installer windows; stopped before test input |
| New four-package preparation | PASS; key removed, no installation and no temporary trust provisioned |
| Full native dialog/automatic-update/installed-GUI rerun | NOT RUN here; operator procedure below |

During development an immediate UIA-field requirement was found premature and
removed: the top-level Notepad window can precede its provider, and UIA was
previously nullable. A later trial unnecessarily restricted existing UIA focus
assistance; that restriction was removed too. Final behavior preserves that route
and waits for independently verified native focus. The final blocked GUI log
still shows Explorer holding foreground; no GUI PASS is inferred. There were no
changes to production text adapters or a forced-foreground workaround.

Logs (ignored): .artifacts/dialog-{build,app,release,gui,gui-final,preparation,
read-state}.log. Unit dialog-close/timeout cases use synthetic snapshots, not a
real installer dialog. Real low-level probe tests prove API execution and format,
not positive popup recognition. The user's rerun must establish that behavior.
Earlier native integrity/version/uninstall results remain historical evidence in
[slice14_package_integrity](WSA-UX-029_slice14_package_integrity.md); no new full
source regression or combined native PASS is claimed.

## Prepared local fixture (now USED)

Session: .artifacts/msix-tests/prepared-bb5e06fb7203/prepared-session.json.
Identity WordSuggestor.Windows.LocalTest.bb5e06fb7203, publisher
CN=WordSuggestor Local Test bb5e06fb7203, public certificate thumbprint
C29C35EDA595D2D872FA27D0A65561625F9EEB54. Seven-day non-exportable test key was
removed. Independently checked: no matching CurrentUser signing key, no matching
LocalMachine TrustedPeople entry and no registered package. Preparation did not
install or change trust. Retention user data will be isolated to this nonce.
The unchanged pinned Core was reused from an earlier verified signed payload.

| Package / purpose | MSIX SHA-256 |
|---|---|
| 0.2.0.1 / install | dc8aae1f79a8203b5571f70fa9a00ca1e07004e89bcbdf67b8bca3c76aff8423 |
| 0.2.0.2 / update | 69e4a9948da5d7c5e4ca1d6aba7bb0651c7778fefa3351efabf86c31472bd780 |
| 0.2.0.3 / higher-version rollback | 7690250fc975478d401160be9476795b4d70d4fb1c1f907aa0967dd0812ac589 |
| 0.2.0.4 / negative tests | eed1d70287ecd5f1bf937393016642ddb4790814c000fdbf6e43a784407e63ab |

## Step-by-step operator rerun

1. Save work, close WordSuggestor and Notepad, and close old WordSuggestor App Installer dialogs
   with Cancel. Open ordinary PowerShell, not an administrator shell. Keep Windows
   logged in, unlocked and the remote session connected throughout the test.
2. Change to the WordSuggestorWindows repository root. A later rerun needs a NEW
   fixture nonce; preserve the used bb5e06fb7203 data/receipts.
3. Prepare a new series, copy its printed session path when prompted, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare_msix_test.ps1
$testSession = Read-Host "Paste the newly printed Prepared local test session path"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 -PreparedLocalTestSession "$testSession" -AllowTemporaryMachineTrust
Write-Host "Test exit code: $LASTEXITCODE"
```

4. Approve the one UAC request for temporary LocalMachine TrustedPeople leaf trust.
   The rest runs as an ordinary user. No root certificate or positive Hello test.
5. Monitor the console during installation/update. If ACTION MSIX appears, close
   the WordSuggestor test update dialog using Cancel/Annuller within 120 seconds.
   Do not choose Finish Updating/Open Now. The script observes closure; no Enter
   confirmation is required. It then applies the verified manual update itself.
6. When GUI MSIX starting appears, leave mouse/keyboard idle until the test exits.
   The test opens Notepad and WordSuggestor using synthetic documents.
7. Send terminal output, the MSIX qualification record path and printed exit code.
   G05 BLOCKED and any interactive/timeout automatic observation remain false;
   completed partial qualification exits 3. A thrown failure is not a partial PASS.
   Check test_trust_cleaned and remaining false gates even after a nonzero exit.

After a failed/interrupted run, preserve its profile/receipts. Do not rerun the
same now-nonempty fixture or delete synthetic data. Instead prepare a NEW nonce:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepare_msix_test.ps1
$testSession = Read-Host "Paste the newly printed Prepared local test session path"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 -PreparedLocalTestSession "$testSession" -AllowTemporaryMachineTrust
Write-Host "Test exit code: $LASTEXITCODE"
```

No Skip flags in the complete run. Both Skip flags remain LocalTest-only partial
lanes. If the seven-day certificate has expired, prepare a new series as above.
No generated packages/certificates/keys belong in Git.

## Remaining product work

The later user run now proves popup detection, subsequent idle and installed
editor/Notepad GUI 56. See the measured acceptance linked above. Investigate the
two remaining native checks before another run. A dialog-dependent update remains
interactive, not automatic acceptance.
If foreground is denied even in the user's active session, use the new fixed
metadata to distinguish target readiness from desktop focus restrictions.
G05 0x800706F7 is a separate unresolved native-policy failure and stays read-only.
Hello remains user-deferred; human Narrator/mixed DPI, production signer, canonical
HTTPS/public beta and macOS-owned sharing receiver remain open. Full sprint and
release flags stay false. No macOS/Core/backend/shared Docs edits or public upload.
