# WSA-UX-029: approved closing slices 12-15

## G05 Settings page verification (2026-11-14)

The operator reached Installed apps; the old nonce-anywhere check was a false
positive for app details. List pages are now rejected; details need header/spec/
version proof. A guarded in-package launcher replaces the unreliable external URI.
Installed own-package launch now verifies app_details and its update control.
Build/app105/UpdateState38/release161 retain their recorded scope.
G05 remains read-only. See [measured read-only acceptance](../qualification/WSA-UX-029_g05_packaged_settings_page.md).


## G05 Settings window correction (2026-11-14)

The probe admitted IntPtr.Zero and assumed Settings owned the top-level window.
Both are corrected; a real hosted Settings window and 769 UIA nodes read without
error on this VM. Release scripts: 151 PASS. The specific app page still needs its
fresh UI-only check; G05's separate 0x800706F7 remains unqualified/read-only.
See [reproduction, live proof and fresh test](../qualification/WSA-UX-029_g05_hosted_window_fix.md).


## G05 measured input/schema result (2026-11-14)

The operator completed the isolated run: all 21 input attempts still reject with
0x800706F7, across baseline/capabilities and schemas 2018/2021. Original policy
and cleanup verified. Settings UI remains unqualified; diagnostic labels/error
metadata and a read-only UI-only follow-up are added. G05 stays read-only.
See [measured matrix and next test](../qualification/WSA-UX-029_g05_input_schema_acceptance.md).


## G05 input/schema investigation (2026-11-13)

Seven native input controls, same-identity schema comparison and a read-only
Settings probe are implemented. Build/app105/UpdateState35/release135 pass.
The native attempt stopped at UAC BEFORE installation; the new comparisons are
NOT RUN and G05 remains unqualified/read-only. No VM cause is established.
See [exact evidence, research and continuation](../qualification/WSA-UX-029_g05_input_schema_diagnosis.md).


## G05 isolated native diagnosis (2026-11-13)

Both installed baseline and capability variant fail at write_policy/0x800706F7;
Windows cmdlet, PowerShell WinRT and native C++ controls fail identically,
including factory/manual no-op and 0/6-hour comparisons. All
settings remain unchanged/restored and temporary trust/install cleaned. Broad
capabilities alone do not solve it, so production rights/template stay unchanged.
New diagnostic-only guards and actual off/on proof requirements are implemented;
build, app105, UpdateState31 and release131 pass. G05 stays read-only. The host is current GA Windows 11 26H2, not an
established Insider/VM fault; native/data diagnosis continues on this same VM.
See [measured diagnosis and exact isolated procedure](../qualification/WSA-UX-029_g05_native_diagnosis.md).
Earlier acceptance records retain their scope; no G05/public release PASS implied.


## Measured native installer and GUI acceptance (2026-11-13)

The default combined user run completed: 22/24 native checks and all 56 installed
GUI checks pass. Installer dialog detection/clearance, real editor/Notepad,
manual update, dirty rollback, integrity negatives and uninstall data retention
are verified together. Cleanup independently confirmed. Exit 3 remains correct:
automatic update required dialog handling; G05 still returns 0x800706F7/read-only.
No unchanged rerun is needed now; investigate those two native checks next.
Production signing/HTTPS/public release and remaining human gates stay open;
Hello user-deferred. See [exact evidence and next work](../qualification/WSA-UX-029_slice14_installer_dialog_acceptance.md).
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
See [exact evidence and next steps](../qualification/WSA-UX-029_slice14_package_integrity.md).
Earlier dated sections below preserve historical, narrower results.



Date: 2026-11-13. User authorizes implementation of all four slices, each documented
and committed in every changed repo before the next slice. No push/publication.

| Slice | Fixed scope | Acceptance boundary |
|---|---|---|
| 12 | Verified typing -> Core boundary -> consent-controlled local import | Native editor and Notepad, cancellation, throttle, old consent never enables capture |
| 13 | Windows unknown-word sharing client and existing Core handoff format | Backend implementation belongs to macOS agent by explicit user decision; no real uploads until deployed contract is approved |
| 14 | Installed updater / signed installation and N-to-N+1 qualification | No unsafe trust or production-policy bypass; production signing and any required external trust action stay explicit |
| 15 | Reproducible final regression, accessibility/DPI and acceptance record | Report actual hardware/manual/native results; never convert blocked or untested items into PASS |

Windows Hello remains user-deferred throughout. macOS app/shared Docs/backend are
read-only here. Shared Core changes must remain additive with old-client tests;
macOS-native execution is not attested by Windows tests. Existing MSIX/App Installer
and O-052 upload contracts remain authoritative. A code-delivered slice can retain
external acceptance blockers; sprint_complete/release_qualified stay false until
all actual acceptance requirements are met.

Next for users: automatic lookup during writing, then separately consented sharing
when the macOS-owned backend is ready, then verified update/install behavior and
final human acceptance. No automatic ranking changes, public upload or official
beta publication follow merely from implementation or software tests.

## Delivery checkpoints

- Slice 12: committed Windows 9966143 / Core ebd2424; physical editor/Notepad PASS.
- Slice 13: committed Windows 888058b / Core 0278cbc; Windows client ready,
  real receiver/activation pending macOS backend owner. No backend changes here.
- Slice 14: Windows b0d0eec committed; signed but untrusted series, no
  installed/native acceptance without explicit trust approval. See slice14 evidence.
- Slice 15: independent final source/desktop regression and UIA name correction;
  see [final evidence](../qualification/WSA-UX-029_slice15.md) and
  [remaining operator/human acceptance](../FinalAcceptance.md).

These checkpoints do not close sprint_complete or release_qualified. Missing
hardware/receiver/signing/installed evidence remains an acceptance dependency.

Final slice 15 software result: PASS all 14 clean-source sequential gates,
including 16 Settings suites and native editor/Notepad GUI. Native installed /
human / real-backend acceptance remains explicitly open. The initial transient
GUI timeout and unchanged retry are retained in the qualification record.
