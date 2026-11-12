# WSA-RT-017 Windows release cleanup qualification — 2026-11-12

## Scope and source provenance

Cleanup began at d7826c9 with 24 tracked modifications and 18 untracked source/doc
files retained from earlier Windows work. They were deliberately excluded from
Settings commits, not newly generated build junk. They are now organized as
developer restart (f9f62a4), editor tabs (3b84798) and this release-tooling delivery.
A byte/hash backup of all 42 inputs remains in ignored cleanup-baseline artifacts.
No destructive reset/clean, user-process termination or push is used.

Nine software gates ran on source-only Git archives, Windows staged tree
`b136aaeb11c6bf3fe6c0fe9fc83417d0f0e81f32` and pinned Core
`56aba63042796b2a6c398828c3737ab0cb02eae5`. No repository build outputs/caches were
copied. Fresh dependency restore and installed toolchains remain prerequisites.
Final non-document source in both the tested export and normal package-build
working tree was compared byte-for-byte (Git line endings) to the staged source.
Sorted path/NUL/content-SHA256 fingerprint: `95ae75b6555bafd387af69596aa47f61ffb783ae0734d837b28ff7b8fb4a4984`.
Only documentation was added after those tests. Logs: `.artifacts/cq-release-52ebe81a/`.

macOS ba18b451372dd2e3f125e557c5db45894f4e1ae3, Core, backend
2f67ddc6f5382b286e3039677a05677fe4d7d16f and shared Docs were read-only and clean.
No macOS build is claimed on Windows. No language-library or pack changes.

## Commands and measured software results

Run inside WordSuggestorWindows, with repo-local SDK 10.0.403 and installed Swift/
Windows toolchain. The report uses original scripts, not replacement product flow.

| Command | Result |
|---|---|
| `scripts/build_app.ps1` | PASS, 0 .NET warnings/errors |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS 354 + six Danish fixtures |
| `scripts/test_app.ps1 -SkipBuild` | PASS 98, including four reserved probe-path checks |
| `scripts/test_settings_app.ps1 -SkipBuild` | PASS All software/UI suites; native Hello/update excluded by contract |
| `scripts/test_gui.ps1 -SkipBuild` | PASS 54, real editor/owned Notepad, DPI 96, one monitor |
| `scripts/test_media.ps1 -SkipBuild` | PASS 3; actual TTS/OCR, synthetic STT wire |
| `scripts/test_settings_qualification.ps1` | PASS 41 safety/mapping checks |
| `scripts/test_run_scripts.ps1` | PASS 14 owned-fixture checks |
| `scripts/test_release_scripts.ps1` | PASS 48 local fixture/mocked transport + pinned backend validation |
| `scripts/test_clean_checkout.ps1 -IncludeWorkingTree` | PASS separate source-only copy, build, six Core fixtures and 98 app checks; immutable staged-tree archives passed the nine gates above |

```text
PASS Core CLI: 6 real Danish fixtures; no document echo.
PASS CORE SETTINGS 354 checks.
PASS 98 checks. Test data: .artifacts/tests/<isolated-run>
PASS ADVANCED SETTINGS 34 checks; desktop=True; unknown-word lookup/sharing unavailable; Hello deferred.
PASS UPDATE SETTINGS 37 checks; desktop=True.
PASS ACCOUNT SETTINGS 55 checks; desktop=True; no production account mutation.
PASS ANALYSIS SETTINGS 39 checks; desktop=True.
PASS SUGGESTION SETTINGS 48 checks.
PASS SETTINGS SPEECH 102 checks; real native PCM timing/playback; audible quality not human-verified.
PASS WINDOWS CORE SETTINGS 58 checks.
PASS SETTINGS STATE 63 checks.
PASS SETTINGS UI 58 checks; interactive WPF desktop, ordinary user.
PASS SETTINGS SPEECH UI 25 checks; physical Windows input, real editor/Notepad and native playback. Human audible/screen-reader sign-off remains open.
PASS GUI 54 checks. DPI=96; monitors=1. Data: .artifacts/tests/<isolated-run>
PASS MEDIA 3 adapter/contract checks. Full microphone/snipping/highlight tests remain manual.
PASS SETTINGS 41 contract/safety checks. Mock tests do not attest native OS or Hello access.
PASS run scripts: 14 checks; only a synthetic fixture process was closed.
PASS RELEASE checksum mismatch rejected before SSH
PASS RELEASE 48 checks. Synthetic artifacts: .artifacts/tests/<isolated-run>
```

