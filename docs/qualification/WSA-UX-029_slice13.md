# WSA-UX-029 slice 13 qualification - 2026-11-13

Windows sharing client is implemented; backend receiver/deployment belongs to the
macOS agent by explicit user instruction. Production source gate false, no endpoint
chosen, A07 disabled/off. See ../UnknownWordSharing.md and the backend agent prompt.
Core pinned to 0278cbc515580f6f9c9e18020e5ed753d9fc7b11; library/macOS source,
Package.swift, language pack and backend remain unchanged. No push/public upload.

## Executed validation

Run from WordSuggestorWindows using powershell -ExecutionPolicy Bypass -File:

- scripts/build_app.ps1 -ArtifactsPath .artifacts/slice13: PASS zero warnings/errors.
- scripts/test_core_cli.ps1 -SkipBootstrap -SettingsContract: PASS six DA fixtures and 675 executable checks.
- scripts/test_settings_app.ps1 -Mode All -SkipBuild -RunnerPath <slice13 runner>: full 15-suite run; see final results below.
- scripts/test_settings_app.ps1 -Mode UnknownSharing ...: final PASS 30 checks.

The final targeted suite follows stronger source-change/expiry handling and store
bounds after the full All run. It uses a real Core-generated packet and fake HTTP
receiver, no customer/account credentials. It covers separate consent, missing
receiver/login, learned-only schemas, malformed/content-bearing fields, stable
idempotency across restart/version, unchanged-store dedupe, account mismatch,
offline/429 backoff, 401 stop, expiry stripping lexical copies, revocation while
sending, source deletion/change, future/corrupt main/backup protection, native
account transport headers and redirect rejection without forwarding credentials.

## Manual / platform matrix

| Flow | Evidence |
|---|---|
| Core handoff and learned-only metadata | PASS actual shared builder / CLI |
| Local outbox, retry/idempotency, account isolation | PASS controlled sender and file tests |
| Actual HTTPS transport request/header shape | PASS existing AccountBackendClient + synthetic HttpMessageHandler |
| A07 deployed backend, live account, server persistence | NOT RUN / receiver not implemented |
| Production enabling, public upload | NOT DONE; source gate remains false |
| Native Windows Hello | USER-DEFERRED |
| macOS runtime | NOT RUN; shared library/app source unchanged |
| Installer / DPI / human accessibility | Subsequent closing slices; not attested here |

Next the macOS agent implements the compatible receiver and returns its exact
path, commit and deployment evidence. Windows must then qualify a real test-account
roundtrip before a reviewed source release enables A07. No hidden environment or
preference bypass exists. This Windows delivery does not close full Settings parity.

## Final results

- Complete Settings All: PASS all 15 suites (source-gated production path).
- Final UnknownSharing: PASS 30 checks after source-change and expiry protections.
- App regression: PASS 98 checks.
- AccountState: PASS (exact count in raw log).
- AutomaticWords: PASS 24 checks, physical editor/Notepad and real Core.
- Control/safety register: PASS 41 checks.
- Final build: PASS zero warnings/errors.

Ignored evidence: .artifacts/slice13-*.log. Production account/receiver, signing,
installed updates, Hello and macOS runtime are not attested by these results.
The final whole-product source export and hardware/manual acceptance record belongs
to slice 15. All changed files are listed in the same-repo change note.
