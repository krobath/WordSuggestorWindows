# WSA-UX-029 final Windows acceptance procedure

## Authorized native follow-up (2026-11-13)

Approved native runs passed trusted signing, ordinary-user clean installation
and the corrected installed Core/user-word receipt gate. Fixture seeding then hit
WindowsApps execution protection for a loose test process. The seeder now uses
the verified MSIX extraction and checks Core/pack hashes against the installation;
local extraction/readback passes. The next UAC was canceled. Installed N+1,
rollback and policy remain unqualified; package/trust cleanup is verified.
See [exact results and next action](qualification/WSA-UX-029_slice14_native_followup.md).
Earlier pending-approval/no-install statements are historical preparation results.


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
| Signed clean install/update/rollback | Approved test environment/trust, exact matching package evidence | Slice14 prepared fixtures; native installed acceptance pending |
| G05 update preference | Packaged native policy write/read/restore, then reviewed source enablement | Remains partial/read-only |
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
