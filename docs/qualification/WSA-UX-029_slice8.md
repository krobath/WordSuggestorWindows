# WSA-UX-029 slice 8 — update Settings qualification

Date: 2026-11-12. Windows base `580268b3b8d154f8ae78327ac31fdbe7d8f0c5e6`;
Core `defa19bdc6a3fcb990948caa1982631ced420dbf` unchanged. Backend contract source
`2f67ddc6`, canonical macOS Settings reference `ba18b45`. Scope: General G04–G06.
See [update contract/native fixture procedure](../WindowsUpdates.md).

## Audit and decision

The previous adapter checked HTTPS XML but opened the feed directly without an
OS-state/close guard. G05 was a read-only explanatory TextBlock with no actual
policy display/setter. Retain working WPF/Core/pack architecture and existing
MSIX/App Installer flow; add typed Windows adapter and app coordinator, preserving
unrelated tabs/restart/release work outside the staged candidate. No backend,
macOS, shared Docs, language logic or data migration is needed.

Older slice-0 PowerShell/cmdlet writes failed with `0x800706F7`; do not infer that
typed API projection alone solves OS permissions. It compiles/reads API presence,
but positive installed ordinary-user authority is still not established.

## Software and native evidence boundaries

Earlier targeted working-tree candidate build passed with zero warnings/errors;
37 update checks passed with an active desktop (26 state/contract plus native WPF).
An earlier broader working-tree Settings All run also passed before the final two
negative XML cases were added. These runs include existing unrelated working-tree
code and do not replace the exact-source results below. Fresh desktop acceptance
is BLOCKED because this Windows session became disconnected. No native assertion
was weakened, and the final All run is not reported as PASS.

| Flow | Result / boundary |
|---|---|
| Bool/mixed/unknown OS state and disabled unqualified editing | Simulated adapter + real loose WPF PASS |
| Successful change/readback, no-effect and unrelated-policy change | Contract tests PASS; simulated native adapter, not actual Windows mutation |
| No persisted automatic-update app bool | Real settings bytes unchanged PASS |
| Mouse, Space and accessibility Toggle share verified path | Earlier native WPF candidate PASS; fresh isolated desktop BLOCKED |
| Feed + OS agreement, offline/unknown/changed/invalid feed | Real HTTP serializer + synthetic responses/OS PASS |
| Canonical version, downgrade, duplicate settings, activation-blocking rejection | Contract PASS; not package signature qualification |
| Single-flight, cancellation, dispose and stale result | Contract PASS |
| Ticket revalidation, expiry and one-use | State/UI PASS; fake launcher |
| Native confirmation No/Yes and canceled close | Earlier candidate PASS; fresh isolated desktop BLOCKED |
| Actual dirty-document Cancel; normal main close before launch | Earlier native WPF candidate PASS, fake launcher; fresh isolated desktop BLOCKED |
| Typed AppInstallerManager presence | Read-only probe available on Windows build 26300; not mutation proof |
| Development-registered isolated native probe | BLOCKED: registration refused by OS developer/sideload policy; policy not changed |
| Separately signed isolated peer certificate probe | BLOCKED: SignTool `/pa /all /v` did not trust root; no package installed |
| Trusted installed LocalTest policy change/read/restore | BLOCKED: no trusted installed candidate fixture |
| Real beta manual/automatic feed; clean install; N→N+1; signature refusal | Not run, still release gates |
| Final Media smoke | Exit 1: WPF OCR fixture rendered black; previous slice-7 baseline fails identically in the same disconnected session; rerun required |
| Windows 10, extra DPI/monitors, human Narrator/contrast | Not run |
| Native Hello PIN/biometric | DEFERRED BY USER 2026-11-12; not rerun |

The short-lived experimental signing probe used a unique non-exportable non-CA
leaf only in CurrentUser My/TrustedPeople. Its own peer certificate/private key
was removed in finally. No root/machine trust, developer policy, production
certificate/feed/package or production automatic scheduling was changed. Ignored
API-probe artifacts are evidence only and are not a product release workflow.
No raw key or document text is logged.

## Final isolated-source result — 2026-11-12

