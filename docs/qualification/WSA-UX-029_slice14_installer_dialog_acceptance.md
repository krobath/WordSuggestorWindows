# WSA-UX-029: measured installed GUI and dialog-boundary acceptance

Date: 2026-11-13. Windows source under test: 61bba3a. Documentation-only
follow-up; no app, test, Core/pack, backend, macOS or shared Docs source changes.

## Result

The user completed the default combined LocalTest run, without either Skip flag.
Receipt: .artifacts/msix-tests/cfe870ce945c442eb1eec39d93a50a31/msix-qualification.json.
Started 2026-11-13T19:48:27Z, finished 19:51:16Z. completed=true, 22 of 24 checks
true. Exit 3 is the expected partial-qualification result: automatic_update and
update_policy_write_read_restore remain false. all_checks_passed=false and
release_qualified=false are correct and must not be changed to green.

The new dialog boundary is now measured: before_manual_update has
dialog_observed=true and idle=true; all other recorded boundaries are idle,
including before_gui. The subsequent actual packaged GUI completes 56 checks.
This verifies the new detection/clearance/GUI sequence. It does not establish
which button was clicked or retrospectively prove the exact foreground cause of
the earlier d532 failure, which had no such metadata.

## Actual integration matrix

| Check | Actual result |
|---|---|
| Trusted signed fixture, ordinary-user clean install .1 | PASS |
| Packaged real Core/DA and user-word suggestions | PASS |
| App Installer dialog detection and clearance before later phases | PASS observed dialog, then idle |
| Unattended automatic .1-to-.2 installation | NOT QUALIFIED; interactive_app_installer, false |
| Manual update path and .2 version observation | PASS |
| Settings, packs, user words, tabs, choices, learned words, pending handoff | PASS hashes and semantic readback; no upload |
| Known-good higher-version procedure .2-to-.3 and dirty draft retention | PASS |
| Packaged native update policy (G05) | BLOCKED 0x800706F7; false, no mutation |
| Installer idle before installed GUI | PASS |
| Actual installed toolbar/editor/Notepad GUI | PASS 56, DPI 96, one monitor |
| Installed activation/relaunch/restart | PASS |
| Newer wrong identity/publisher, offline and malformed feed | PASS rejected |
| Corrupt signature footprint .4 | PASS full SignTool failure and native rejection |
| Older package and same-version behavior | PASS older rejected, same unchanged |
| Payload-only corrupt .4 source | Native registration accepts; ALL 441 installed payload files match approved signed original |
| Uninstall and retained local data | PASS exact hashes plus Core/store semantic readback |
| Test cleanup | PASS receipt and independent registration/key/trust/process readback |

The actual GUI output covers Notepad native mouse and Ctrl+1 commits/caret,
clipboard preservation, stale-target rejection, toolbar focus, follow-caret opt-in
and static behavior, active internal editor over Notepad, accessibility names,
Unicode/multiline/selection replacement, native save/open, tab switching, dirty
draft/restart recovery and content-free bounded runtime diagnostics. It does not
qualify Office, browsers, Narrator human review, mixed-DPI or multiple monitors.
No separate real-account/backend sharing, mic/audio or positive Hello test claimed.

## Exact fixtures and verification

Used preparation: .artifacts/msix-tests/prepared-bb5e06fb7203/prepared-session.json.
Identity WordSuggestor.Windows.LocalTest.bb5e06fb7203, publisher
CN=WordSuggestor Local Test bb5e06fb7203. Public leaf thumbprint
C29C35EDA595D2D872FA27D0A65561625F9EEB54. This identity is local-test only and
cannot qualify the public uploader. Four signed MSIX SHA-256 values were rechecked
against their release manifests without restoring trust:

| Version | MSIX SHA-256 |
|---|---|
| 0.2.0.1 | dc8aae1f79a8203b5571f70fa9a00ca1e07004e89bcbdf67b8bca3c76aff8423 |
| 0.2.0.2 | 69e4a9948da5d7c5e4ca1d6aba7bb0651c7778fefa3351efabf86c31472bd780 |
| 0.2.0.3 | 7690250fc975478d401160be9476795b4d70d4fb1c1f907aa0967dd0812ac589 |
| 0.2.0.4 | eed1d70287ecd5f1bf937393016642ddb4790814c000fdbf6e43a784407e63ab |

The .2 App Installer file SHA-256 in the run receipt is
641bb18499de2a7c477a723f940902b7fd3e7801f864c88631d5794e7c738ac5.
The .4 corrupt-source outcome is accepted_verified_original_payload, not a valid
source archive or native rejection claim. Original source signing/hash release
gates remain strict; the uploader cannot publish that corrupt archive.

Update-policy receipt: matching attempt/update-policy/update-native.json. It
reports packaged_app_context=true, changed=false, restored=true,
source_and_other_settings_preserved=true, production_control_enabled=false and
0x800706F7 on Windows 10.0.26300.0. G05 stays read-only. Automatic update separately
reports interactive_app_installer; neither failure is hidden by the GUI result.

Cleanup receipt matching attempt/trust-cleaned.json reports removed=true and the
exact leaf thumbprint. Independent ordinary-user readback confirms no registered
bb5e06fb7203 package, no matching CurrentUser\My signing key, no matching
LocalMachine\TrustedPeople leaf, and zero WordSuggestor app processes. No root
trust or permanent signing identity was added. Synthetic retained profiles and
all prior receipts are preserved; the used nonce must not be run again.

## Reproduction and next work

Exact user command, from WordSuggestorWindows (now historical; nonce is USED):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 -PreparedLocalTestSession .\.artifacts\msix-tests\prepared-bb5e06fb7203 -AllowTemporaryMachineTrust
Write-Host "Test exit code: $LASTEXITCODE"
```

Do not repeat the full test unchanged just to remove exit 3. First investigate
the G05 native API failure and a genuinely unattended App Installer update path
under the approved feed policy; do not auto-click consent or reclassify dialog
handling as unattended success. A later installed rerun requires NEW prepared
fixtures. Follow [the updated operator procedure](WSA-UX-029_slice14_installer_dialogs.md#step-by-step-operator-rerun).
The operator has no additional immediate task for this completed measurement.

In product terms, installation and the actual editor/Notepad flow now work
together in one measured local run, including update/rollback/uninstall retention.
Remaining native work concerns the automatic-update setting and unattended
completion. Official beta still needs production signing, canonical HTTPS feed /
public publication and remaining human/platform acceptance. Hello stays
user-deferred; the sharing receiver belongs to the macOS backend owner. Full
sprint/release qualification remains false, with no public upload or production
signing claim. No new software suite or code change is attested by this doc commit;
prior build/app105/release112 results retain their original scope.
