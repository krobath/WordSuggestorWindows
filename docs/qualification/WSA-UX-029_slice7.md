# WSA-UX-029 slice 7 — account and native Hello qualification

Date: 2026-11-11. Windows base `8db859a`, Core `defa19b` unchanged, backend source
`2f67ddc6`, canonical macOS Settings `ba18b45`. Controlled WPF extension; existing
uncommitted tabs/restart/release files/hunks preserved outside the slice candidate.
See [account, wire, privacy and recovery contract](../AccountAndHello.md).

## Audit and scope decision

The previous Profile page only had unavailable P02/P07 placeholders. No Windows
account/backend/credential service existed. Retain the functioning WPF/Core shell
and add isolated Windows adapters. The backend already supplies all required
endpoints, flags, profile IDs and snake_case Jackson mapping; no parallel API or
backend edit is needed. Core/pack format/ranking remain unchanged.

## Automated and native smoke

Final results below supersede intermediate working-tree builds/counts. Native
input, synthetic HTTP, native credential storage and actual Hello are separate
evidence categories; no mock result is promoted to a platform PASS.

| Flow | Evidence / limit |
|---|---|
| Login, config/flag gating, snake_case, verified profile/entitlement | Actual HTTP serializer with injected synthetic server PASS; live account not tested |
| Registration/resend/cooldown; no implied browser login | Contract/state PASS; deployed email delivery not tested |
| Real Credential Manager read/write/delete | Unique synthetic fixture PASS, ordinary-user desktop |
| Hello setup/reopen/encryption, failure/cancel, locked passive rejection | Mock cryptography/state PASS; not native PIN/biometric evidence |
| Native Hello availability and full explicit gate | Available=false; `HelloNative` exit 3 BLOCKED; no enrollment changed |
| Profile UTF-8/drafts/catalog IDs and partial conflict | State and physical WPF save PASS |
| Password/current credential, email change/pending/cancel | Synthetic contract PASS; no production mutation |
| Offline local/locked logout and bounded secure revocation | State PASS; current/all-device routes remain distinct |
| Native all-device confirmation cancel/confirm | Physical mouse + native modal No/Yes PASS |
| Lock/view close/late response/single-flight/idempotent refresh | Contract/state PASS; actual OS lock/Hello acceptance remains manual |
| Narrator/contrast/extra DPI/monitors and MSIX Hello | Not run; no human/hardware/install signoff claimed |

## Reproduction

```powershell
powershell -ExecutionPolicy Bypass -File scriptsuild_app.ps1
powershell -ExecutionPolicy Bypass -File scripts	est_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts	est_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts	est_settings_app.ps1 -Mode AccountState -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts	est_settings_app.ps1 -Mode All -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts	est_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts	est_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts	est_settings_qualification.ps1
# Separate explicit interactive native gate; not part of unattended All:
powershell -ExecutionPolicy Bypass -File scripts	est_settings_app.ps1 -Mode HelloNative -SkipBuild
```

## Final exact-source qualification

All seven software gates passed from fresh Git archives on 2026-11-11. Candidate
Windows tree `297ee1c2170a394db9905f4028769ebfcbe569bd` and committed pinned Core `defa19bd` were exported
to new sibling directories; no working-tree source, binary or build cache was
copied. Windows and Core were built inside that export. Final updates are docs
only; complete non-documentation Git-object-list SHA-256 is
`648579ea939b55fcba1ad88f2cf0e399dd125517a8070e4071792bddefba0861` and is checked unchanged before commit.

| Command | Result |
|---|---|
| `scripts/build_app.ps1` | PASS; C# build 0 warnings / 0 errors |
| `scripts/test_core_cli.ps1 -SettingsContract` | PASS; 6 real DA fixtures + 335 executable protocol checks; Core unchanged |
| `scripts/test_app.ps1 -SkipBuild` | PASS; 67 checks |
| `scripts/test_settings_app.ps1 -SkipBuild` | PASS; Account 55, Analysis 39, Suggestions 48, Speech 102, Core adapter 58, State 63, UI 58, Speech UI 24 |
| `scripts/test_gui.ps1 -SkipBuild` | PASS; 31 physical desktop/editor/Notepad checks; DPI 96, one monitor |
| `scripts/test_media.ps1 -SkipBuild` | PASS; 3 adapter/contract checks; microphone/snipping/highlight remains manual |
| `scripts/test_settings_qualification.ps1` | PASS; 35 contract/safety checks; mocks do not attest OS/Hello access |
| `scripts/test_settings_app.ps1 -Mode HelloNative -SkipBuild` | **BLOCKED, exit 3**; native key service unavailable; no positive PIN/biometric claim |

The account gate tests the actual transport serializer against injected synthetic
HTTP responses, real Credential Manager in a unique fixture namespace, and real
WPF mouse/confirmation controls. It does not mutate a production account. The
native Hello gate is separate from the seven software PASS results. It remains
blocked and the slice is an implementation candidate, not full platform signoff.

Regression fixes include harmless late lock/suspend after disposal, cancellation
of stale auth responses, conditional **whole-row** registration/verification/email
visibility, restored unverified-account resend routing, partial profile-save
reporting, and secret-store failure without an unprotected fallback. Existing
expectations were retained. Earlier passing intermediate trees/counts are not
substitutes for this candidate's exact source.

Pre-existing tabs/restart/release changes remain outside the commit. Therefore
speech-tab UI is explicitly not run in this clean source; document identity is
still contract-tested. Staged text/source review checks whitespace, secrets,
machine paths, binaries, file scope and local links. Non-slice tracked baseline
files remain byte-identical. Backend/macOS/Core/shared docs have no changes.

Ignored local evidence: `.artifacts/slice7-index-a73b4967fbb147258f5bfc76a13bbf26/results.json`,
gate logs and `hello-native.log`. No generated binaries, logs or test credentials
are committed. The standard da_DK pack and pinned Core revision remain unchanged.

## Remaining external qualification and next step

Use a local unlocked Windows session with an enrolled Hello PIN/biometrics and
rerun `HelloNative`. It uses only synthetic credentials and records a positive
roundtrip only if the real protected create/reopen/recovery/rotation/off path
completes. Do not send PINs or account secrets through chat. Then test actual
cancel/lockout/hardware and the packaged/unpackaged boundary. A dedicated authorized
backend test account is still required for live email/session-device acceptance.
Keep the candidate's positive Hello/live-account gate open. Next engineering slice
implements General's actual App Installer update authority, not a local toggle.