Final exported Git tree: `dfca67907bd904cfe5103668757f4a2d1fc716d9`.
Core: `defa19bdc6a3fcb990948caa1982631ced420dbf`.
SHA-256 of sorted complete non-document index object entries:
`b5488e19b3f54cc0b44bfa81b1f581ef95907ba285ff711367f4fc73f9d69749`.
Only documentation changed after this export. This fingerprint covers all source,
scripts, assets and dependency pins, excluding root README and docs; it must match
the final staged source. Windows/Core were initially archived into fresh sibling folders and built
without working-tree source or binary/cache copies. The final localization-only
Windows re-export reused that unchanged committed Core source/build in its sibling
folder; all listed checks ran again. Windows binaries were rebuilt inside the new
Windows export, and Core CLI/fixtures ran from that isolated committed Core build. Logs/results remain ignored under `.artifacts`.

| Exact-source command/gate | Concrete result |
|---|---|
| `scripts/build_app.ps1` | PASS; 0 C# warnings/errors; normal output paths preserved |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS; 6 real Danish fixtures and 335 Core checks; existing Swift/pkg-config warnings retained |
| `scripts/test_app.ps1 -SkipBuild` | PASS; 67 checks |
| `scripts/test_settings_app.ps1 -Mode UpdateState -SkipBuild` | PASS; 26 |
| `-Mode AccountState` | PASS; 44, synthetic backend/native Credential Manager; no production account mutation |
| `-Mode AnalysisState` | PASS; 30 |
| `-Mode SuggestionState` | PASS; 27 |
| `-Mode Speech` | PASS; 102; no human audible acceptance |
| `-Mode Core` | PASS; 58 |
| `-Mode State` | PASS; 63 |
| `scripts/test_settings_qualification.ps1` | PASS; 35 contract/safety checks |
| `-Mode All` / `UpdateUi` | Not run in final export: disconnected session. Earlier isolated attempt exited 1 waiting for foreground; rerun required |
| `scripts/test_gui.ps1` | Not run in final export because active desktop was unavailable |
| `scripts/test_media.ps1 -SkipBuild` | NOT PASS, exit 1; all-black OCR fixture, bridge exit 0/empty result; later media components not reached |
| `-Mode UpdateNative` | BLOCKED, exit 3; prerequisite reminder, not actual fixture mutation |
| `HelloNative` | DEFERRED BY USER; not executed |

An initial isolated attempt failed at the mouse-state assertion. A stricter
completion wait and foreground guard were added without relaxing the expected
state/write count. In the subsequent exact export
`381c54bd022d837a8dab07d1ebc2e38920d523dd`, the guard timed out; session 2 was
`Disc` (`query user`) and the target foreground was unavailable.
That run was retained as a failed/blocked desktop
attempt, not silently retried into a PASS. For OCR, both candidate and previously
built slice-7 baseline produced visually inspected all-black 500×140 PNGs and the
same line-32 assertion failure in this disconnected session. This comparison is
evidence against attributing the failure to the new SDK target; it does not
qualify OCR. Leave the fixture/test unchanged and rerun on an active desktop.
The final archive above changes only the two localization JSONs outside docs
compared with the earlier exact source,
with plain user help and an accurate OS/feed-disagreement message. All listed
state/build/Core/app/safety commands were rerun in that fresh final archive;
Media still failed on the black fixture. Physical desktop tests were not repeated
while disconnected; their earlier failure and intermediate success remain distinct.

Final review: 34 documented text/source files; 219 non-slice tracked baseline files
byte-identical and all 18 original untracked file digests preserved. Mixed-file
projections preserve original uncommitted hunks; no generated binaries, production
keys, tokens or machine-specific paths are staged. Backend/Core/macOS/shared Docs
remain unchanged. The whole suite and release are **not fully qualified**.

## Reproduction from Windows repository root

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode UpdateState -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode All -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_qualification.ps1
# Prerequisite reminder only: exits 3 until packaged fixture is supplied.
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode UpdateNative -SkipBuild
```

The actual packaged roundtrip uses `test_settings_platform.ps1 -PackagedUpdateProbe`
with a preinstalled isolated family; follow the contract and do not target beta.
`All` excludes that mutation gate and HelloNative; it may read Hello availability
and exercise fake cryptography but never enrolls/prompts PIN/biometrics.

## Remaining qualification and next work

Supply a trusted isolated candidate fixture through the existing release process,
then run the ordinary-user policy roundtrip and pause/system-policy failure tests.
Keep production editing read-only until review. The current app can show Windows'
actual schedule and safely prepare a manual update; that does not attest a public
feed or successful installation. Real trusted beta install/update/data retention
and human accessibility still need their own evidence. Hello remains parked.
Next engineering slice implements Advanced diagnostics controls so users can
investigate failures without exposing document text; it does not publish a beta
or silently close these native gates.
