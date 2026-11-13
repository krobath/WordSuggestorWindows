# WSA-UX-029 slice 14: signed isolated update fixtures

## Summary

Prepare and inspect a signed x64 N/N+1/rollback series through the existing MSIX
flow. Native installed acceptance awaits explicit temporary-trust approval.

## User-visible behavior

No production UI/default/identity changes. Maintainers can prepare signed but
untrusted test packages before deciding whether to authorize a device trust change.
The current app remains usable; official beta publication stays gated.

## Implementation details

prepare_msix_test provisions an isolated non-exportable seven-day user-store key,
reuses package_app for three versions and deletes the key in finally. Preparation
sets no trusted attestation. test_msix validates prepared ownership/identity,
requires explicit trust opt-in, then uses existing TTL leaf-trust cleanup, real
SignTool, extended synthetic data readback and higher-version rollback. Required
native-policy/automatic-update failures remain partial exit 3. Tampered current
version is tested independently of downgrade rejection. Production uploader and
backend O-052 contracts unchanged. RetentionDataSmoke exercises actual stores and
bundled Core; it refuses arbitrary roots/existing seed data and never uploads.

## Files changed

- `/README.md`
- `/docs/Architecture.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-13_wsa_ux_029_slice14_msix_qualification.md`
- `/docs/qualification/WSA-UX-029_slice14.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/package_app.ps1`
- `/scripts/prepare_msix_test.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/RetentionDataSmoke.cs`

## Feature flags / settings impact

No schema/default changes. G05 production mutation remains read-only; sharing
source gate false. Hello remains user-deferred. Test data uses separate LocalTests
nonce roots and false capture/lookup/sharing preferences.

## Logging / telemetry impact

Only fixed test results, public signer/identity, hashes and synthetic test profile
metadata. No private key/password, document content, account token or HTTP upload.
Generated artifacts/logs remain ignored.

## Validation performed

Build PASS zero warnings/errors; release tools PASS 56 including backend's pinned
validator; standalone self-contained payload PASS 54 GUI checks; real Core/store
retention seed/check/dirty/check-dirty PASS. Three signed package hashes/versions/
publisher inspected; trusted verification rejects all three untrusted packages.
Private key removed, device trust unchanged, no installation. Default native
harness exit 3 BLOCKED. See ../qualification/WSA-UX-029_slice14.md for exact hashes.

## Known limitations / follow-up

Clean installed launch, native automatic/manual update, policy write/read/restore,
installed retention/rollback and uninstall NOT RUN pending explicit trust approval
on a test account/VM. Production signer/access unavailable. Same-source .3 only
exercises rollback procedure. Next independently qualify final source/desktop
regression in slice 15; maintainers still need actual installed evidence before
users can receive automatic beta updates. No public feed or backend is changed.

## Consolidation notes for `WordSuggestorDocs`

Later summarize Windows-only tooling and native evidence gap. Do not mark Settings
sprint/release complete or copy isolated signer details into macOS identity docs.
No shared documentation/backend/macOS edits in this slice.
