# WSA-UX-029: human-controlled Windows update setting experiment

Date: 2026-11-14. Follows the [verified read-only discovery](WSA-UX-029_g05_packaged_settings_page.md).
Status: the two-phase installed run verified Off twice BEFORE reopening and On
twice AFTER reopening. Native settings were unchanged in BOTH phases, with the
on-launch/background flags still true and PolicySource=Default. This confirms
a UI/native mismatch, not its cause or a successful saved Off state. The next
[passive observation](WSA-UX-029_g05_passive_observation.md) has now completed
without diagnostic reactivation: Off during four samples, On after human Settings
navigation, native values unchanged. G05 stays read-only.

## Purpose and boundaries

Find out whether Windows can change its own per-app update setting even though
our native setter fails with 0x800706F7. A human changes ONLY the disposable
WordSuggestor LocalTest app's update control, then restores its starting state.
The script FIRST reads the current verified details page and native state before
any packaged activation/navigation, THEN reopens that same app's details page and
reads both again. Each phase reacquires UIA elements and reads state twice, 500 ms
apart. It never calls Toggle(), a native setter, SendInput or
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

The former command for prepared-cd8326885d3a is RETIRED: the session is USED.
Do not rerun it, remove its marker, or reuse its profile. No replacement session
is prepared by this evidence-only update. The next independent experiment should
first be designed to observe without reactivating the package, not repeat the
same two-phase run. The procedure below records how this completed diagnostic
was operated; a future run always requires a fresh isolated identity.

1. Wait for the console's `MANUAL G05 step 1/2` and requested state. The script
   requires the correct details page, a unique exact package-family control ID,
   an enabled On/Off control and an initial native snapshot before prompting.
2. In Windows Settings change the update control for the uniquely named
   **WordSuggestor LocalTest** app shown in the console. Do not change another application.
   **LEAVE IT in that state. Do not switch back yet.** Return to PowerShell and
   press Enter while the control is still in the requested state. The script
   reads UI/native state BEFORE reopening, then reads it again AFTER reopening.
   No extra click is needed during these measurements.
3. Wait for the separate `MANUAL G05 step 2/2` console instruction. Only now
   restore the SAME app's original state as
   requested, return to PowerShell and press Enter again. The starting state may
   be On or Off; follow the measured sequence rather than assuming On.
4. Wait for the receipt and cleanup. Copy the `G05 manual` lines and receipt path
   back for analysis. Do not rerun the same prepared session. A phase mismatch
   stops with an error and retained samples. A completed diagnostic still exits
   3 with the partial-qualification warning; it does not qualify a release.

If the exact app/control cannot be verified, the test stops. If you abort with
Ctrl+C, cleanup is attempted; confirm its actual receipts rather than assuming
that interruption guarantees cleanup. No automatic UI restoration is attempted.
Do not manually change unrelated system policies to make this test pass.

## Recorded evidence and interpretation

The main qualification receipt uses mode `update_policy_settings_manual_roundtrip`
and a `manual_settings_roundtrip` object, including when the manual probe fails.
Earlier code assigned the mode only after success; the first failed manual run
therefore has the generic update_policy_diagnostic mode. UI-only runs retain their prior mode.
Each step retains `before_reopen` and `after_reopen` samples with observed and
stable UI state, safe native summary/deltas or fixed read failures. The full
bounded page/launch evidence is retained in `reopen`. Native reads are attempted
even when UI state differs, and phase evidence is saved before reporting a
mismatch. The existing step observed/native fields mirror the AFTER sample.
This only measures the installed session around a packaged activation/URI launch,
not actual update suppression or retention across N-to-N+1.

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

`unexpected_state_before_reopen` or `unexpected_state_after_reopen` means the
current step's requested state was not observed in that phase; it does not
establish that Windows rejected a human change. In particular,
changing Off then On before the first Enter leaves nothing for the Off checkpoint
to measure. `unstable_state_before_reopen` / `unstable_state_after_reopen` records
a change between the two reads in that phase. `ui_read_failed_*` identifies a
failed phase read; `reopened_page_unverified` preserves the pre-navigation sample
but does not sample controls from an unverified page. Previous raw receipts keep
their former `*_on_reopen` codes unchanged.

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

