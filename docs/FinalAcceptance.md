# WSA-UX-029 final Windows acceptance procedure

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



The software delivery covers closing slices 12-15. Full Settings parity and
public beta acceptance remain open until the independent native/backend/human
gates below have actual evidence. Never change `sprint_complete` or
`release_qualified` merely because a software runner exits zero.

## Reproducible software gates

From the WordSuggestorWindows repository root, with the documented .NET/Swift/
Windows SDK/Python prerequisites and pinned Core source available beside it:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_clean_checkout.ps1 -FullRegression
```

This exports committed Windows HEAD and dependencies.lock.json's Core revision,
then invokes the existing build/Core/app/Settings/GUI/media/control-map/restart/
release scripts sequentially. No copied bin/obj/Core build cache is used. Installed
toolchains remain prerequisites. An unlocked ordinary-user desktop and installed
Danish speech/OCR capabilities are needed for native adapters. This is not a
zero-prerequisite offline build. For pre-commit review, add `-IncludeWorkingTree`;
the report explicitly labels that snapshot and does not modify the Git index.

Ignored output `.artifacts/clean-<nonce>` contains per-gate logs, `regression.json`
and `software-source.sha256` (non-Markdown source bytes, canonical LF for text).
A failed/blocked child stops the run; exit 3 never counts as PASS. The report
separates full software qualification from release qualification (always false).
Default three-gate mode remains available and cannot qualify the complete suite.

`test_settings_app.ps1 -Mode Layout` (also in All) checks both account states using
synthetic local transport, both interface languages, eight categories and 760x480 /
1000x700 layouts, help/editor peers, native UIA names and enabled-control focus /
scroll. Four offscreen raster scales test WPF rendering only. They do not change
OS DPI or attest physical mixed-DPI/multi-monitor or human Narrator behavior.
All still excludes HelloNative and installed update-policy mutation. No positive
Hello prompt/enrollment is triggered. Account transport/storage remain synthetic.

## Final hardware / human / external matrix

| Flow | Required evidence before acceptance | Current boundary |
|---|---|---|
| Native editor, toolbar focus and Notepad | Existing GUI + AutomaticWords physical input checks | Automated on actual desktop; see slice15 results |
| Settings labels/navigation | Layout/UI native automation plus human screen reader review | UIA/keyboard automation available; Narrator human review NOT RUN |
| Monitor scaling | 100/125/150/200% OS settings, at least two monitors, move editor/overlay across them | Offscreen raster scales only; physical mixed DPI NOT RUN |
| Audio, microphone, snipping | Listen to real speech, dictate via mic, actual screen selection/permissions | Native adapter checks do not attest subjective audio/microphone/snipping |
| Hello PIN/biometry | Real supported device/account, cancellation and protected recovery | USER-DEFERRED, do not run without later user resumption |
| Real unknown-word sharing | macOS-owned receiver deployed, test-account consent/revoke/retry roundtrip | Windows source gate false, A07 unavailable |
| Signed clean install/update/rollback | Approved test environment/trust, exact matching package evidence | Combined LocalTest install/manual update/rollback, integrity, retention and installed GUI 56 verified; unattended automatic update/G05 remain false |
| G05 update preference | Packaged native policy write/read/restore, then reviewed source enablement | Native blocked 0x800706F7, no policy change; remains partial/read-only |
| Official public beta | Approved stable signer, native gates, reviewed content, pinned SSH and O-052 flow | No production certificate or public upload here |
| macOS runtime/download/Sparkle | Native macOS and actual public-flow tests by owning agent | Source/library/pack contracts unchanged here; runtime NOT RUN |
| Office/browser/Google Docs | Explicit per-adapter native qualification | Not supported by current Notepad-only acceptance |

## Remaining steps in product terms

Give the [backend prompt](handoffs/WSA-UX-029_slice13_backend_agent_prompt.md) to the
macOS agent. After receiving an endpoint/commit/deployment record, qualify a real
test-account transfer before enabling sharing in a reviewed Windows release.
Users cannot currently upload learned words by toggling a hidden setting.

Use [Release.md](Release.md) for signed local fixture preparation and explicit
leaf-trust consent on a disposable test account/VM. Check the actual native
qualification record and cleanup, not just the command exit. Rebuild a fresh series
from the final reviewed source before acceptance. Production release still needs
an approved recipient-trusted stable signer and canonical HTTPS feed. The Windows
uploader remains unchanged, private staging only, pending before reviewed active.

Human acceptance then verifies visible focus/contrast/Narrator, real screen/DPI
transitions and audio/dictation. Record exact OS/build/displays and PASS/FAIL/NOT RUN
per row without sensitive document or account content. Hello remains parked until
the user explicitly resumes it. macOS/backend/shared documentation ownership and
source contracts remain unchanged.
