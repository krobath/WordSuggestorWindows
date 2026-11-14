# WSA-UX-029: passive update-control observation without package reactivation

Date: 2026-11-14. Installed passive experiment COMPLETED; G05 NOT qualified.
Follows the [measured UI/native mismatch](WSA-UX-029_g05_manual_control_roundtrip.md).
No G05 production checkbox or release gate enabled.

## Question being tested

The previous run measured Off twice before packaged activation/navigation and On
twice afterwards, while native flags were unchanged in both samples. This new
experiment separates a reset over time from one observed after HUMAN navigation
within Windows Settings. It does not reactivate the test app after the initial
page launch, automate clicks or invoke a setter. Reading is not proof of the
semantics of every internal Windows disable mechanism.

The harness first installs a NEW disposable LocalTest .1 package and uses the
already qualified own-package launcher ONCE to open its details page. After the
first human change, the new observer accepts no runner/URI argument and calls no
launcher, navigation or activation helper. All subsequent navigation and optional
restoration are human actions on ONLY this test app.

## Run procedure

Use ordinary, non-administrator PowerShell on the unlocked desktop. Close
unrelated Settings windows. Existing temporary test-leaf trust/uninstall cleanup
applies. Approve the trust-helper UAC dialog if Windows requests it.

The former command for prepared-f970146e46d9 is RETIRED: this session is USED.
Do not rerun it or remove its marker/profile. No new fixture is prepared in this
evidence update. The procedure below records the completed diagnostic, not an
instruction to repeat it. All earlier listed LocalTest identities are also USED.

Verified preparation: identity `WordSuggestor.Windows.LocalTest.f970146e46d9`,
publisher `CN=WordSuggestor Local Test f970146e46d9`; four signed versions .1-.4,
all bundle checks passed before use. Initially unused, no installation/trust,
private test key removed. Now used and cleaned up. Certificate expires
2026-11-21T21:05:22Z. This mode
installs .1 only; the prepared setter-experiment worker is never invoked.
If the session expires or is used, prepare a NEW one through the existing scripts;
never remove its marker or reuse its profile.

1. At **PASSIVE G05 step 1/3**, switch ONLY the console-named WordSuggestor
   LocalTest app to the requested opposite state, normally Off. LEAVE IT there
   and press Enter in PowerShell. Do not click or navigate in Settings during
   the following measurements. Four UI/native samples are scheduled at 0, 5, 10
   and 15 seconds. UI reads, native calls and desktop delays can extend that
   duration; this is a bounded count/schedule, not a hard timeout for OS calls.
2. Wait for **step 2/3**. Use Windows Settings' own Back navigation to
   **Apps > Installed apps**. Find the SAME uniquely named WordSuggestor LocalTest
   app, open its `...` menu and **Advanced options / Avancerede indstillinger**.
   Do not launch WordSuggestor or change its update control. With that SAME
   details page visible, return to PowerShell and press Enter. The script only
   reads the newly verified page; it does not reopen it itself. If you cannot
   locate that exact app/page, abort rather than change another app's control.
3. At **step 3/3**, verify the SAME app's control is in its original state,
   normally On. If already correct, leave it unchanged; otherwise restore it
   manually. Press Enter for readback and cleanup.
4. Wait for the qualification receipt/cleanup. Send the `G05 manual phase=passive`,
   `phase=after_manual_navigation`, `phase=restoration`, `G05 passive` lines and
   receipt path. Exit 3/partial-qualification warning remains expected after a
   completed diagnostic; a failure can stop earlier while retaining evidence.

If the initial state is Off, follow the printed opposite/original states instead
of assuming an On-to-Off sequence. Ctrl+C attempts the existing finally cleanup;
confirm actual receipts rather than assuming interruption guarantees cleanup.
The script never restores a UI setting automatically.

## Evidence and interpretation

Receipt mode: `update_policy_settings_passive_observation`.
Object: `passive_settings_observation`. It records initial/requested state and
safe initial native values; four `samples` with requested time offsets and actual
elapsed milliseconds; `after_manual_navigation`; final restoration readback.
Each phase reacquires the exact package-family control on verified app_details,
reads its On/Off state twice, and attempts a native snapshot even on UI read
failure. Unknown pages are not treated as the target.

- `first_requested_ui_observed`: both first UI reads match the human's requested
  state. False alone does not establish why they differ.
- `passive_ui_transition_observed`: known On/Off reads differ during passive
  sampling. Interpret alongside `passive_ui_reads_complete`; missing reads are
  not proof of a stable state.
- `passive_native_reads_succeeded`: actual successful native snapshots out of four.
  `passive_native_change_observed` is null when all failed, not false/no-change.
- `observation_completed` means the three human checkpoints were reached, NOT
  that every read succeeded or the update policy saved. Review errors/samples.
- `ui_state_restored` and `native_snapshot_restored` are observed final equality
  with the baseline, not an automatic restore. Drift is explicit in
  `observation_completed_ui_not_restored` / `observation_completed_native_changed`.
