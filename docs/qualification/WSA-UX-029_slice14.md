# WSA-UX-029 slice 14 - isolated signed update qualification tooling

Date: 2026-11-13. Windows base 888058b, pinned Core 0278cbc515580f6f9c9e18020e5ed753d9fc7b11.
Code/tooling delivered; installed/native acceptance remains pending explicit trust
approval. This is not an official beta or completed release qualification.

## Actual results

| Check | Result |
|---|---|
| Build app + retention runner | PASS, zero warnings/errors |
| Existing release/backend O-052 gates | PASS 56 checks |
| Self-contained x64 payload, SDK/source paths removed | PASS 54 GUI checks |
| Synthetic profile seed/read/check-dirty with real bundled Core | PASS all four modes |
| Three matching signed LocalTest packages | PASS signer/identity/version/hash checks |
| Trusted signature gate against these untrusted packages | PASS rejection; no false attestation |
| Prepare -WhatIf / native harness without trust opt-in | PASS no provisioning; native exit 3 BLOCKED |
| Temporary key removed, device trust unchanged, no package installed | PASS readback |
| Clean installed launch, N to N+1, automatic update, native policy | NOT RUN, awaiting explicit trust approval/test account |
| Higher-version rollback, installed retention, uninstall | NOT RUN, same dependency |
| Production signing, SSH staging/public feed | NOT DONE, no approved signer/access supplied |
| Windows Hello | USER-DEFERRED |

Isolated identity: `WordSuggestor.Windows.LocalTest.ce949ebe8b19`.
Publisher: `CN=WordSuggestor Local Test ce949ebe8b19`. Versions 0.2.0.1, .2, .3.
All three contain the same known-good app source: .3 is a rollback procedure
fixture, not evidence of a different historical binary being rolled back.

| Version | Actual MSIX SHA-256 |
|---|---|
| 0.2.0.1 | 08f4e252dcebab18269030ffca368645f3cd3db7f10ebcaebd8c6155306fd790 |
| 0.2.0.2 | 2466801dddf54645fc200b1f04a695b1d61c900425acb634a99ba6d99f092643 |
| 0.2.0.3 | 30dbbadc085f7514e08cc792b126bad13ab6b611df8830600e55d64788fa04fe |

Ignored raw evidence: `.artifacts/slice14-prepare-final.log`,
`slice14-prepared-verification.{log,json}`, `slice14-retention.log`,
`slice14-payload.log`, `slice14-release.log`, `slice14-no-trust.log`.
The prepared-session receipt is under `.artifacts/msix-tests/prepared-ce949ebe8b19`.
It contains public metadata/paths, no private key. Do not commit generated packages.

## Architecture and safety

Extend existing package_app/test_msix/release gates rather than adding another
release system. Preparation is limited to pending LocalTest identity, short-lived
non-exportable signing key, public certificate and local file feed. It deliberately
records signature_verified=false / not_checked until actual trusted verification.
Normal production signing, HTTPS feeds and uploader rules are unchanged. Test
identities cannot be uploaded as official beta bundles.

The native harness checks trust with SignTool /pa /all /v and signer match before
attestation, then attempts clean install, update, data readback, higher-version
rollback, packaged native policy write/read/restore, negative feeds/signature/version
cases, relaunch and uninstall. A false automatic-update or policy result yields
partial exit 3, not success. G05 stays read-only. Test leaf trust requires explicit
operator consent; the TTL helper never adds root trust and removes only its own
addition. Installed test data is isolated by nonce. Existing installations/data
are refused, not overwritten. Production rollback/draft fixtures are rejected.

Retention fixtures use real stores for settings, saved tabs/selection, dirty
recovery, word choices, user lexicon, learned overlay and a synthetic pending
handoff. No network/credentials. Software readback is not installed-update proof.
Tamper testing uses the currently installed version, avoiding downgrade as an
alternative reason for rejection.

## Next action

Use the procedure in [Release.md](../Release.md#native-test-qualification-without-production-credentials)
on an approved ordinary-user test account/VM. The user must authorize temporary
leaf trust before running the prepared session with AllowTemporaryMachineTrust.
Afterward review the actual msix-qualification.json, trust cleanup, install/update
and data results. Only an approved production signer and successful native gates
can make the Windows beta install/update flow ready for public publication.
While that approval is pending, slice 15 can qualify the independent source and
desktop regression; neither record may convert missing native evidence to PASS.
