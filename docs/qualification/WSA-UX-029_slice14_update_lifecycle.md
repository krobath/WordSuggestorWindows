# WSA-UX-029: native update lifecycle evidence

Date: 2026-11-13. Base 20e63b1. Only Windows qualification scripts/docs change.
The app, signed fixture MSIX bytes, Core/library/pack, backend and macOS are unchanged.
The user authorized temporary local leaf trust and remained available for UAC.

## Observed native results

Same final isolated series: WordSuggestor.Windows.LocalTest.615eb55aebd0,
CN=WordSuggestor Local Test 615eb55aebd0, versions 0.2.0.1/.2/.3. Package hashes
remain those in slice15. Tests use copied local file feeds with rewritten fixture
URIs and on-launch interval 0; this is not public canonical HTTPS-feed acceptance.
No production signing or publication is attested.

| Check | Result |
|---|---|
| Trusted signing, per-user clean .1 install, installed Core/DA/user word | PASS |
| Extracted retention Core/pack hashes against installed files | PASS native |
| Automatic .1 to .2 installation after shell activation | PASS in 8825 and 4220 attempts below |
| Actual manual .1 to .2 install | PASS in 2293 attempt after automatic window elapsed |
| Settings/pack/user-word and extended profile hash retention | PASS in 8825 and 2293 |
| Readback of tabs, word choices, learned overlay and pending handoff | PASS through real stores and hash-matched extracted Core |
| Installed .2 Core/DA/user word after update | PASS in 8825 and 2293 |
| Automatic timing on every run | NOT QUALIFIED: 2293 did not update within 60-second observation |
| Higher-version rollback / dirty draft through .3 | BLOCKED by Windows 0x80073D02 (package in use) |
| G05 packaged policy, installed GUI/negative feeds/full uninstall sequence | NOT REACHED in these runs |
| Test package removal / removal of temporary leaf trust | PASS, independently checked |
| Official beta / Hello / real sharing backend | NOT QUALIFIED / USER-DEFERRED / unavailable |

Manual update in 8825 ran after automatic .2 and was a same-version operation;
2293 separately proves an actual manual .1 to .2 change. Do not count the former
as a separate manual upgrade. Neither run proves rollback or a full passing native
suite. Learned-overlay readback uses identical extracted Core bytes; installed
learned-word UI is not separately attested. Installed Core/user-word proof uses
the actual packaged app and fresh exact-identity/version receipts.

## Attempt records (ignored)

- 8825fc4dd5d8475d8e1797ddc36a82da: automatic update + hash/readback retention PASS,
  then rollback .3 rejected 0x80073D02. Native record incomplete/false. Log
  .artifacts/slice14-native-final-rerun.log.
- 42205e3fcb194aa88ee0d8bfed5c88b1: automatic .2 observed, then owned-app graceful
  close timed out. Incomplete/false. Log native-owned-close-rerun.log.
- 229343e3e8fe4b0a88c66c18f160d913: automatic observation expired; actual manual
  .2 + data retention PASS; rollback rejected 0x80073D02. Windows deployment event
  638 reported the fixture AUMID still running. Log native-toolbar-close-rerun.log.
- db267ef403f8420c91acf0ea3af34b90: Windows reported UAC canceled, no trust/install.
  Log native-rollback-close-rerun.log.

All attempt directories are under .artifacts/msix-tests; log basenames above have
slice14- prefix except the first explicitly spelled path. Previous synthetic data
was moved only from the exact owned LocalTests/615eb55aebd0 directory to the
corresponding retained-profile under its attempt. No ordinary user profile was
moved/overwritten. Initial failure history remains in slice14_native_followup.

## Qualification changes and affected software gates

The harness now reuses app_process.ps1's existing exact-path/start-time checked
normal toolbar shutdown, waits for GUI input-idle, and invokes it between phases
and immediately before writing offline rollback fixtures. No force-stop flags,
editor termination, auto-confirmed Save/Cancel or WindowsApps ACL changes.
Rollback resource-in-use is retained as a blocker, not converted into PASS; the
latest immediate pre-fixture close still needs a native rerun.

An explicit -SkipAutomaticUpdateProbe is limited to LocalTest fixtures. It allows
independent manual install/rollback/negative/UI qualification without shell-launching
the automatic lane. It records automatic_update=false and a not-run reason; the
run remains incomplete as an overall gate (exit 3 if other checks finish). Default
full behavior is unchanged, and production fixtures reject the switch before
provisioning/data changes. Existing production uploader and contracts unchanged.

- test_release_scripts.ps1: PASS 69 (including production rejection of manual mode).
- test_run_scripts.ps1: PASS 14, identity/cancel/normal-close safety, synthetic app.
- No product .NET/Swift source or new app build in this follow-up. Prior full
  source regression remains evidence for its original snapshot, not this harness.

Raw affected-gate logs: .artifacts/native-manual-mode-release.log and
native-manual-mode-close.log. Intermediate release gates also passed 68 before
the added manual-mode guard. No native manual-only run yet.

## Next operator action

The pending user question asks whether the latest UAC dialog was actually visible.
Read-only session inspection showed the test process in the active Windows RDP
session, so approval must be supplied there with its desktop visible. This is a
possible explanation of cancellations, not proof of why Windows canceled them.
Do not bypass UAC or infer approval from the default option/time elapsed.

After visibility/readiness is confirmed, run from WordSuggestorWindows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_msix.ps1 -PreparedLocalTestSession .artifacts/msix-tests/prepared-615eb55aebd0 -SkipBuild -AllowTemporaryMachineTrust -SkipAutomaticUpdateProbe
```

Leave the explicit skip off for the default combined automatic lane. Preserve any
nonempty earlier fixture profile before rerun. Review actual rollback/dirty data,
policy, installed GUI, negative-signature/feed/version and cleanup results. In
product terms these prove users can safely replace an installed beta with a
higher-numbered known-good release while retaining documents and local words.
Until then rollback/native Settings acceptance stays open, G05 read-only, full
sprint/release flags false. Public HTTPS/signing/backend/human/Hello gates retain
their separately documented ownership and limits. No app or backend is published.