## Corrected operator sequence still mismatched, 2026-11-14

Run 538b428b00324ac68198a72b84ba90d7, 16:38:14Z to 16:39:03Z
(17:38:14 to 17:39:03 Copenhagen), tested .1 under af00e7ebeb63.
The operator explicitly confirms: started On, switched Off and pressed Enter
while leaving it Off. This differs from the first attempt and must not be
attributed to the earlier instruction misunderstanding.

Clean install and ordinary user passed. Header/specifications/version and the
exact control on app_details were verified, hosted route, launch_completed,
zero UI node errors. The first requested Off checkpoint instead read On after
packaged activation/navigation; status unexpected_state_on_reopen. There was no
before-navigation sample, no native snapshot at the mismatched checkpoint, and
no second manual step. We cannot distinguish an earlier reset, navigation or
activation effects, or a UI Automation discrepancy from that receipt.

Initial/final UI=On; initial/final native settings matched, PolicySource=Default;
ui_state_restored=true and native_snapshot_restored=true describe observed equality,
not an automatic restoration. completed=false, all_checks_passed=false and
release_qualified=false. This is not an off/on success or a G05 setter test.

Tested .1 SHA-256: 34ceda51b4f813f7c456518017b7dd2e5dd1e6d4f7ffcd1473f1cf1fed8e3f82.
Raw receipt SHA-256: 3efc4e7a24bf993077d845b35c7cd64be7f1f1e8d4754aa2d5a7c42c931c5ab5.
Trust added=true / removed=true. Independent checks: no installed package,
signing key or machine TrustedPeople leaf; used marker retained. af00e7ebeb63 is
USED. Its former command is retired; do not reuse it or remove its marker/profile.

## Measured UI/native mismatch around reopening, 2026-11-14

Run 373fb3ee03c04d28a168c8bd055545af, 20:53:49Z to 20:55:00Z
(21:53:49 to 21:55:00 Copenhagen), tested .1 under cd8326885d3a.
The operator confirms the visible control returns to On after switching Off and
pressing Enter. Actual structured evidence:

| Checkpoint | UI first / stable read | Native configuration |
|---|---|---|
| Initial | On | on-launch=true, background=true, source=Default |
| After human Off, BEFORE activation/navigation | Off / Off | unchanged; both flags true, source=Default |
| AFTER activation/navigation | On / On | unchanged; both flags true, source=Default |
| Final | On | unchanged; no final native field differences |

Both phases reacquired the verified app's exact control. The native property
names are CheckForUpdatesOnLaunch and AutomaticBackgroundTaskUpdatesEnabled.
All native changed-field arrays were empty, not null/read failures. Correct
app_details/header/specifications/version, exactly one control, hosted route,
both packaged launch receipts and zero node errors were verified. The script did not reuse
a cached UIA state: new reads actually captured Off before reopening. This differs
from the earlier missing-control and two-click sequence problems. UI Automation
provider semantics are not independently qualified by this evidence.

The visible transition lies between the two phase samples, which include package
activation and Settings navigation. The sample does NOT show a native Off policy
being overwritten by reopening: the getter already reported unchanged values
before reopening. Whether the getter represents every internal disable mechanism,
whether a write failed or was delayed, and which component is responsible remain
unresolved. Do not equate a visible Off state with actual update suppression.

Status=unexpected_state_after_reopen, one manual step, ui_verified=false.
ui_state_restored=true / native_snapshot_restored=true describe observed final
equality; no automatic restoration was performed. completed=false,
all_checks_passed=false, release_qualified=false, g05_qualified=false.