Release fixtures cover unsigned rejection, metadata/hash/path/extra-file errors,
identity/publisher/version/origin/downgrade/unknown/duplicate XML policy, strict
ED25519 pin, structured exit 2–9/SSH failure/malformed replies, unique-ID WhatIf,
default no-trust-mutation gate, native quoting/Unicode/literal shell syntax,
deadline/process cleanup/output cap and mock mkdir/upload/atomic rename/validate/
register/publish ordering. No network/SSH or signing trust is exercised by mocks.
The original backend validator source hash is verified before portable execution.

## Actual package and standalone payload

Optimized Core was built from the archived pinned commit. Its production build
reported existing @Sendable/Swift-mode, SQLite pkg-config, vswhere and symlink
advisories; these were nonfatal and are not disguised as a zero-warning Swift
build. No Core source was changed.

An initial deeply nested export publish failed MSB3030 on runtime satellite DLL
extraction/path lengths. Packaging then ran from the normal shorter repository
path with identical staged source and that same optimized CLI:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1 -PackageVersion 0.2.0.1 -DisplayVersion 0.2.0-beta.1 -BuildNumber 2001 -SkipCoreBuild -CoreCliPath <verified-optimized-cli>
powershell -ExecutionPolicy Bypass -File scripts\test_package.ps1 -AppDirectory <output-app-directory> -SkipBuild
```

Actual artifact: `.artifacts/release/0.2.0.1-9c0625269b2d49fea8e581a65a47e3f2/bundle/WordSuggestor.Beta_0.2.0.1_x64.unsigned.msix` (ignored).
SHA-256: `0d08cf976e228903ef65258115d148c047f649a4bb7dcd029e4e99c64a54276b`.
Identity `WordSuggestor.Windows.Beta`, x64, package `0.2.0.1`, display
`0.2.0-beta.1`, build 2001. Publisher `CN=WordSuggestor` is a template, **not an
approved/provisioned production identity**. MakeAppx validated/created the complete
MSIX. It is explicitly unsigned: actual SignTool trust verification was rejected
and no App Installer file/release manifest was emitted.

The complete self-contained payload copied outside the workspace passed the
actual GUI/editor/Notepad suite with Swift/.NET SDK/source discovery removed from
the child environment. All runtime resources were retained. Core/pack hashes
match build-info and the pinned Danish dependency. This is not clean install or
native installed update evidence. Original failed logs remain ignored evidence.

## Native/manual/publication matrix

| Gate | Status |
|---|---|
| Core DA, candidates, prefix/caret and mouse/keyboard | PASS real loose and standalone payload |
| Editor tabs, native Open/Save, recovery, Notepad focus/commit | PASS automated desktop |
| Local uploader/backend contract and pending/active staging ordering | PASS fixtures/mocked boundary only |
| Unsigned real MSIX rejected by signature gate | PASS; never installed |
| Trusted signed clean installation | NOT RUN; no approved production signer |
| Native automatic/manual N-to-N+1 | NOT RUN; no trusted fixture, no trust mutation authorized for cleanup |
| Wrong signer/identity/publisher/tamper rejected by actual Windows installer | NOT RUN; local validator cases do not close this |
| Native retention/uninstall/interrupted update/rollback | NOT RUN; include editor session/draft retention |
| G05 native update-policy editor | Remains gated read-only; no trusted fixture change/read/restore |
| Hello PIN/biometric | USER-DEFERRED; no native run |
| Human Narrator, high contrast, mixed DPI/multiple monitors, microphone | NOT RUN |
| Production signing + timestamp/trust | NOT RUN |
| Real SSH upload, pending registration, active publication | NOT RUN |
| Public HTTPS artifacts, /beta Windows link, live Mac/Sparkle regression | NOT RUN |

The tab delivery documents initial desktop test failures and synchronization;
this final source passed the same complete uninstrumented desktop gates again.
An initial sandbox-limited release timeout test could not reap its fixture; the
same local test with normal process access passed. No test expectation was
weakened, no failed native gate reclassified as software success.

## Review and next actions

Diff review covers all original 42 files plus scoped fixes/docs, no binary outputs,
private keys, credentials or new machine-specific paths. All three change notes
use the required sections/absolute repo-relative file lists. Only Windows is
committed; no source edits/commits in macOS/Core/backend/shared Docs and no push.

Follow [Release.md](../Release.md) to securely provision signing/approved SSH,
qualify a disposable N-to-N+1 with data retention and human/hardware acceptance,
then register pending and publish an independently reviewed active candidate.
Users will then see the Windows x64 installer on /beta and receive OS-managed
updates. Until those gates pass, official signing/activation remain blocked;
this cleanup does not ask for trust mutation, credentials or public deployment.
