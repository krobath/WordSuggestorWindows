# WSA-UX-029: passive update-control observation without package reactivation

Date: 2026-11-14. Scripts READY; installed passive experiment NOT RUN.
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

From the WordSuggestorWindows root, run the fresh-session command recorded below
once. Do not reuse earlier USED identities c371a4bbd8f8, af00e7ebeb63 or cd8326885d3a.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test_msix.ps1 `
  -PreparedLocalTestSession .\.artifacts\msix-tests\prepared-f970146e46d9 `
  -SkipBuild -UpdatePolicyDiagnostic -UpdatePolicySettingsUiProbe `
  -UpdatePolicySettingsUiOnly -PassiveSettingsUpdateObservation `
  -AllowTemporaryMachineTrust
```

Verified preparation: identity `WordSuggestor.Windows.LocalTest.f970146e46d9`,
publisher `CN=WordSuggestor Local Test f970146e46d9`; four signed versions .1-.4,
all bundle checks pass. Session unused, no installed package or machine trust,
private test key removed. Certificate expires 2026-11-21T21:05:22Z. This mode
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

## Decision after the operator run

If the control returns to On during passive sampling, package reactivation is
not required for that observed reset in this experiment. If it stays Off until
the human reopens app details, the refresh/navigation interval becomes the next
focus. Either result still needs native/UI interpretation; unchanged native flags
are not evidence of an effective saved Off policy. A missing Advanced options
route is a manual-navigation limitation, not evidence that the update setting
works. No outcome by itself resolves native 0x800706F7 or qualifies actual update
suppression/resumption and N-to-N+1 preference retention.