Tested .1 SHA-256: 9507059048f91627ba54f1eb00948ae9dc323c8d4d7d7f257cff278f0a68e5af.
Raw receipt SHA-256: d0560d85d7a0031afa9c69db4014ad36f636ace7fad98f0530b10fab7137b533.
Trust removed=true. Independent checks find no installed package, signing key or
machine TrustedPeople leaf; used marker retained. cd8326885d3a is USED.

Read-only event-log follow-up used explicit UTC SystemTime XML bounds
20:53:49Z through 20:55:05Z. The initial PowerShell DateTime filter returned no
events and was discarded; the UTC query found real events and was cross-checked
against their latest timestamps. Results, restricted to this package:

| Log | All events in interval | Package-associated events | Package warnings/errors |
|---|---:|---:|---:|
| AppxPackaging/Operational | 149 | 9 | 0 |
| AppXDeploymentServer/Operational | 32 | 25 | 0 |
| AppModel-Runtime/Admin | 25 | 12 | 0 |

The alternate AppXDeployment and AppXDeployment-Server operational channels had
no events in the interval. No 0x800706F7, 0x000006F7 or RPC_X_BAD_STUB_DATA match
occurred in actual message/EventData payloads in the three populated channels.
System XML Keywords masks are not HRESULTs and were excluded from that check.
Absence here does not establish that no error occurred or link the UI mismatch
to the separately reproduced native setter failure. No raw messages, captions,
other-app content or event-log exports were retained in source.

## Preparation and validation

- `test_release_scripts.ps1`: PASS 175. Cases cover context/production guards,
  redacted delta comparison, source drift, simulated off/on with two reopenings,
  interruption, native read failure while off, first-Enter mismatch, Off/native
  deltas retained before reopening followed by On afterwards, a failed pre-read
  with native data retained and an unverified reopened page. These are simulations;
  the real installed two-phase diagnostic remains NOT RUN.
- Historical preparation: existing scripts prepared signed versions 0.2.0.1 through .4 with
  capability-experiment identity `WordSuggestor.Windows.LocalTest.cd8326885d3a`,
  publisher `CN=WordSuggestor Local Test cd8326885d3a`. This UI-only mode installs
  .1 only and invokes none of the native setter matrix cases.
- Preparation receipt key_removed=true. Before use: no installed package,
  signing key, machine TrustedPeople leaf or used marker. After the measured run,
  cleanup again finds no package/key/trust, with the used marker now present.
  Certificate expires 2026-11-21T16:42:21Z. If expired/used, prepare a NEW
  session through the existing script and update the command; never remove a
  used marker or reuse the identity/profile to obtain a clean result.
- Qualified Core/DA payload reused; no linguistic logic or runtime source change.
  No new full GUI regression or production signing/publication performed.
- Ignored evidence: `.artifacts/g05-phases-{release,prepare}.log`, the prepared
  session and generated release bundles. No binaries, certificates or raw
  operator artifacts belong in git.

## Next decision

Microsoft's [auto-update overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview)
documents Windows Settings as a supported configuration route for apps with an
App Installer repository entry. It does not identify the cause of this measured
mismatch or establish that the Settings route works on this machine.

The [passive observation runbook](WSA-UX-029_g05_passive_observation.md) now
implements the next distinct experiment: sample the verified UI/native state after
the human flip WITHOUT another app activation/URI launch, then compare a human
reopening within Windows Settings. The installed passive result
now verifies Off over 15.74 seconds and On after human navigation, with unchanged
native settings. The diagnostic completed; no G05 product qualification follows. This would distinguish a time-dependent
reset from one around the diagnostic's package activation/navigation, without
automating clicks or mutating policy. It would still not identify a root cause
or qualify automatic-update behavior. Native/UI disagreement must remain visible.
If a reliable route is eventually qualified while the setter still fails, decide
whether opening Windows' own app settings is an acceptable product fallback;
do not silently automate that screen. Actual automatic update and user-preference
retention across N-to-N+1 still need separate verification. The original native
RPC failure is unresolved; this experiment does not attribute it to the VM.
