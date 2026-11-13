# WSA-UX-029: native acceptance results and remaining checks

## Superseded by measured package-integrity follow-up

The user's e281a55453d24952a915c8b2fed4a068 run reached the old payload-tamper
case and failed because Windows registered .4. Its installed bytes were not
established. The corrected test and new enforced-integrity fixture have now been
run: see [current evidence](WSA-UX-029_slice14_package_integrity.md). The earlier
remaining-check command/profile is obsolete; prepare a fresh nonce. The text below
is historical and does not describe the latest run.

Date: 2026-11-13. Base 97bb840. Windows test/release scripts and local docs only.
App/Core/pack/macOS/backend source unchanged. No public upload or production beta.
The user confirms UAC was visible and approved. Successful helper receipts prove
approval in several runs; other ShellExecute launches reported cancellation. The
cause of that discrepancy is not established and is not attributed to the user.

## Proven results across explicit native runs

| Check | Actual result |
|---|---|
| Trusted signature/publisher, ordinary-user clean install | PASS |
| Actual installed Core/DA/user-word suggestions | PASS |
| Automatic .1 to .2 update | PASS in earlier 615eb55aebd0 runs; not repeatable within every 60s observation |
| Actual manual .1 to .2 update and user-data hashes | PASS |
| Readback of settings, tabs, choices, learned overlay, pending handoff | PASS; extracted Core/pack bytes match installed signed payload |
| Higher-version known-good procedure .2 to .3 | PASS cfcf2121 and 833d5d6d, same app source with higher package number |
| Dirty unsaved draft preserved through .3 | PASS actual workspace hash and semantic readback |
| Installed GUI/Notepad, focus/caret/Unicode, restart | PASS 55 checks in cfcf2121 and 833d5d6d |
| Newer .4 feed with wrong identity/publisher | PASS native rejection; installed .3 unchanged |
| Newer .4 feed with missing package / malformed feed | PASS native rejection |
| Corrupted .4 signature | SignTool rejects with 0x80096010; corrected runner not yet through native Add-Appx rejection |
| Older/same-version and dedicated uninstall-retention stage | NOT REACHED |
| Packaged policy write/read/restore (G05) | BLOCKED 0x800706F7, changed=false, restored=true, source/other settings preserved |
| Native cleanup after each test | PASS test registration and temporary leaf trust absent; private key removed |
| Full native/release qualification | FALSE; skipped/blocked/failed checks remain explicit |

Installed learned-word UI is not separately claimed: native app proves Core/DA/
user words; fixture readback of the learned overlay uses hash-matched extracted
Core. GUI account/backend data is synthetic and there is no real sharing receiver.
Actual display is 96 DPI / one monitor; Narrator, mixed-DPI, mic/audio human review
remain open. Hello remains user-deferred and was not prompted.

## Four-package fixture

Prepared receipt: .artifacts/msix-tests/prepared-292715dbca6b/prepared-session.json.
Identity WordSuggestor.Windows.LocalTest.292715dbca6b, publisher
CN=WordSuggestor Local Test 292715dbca6b. Seven-day non-exportable test key deleted
at preparation; only its public certificate remains. Local file feeds are copied
with fixture URIs/interval 0. This is not canonical HTTPS-feed or recipient-trusted
production signing evidence.

| Version / purpose | Actual signed MSIX SHA-256 |
|---|---|
| 0.2.0.1 install | b6df535ba408b6e4d5d07941122b9d4be0dd5b3159ccbf164b80e35f95a9a9cc |
| 0.2.0.2 update | 71a9e7941617238b48ea9b52a9fe034af53f0e018ca10b5ad510757c3826ff23 |
| 0.2.0.3 rollback procedure | c4c00b4f5a2b6a0d4060ff1efc3676bf2bb9ad25e2a8de3067608ff4f6ce47d3 |
| 0.2.0.4 rejection-only candidate | 549fe5dbfa989361f9a949f3920a799b124dbb00786da3da8d9e8eb071c01179 |

The .4 candidate is never intentionally installed as a valid release. It is newer
than installed .3 so rejection tests cannot pass by a same-version no-op. Each
negative XML has its own self-URI and points to .4; the original stable feed is
not overwritten by a negative case. Both actual signing and newer-version bounds
are checked before native tests. Old three-package receipts are rejected before
trust/install; explicit bundle qualification also requires a newer negative bundle.

## Attempts and limitations preserved

- cfcf2121c8cc47178afb6cc2703e3f4b (old 615eb55aebd0 series): rollback/dirty data,
  installed GUI 55 PASS; same-version wrong-identity feed was accepted as a no-op
  and harness stopped. No native identity rejection was claimed from that test.
- 833d5d6df1964b0d88652bc83c5918e3 (new four-package series): rollback/GUI 55 and
  actual newer invalid-feed cases PASS. SignTool correctly rejected tamper, but
  PowerShell 5.1 promoted expected native stderr to a terminating ErrorRecord.
- 7779c96f578b4621b7d5eb162f7cad08: rollback/dirty data PASS; GUI timed out at the
  second-tab mouse selection, a previously observed intermittent test failure.
  No GUI assertion or timeout was weakened.
- 79e213d923bf4ae4b006dba5fdb44119: last new UAC launch reported cancellation;
  no trust/install. No further unattended prompt is left open.

Ignored raw logs: .artifacts/slice14-native-{manual-isolated,four-package,
manual-completion,remaining-checks}.log. Attempt JSON and cleanup receipts live
under matching .artifacts/msix-tests/<id>. Previous synthetic profiles are retained
under their attempt; no ordinary user profile moved/overwritten. Earlier automatic
and manual .2 evidence remains linked in slice14_update_lifecycle.

## Corrected test tooling and software validation

prepare_msix_test reuses existing packaging for .1/.2/.3 plus the higher .4 rejection
fixture. No new release flow or production updater. test_msix requires that candidate,
rewrites only isolated negative feed URIs, and corrupts .4 rather than an already
installed package. Expected SignTool stderr/nonzero status now uses the existing
bounded Invoke-ReleaseProcess wrapper; any zero verifier status or accepted corrupt
installation still fails. No weakened rejection expectation.

Explicit LocalTest-only SkipAutomaticUpdateProbe and SkipInstalledGuiProbe support
independent remaining checks. Skipped gates are recorded false with not-run reasons,
not PASS; a completed partial run exits 3. Production fixtures reject both switches.
Default full behavior remains unchanged. G05 stays read-only, A07 source-gated,
full sprint/release flags false. Individual positive checks are not blanket approval.

- test_release_scripts.ps1: PASS 73, including old-fixture/new-negative requirements,
  expected verifier stderr/nonzero behavior and production rejection of GUI skip.
- Four self-contained packages built and signed through package_app, test key removed.
- Actual SignTool against the corrupted .4 through the corrected process wrapper:
  PASS bad-hash rejection 0x80096010 without PowerShell stderr termination.
- No product C#/Swift edits in this follow-up. The historical full source regression
  does not attest a new full native run. Native installer tamper/version/uninstall
  cases still need operator execution.

Raw software logs: .artifacts/native-{four-package-release,negative-stderr-release,
final-facet-release,four-package-preparation,actual-bad-hash}.log. No private keys/
PFX/passwords/tokens.

## Current operator action

Use [the current integrity qualification procedure](WSA-UX-029_slice14_package_integrity.md#reproduce-and-remaining-acceptance).
Do not rerun prepared-292715dbca6b: it lacks the new mandatory runtime integrity
policy and its user-run synthetic data/evidence must be preserved. The original
partial progress and 'Tampered MSIX accepted' failure are not retroactively PASS.
