# WSA-UX-029: human-controlled Windows update setting experiment

Date: 2026-11-14. Follows the [verified read-only discovery](WSA-UX-029_g05_packaged_settings_page.md).
Status: first installed manual attempt INCONCLUSIVE: the operator performed both
changes before the first Enter. Separate Off and On checkpoints remain NOT RUN.
The clearer two-step prompt and fresh fixture prepare the retry. This does not
enable the product's automatic-update checkbox.

## Purpose and boundaries

Find out whether Windows can change its own per-app update setting even though
our native setter fails with 0x800706F7. A human changes ONLY the disposable
WordSuggestor LocalTest app's update control, then restores its starting state.
The script reopens that same app's verified details page and reads UI/native
state after each change. It never calls Toggle(), a native setter, SendInput or
registry mutation. It does not change global Windows update settings.

An observed off/on state is evidence about that control, not proof that updates
actually stop/resume or that the preference survives N-to-N+1. G05 and production
release qualification remain false, including after a successful roundtrip.
No macOS, Core, pack-format, backend or production identity change.

## Operator procedure

Use an ordinary, non-administrator PowerShell on this unlocked Windows desktop.
Close unrelated Settings windows. The existing temporary LocalMachine TrustedPeople
test-certificate approval applies; approve a visible trust-helper UAC dialog if
Windows requests one. The human UI changes below concern ONLY the new LocalTest
package, which the script removes afterwards.

From the WordSuggestorWindows repository root run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 `
  -PreparedLocalTestSession .\.artifacts\msix-tests\prepared-af00e7ebeb63 `
  -SkipBuild -UpdatePolicyDiagnostic -UpdatePolicySettingsUiProbe `
  -UpdatePolicySettingsUiOnly -ManualSettingsUpdateRoundtrip `
  -AllowTemporaryMachineTrust