- A wrong/unreadable manually reopened control stops with
  `manual_navigation_unverified`; passive samples remain in the receipt.
- `g05_qualified`, `release_qualified` and full qualification remain false.

Comparisons ignore only last-check timing. Stored summaries are boolean flags,
a bounded check interval and recognized policy-source values. Changed property
names can include a URI field but raw URI values and exception messages are never
emitted. No raw UI captions, document content, credentials or telemetry.

## Validation and limits

`test_release_scripts.ps1`: PASS 188. New guards reject production, elevation,
unverified initial details and use outside the isolated UI-only context. Eight
simulations cover unchanged native state, a time-based UI reset, persistent policy
source drift, all native reads unavailable, unavailable UI, wrong manual page,
interruption and starting Off. Activation/navigation helpers throw if called by
the observer in those simulations. These tests mock timing; they do not qualify
real elapsed-time accuracy, the Windows page or update behavior.

Existing two-phase diagnostic remains available separately; the passive switch
cannot be combined with it. No runtime app source, framework, Core/pack contract
or backend change. No fresh full GUI regression or production publication.

Ignored evidence: `.artifacts/g05-passive-{release,prepare}.log` and prepared
session/release directories. Raw receipts and generated signing artifacts do not
belong in source control. Preserve used markers/profiles.

## Measured installed acceptance, 2026-11-14

Run c1bb06333862449ca783ba299ccae40d completed 22:19:41Z to 22:23:21Z
(23:19:41 to 23:23:21 Copenhagen), tested .1 under f970146e46d9.
Clean install and ordinary user passed. Initial verified app_details/control
launch succeeded. The observer invoked no package activation or URI launch after
the first human change; the operator confirms reopening Advanced options manually.

| Checkpoint | UI first / stable | Native changes |
|---|---|---|
| Initial | On | baseline on-launch/background=true, source=Default |
| Passive sample, elapsed 784 ms | Off / Off | none |
| Passive sample, elapsed 5759 ms | Off / Off | none |
| Passive sample, elapsed 10746 ms | Off / Off | none |
| Passive sample, elapsed 15740 ms | Off / Off | none |
| After human Settings navigation | On / On | none |
| Final | On / On | none |

first_requested_ui_observed=true, passive_ui_transition_observed=false,
passive_ui_reads_complete=true, passive_native_reads_succeeded=4 and
passive_native_change_observed=false. There were no read-failure substitutes;
all changed-fields arrays are empty. Native on-launch/background flags stayed
true and PolicySource=Default, including while UI read Off. Thus a visual Off
state is still not verified as a saved/effective disabled-update policy.

status=observation_completed, ui_state_restored=true,
native_snapshot_restored=true, completed=true. The partial warning is expected:
all_checks_passed=false, g05_qualified=false, release_qualified=false.
Completing the diagnostic does not qualify G05, actual update suppression or
N-to-N+1 preference retention.

Tested .1 SHA-256: 13997b6f5a7fde01109f88a159d59060a5498cc78c262e32b207938de688a4fa.
Raw receipt SHA-256: 1bc15150dcc8f0ce6ce0c0815134006dd39df1b095967723b93e955971922b6c.
Trust removed=true. Independent checks find no installed test package, private
signing key or machine TrustedPeople leaf; used marker retained.

Observed Off was stable during the sampled 15.74 seconds, and the next observed
state was On after human navigation. This shows our package reactivation is not
required for that observed sequence. It does NOT prove that navigation caused
the change, exclude a longer timer, or observe the interval between the last
passive sample and the human navigation. No source/native Off policy was measured
beforehand. Do not attribute the discrepancy to WordSuggestor startup, an OS
regression, VM configuration or the separately reproduced 0x800706F7 from this alone.

Follow-up UTC event query 22:19:41Z through 22:23:25Z: AppxPackaging 145 total /
5 test-package events; DeploymentServer 34/23; AppModel-Runtime 15/8. No associated
warning/error events and no known RPC-code message/EventData matches in those
channels. This does not establish absence of errors in other providers.

See the [local support dossier](WSA-UX-029_g05_support_dossier.md) for component
versions, source references and the two separately bounded symptoms. No report
is sent externally by this work.

## Decision after the operator run

The passive question is answered within its sampled scope. Do not repeat the same
GUI run or enable the product toggle from this result. Focus subsequent work on
the save/read path and a narrowly scoped deployment-service reproduction, keeping
the native write failure separate from the Settings UI symptom until correlated.
HTTPS source equivalence and read-priority/disable semantics remain unqualified.
No outcome here resolves actual update suppression/resumption or N-to-N+1 retention.

The subsequent [API semantics audit](WSA-UX-029_g05_api_semantics.md) closes the
metadata inventory and adds explicit pause-state diagnostics without a new GUI
run. It does not retroactively establish pause or Disable values in this receipt.