```

1. Wait for the console's `MANUAL G05 step 1/2` and requested state. The script
   requires the correct details page, a unique exact package-family control ID,
   an enabled On/Off control and an initial native snapshot before prompting.
2. In Windows Settings change the update control for **WordSuggestor LocalTest
   af00e7ebeb63** to the requested state. Do not change another application.
   **LEAVE IT in that state. Do not switch back yet.** Return to PowerShell and
   press Enter while the control is still in the requested state. The script reopens the verified page
   and checks the state twice, then reads native settings.
3. Wait for the separate `MANUAL G05 step 2/2` console instruction. Only now
   restore the SAME app's original state as
   requested, return to PowerShell and press Enter again. The starting state may
   be On or Off; follow the measured sequence rather than assuming On.
4. Wait for the receipt and cleanup. Copy the `G05 manual` lines and receipt path
   back for analysis. Do not rerun the same prepared session. Exit 3 and the
   partial-qualification warning remain expected for this diagnostic.

If the exact app/control cannot be verified, the test stops. If you abort with
Ctrl+C, cleanup is attempted; confirm its actual receipts rather than assuming
that interruption guarantees cleanup. No automatic UI restoration is attempted.
Do not manually change unrelated system policies to make this test pass.

## Recorded evidence and interpretation

The main qualification receipt uses mode `update_policy_settings_manual_roundtrip`
and a `manual_settings_roundtrip` object, including when the manual probe fails.
Earlier code assigned the mode only after success; the first failed manual run
therefore has the generic update_policy_diagnostic mode. UI-only runs retain their prior mode.
Each requested/observed state is recorded separately. Reopening verifies UI
persistence within this installed session, without an update or relaunch test.

Native comparisons ignore ONLY LastCheckedForUpdates timing. They retain changed
property names, including policy-source or URI changes, but never URI values or
raw exception messages. Safe summaries contain boolean properties, a bounded
check interval and recognized Default/System policy-source values. Local native
snapshot values used for comparison are not emitted wholesale.

If the native getter becomes unavailable while the control is off, the step
records a fixed exception category/HRESULT and null changed-fields. The second
human restoration step still proceeds. Null is NOT evidence of no native change.
The final read separately reports `ui_state_restored` and
`native_snapshot_restored`. A source difference can persist despite the original
UI state: `ui_restored_native_changed` reports it honestly. There is no hidden
setter to reverse it. `restoration_read_failed` means restoration could not be
fully measured. `completed` means only the two manual steps completed; review
their native-read results and restoration booleans. Every result retains
`g05_qualified=false`.

`unexpected_state_on_reopen` means the current step's requested state was not
observed; it does not establish that Windows rejected a human change. In particular,
changing Off then On before the first Enter leaves nothing for the Off checkpoint
to measure. `unstable_state_on_reopen` records a change between the two reads.

## First installed attempt and operator clarification

Run 3dab31945b9c4756aac3123fdcb98d40, 2026-11-14T16:20:08Z to 16:22:15Z
(17:20:08 to 17:22:15 Copenhagen), tested version .1 under c371a4bbd8f8.
Clean install and ordinary user passed; app_details/control and hosted own-package
launch were verified without UI errors. Initial UI was On; at the first requested
Off checkpoint, the observed reopened UI was On. The script stopped before step 2.
The operator then clarified: they switched On to Off and back to On before Enter.
That explains the observed mismatch; no claim about Off persistence or a Windows
write failure follows. The previous wording about "not retained" was too strong.

Actual final UI=On, ui_state_restored=true, native_snapshot_restored=true, with
no final native field differences. Initial/final native flags and PolicySource=
Default matched. The old step status awaiting_manual_change is retained in the
unaltered original receipt, not reinterpreted as waiting after termination.
completed=false, all_checks_passed=false, release_qualified=false.

Tested .1 SHA-256: 70857be772a793143c2c53bf0bfb049b8350f23519e84bdc54911bd8fb9861cc.
Raw receipt SHA-256: 75903122ea4182f8a17aabdc68b231eb4431dbbcb5f4fdc558fea652bae22aaf.
Trust-cleaned removed=true; independent checks find no installed package,
signing key or machine TrustedPeople leaf. The used marker remains.
c371a4bbd8f8 is USED; do not rerun it or remove its marker/profile.

## Preparation and validation

- `test_release_scripts.ps1`: PASS 172. Cases cover context/production guards,
  redacted delta comparison, source drift, simulated off/on with two reopenings,
  interruption, a native read failure while off and the first-Enter mismatch.
  Simulations do not qualify
  the real Windows control or its TogglePattern support.
- Existing packaging script prepared signed versions 0.2.0.1 through .4 with
  capability-experiment identity `WordSuggestor.Windows.LocalTest.af00e7ebeb63`,
  publisher `CN=WordSuggestor Local Test af00e7ebeb63`. This UI-only mode installs
  .1 only and invokes none of the native setter matrix cases.
- Preparation receipt key_removed=true. Independent check: no installed package,
  no CurrentUser signing key, no machine TrustedPeople leaf, no used-session
  marker. Certificate expires 2026-11-21T16:24:41Z. If expired/used, prepare a NEW
  session through the existing script and update the command; never remove a
  used marker or reuse the identity/profile to obtain a clean result.
- Qualified Core/DA payload reused; no linguistic logic or runtime source change.
  No new full GUI regression or production signing/publication performed.
- Ignored evidence: `.artifacts/g05-manual-two-steps-{release,prepare}.log`, the prepared
  session and generated release bundles. No binaries, certificates or raw
  operator artifacts belong in git.

## Next decision

After the operator run, compare Windows' native flags and policy source with the
visible control state. If that route works while the setter still fails, decide
whether opening Windows' own app settings is an acceptable product fallback;
do not silently automate that screen. Actual automatic update and user-preference
retention across N-to-N+1 still need separate verification. The original native
RPC failure is unresolved; this experiment does not attribute it to the VM.
