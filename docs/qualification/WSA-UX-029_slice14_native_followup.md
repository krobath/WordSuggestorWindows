# WSA-UX-029 slice 14: authorized native installation follow-up

Latest: [native acceptance evidence](WSA-UX-029_slice14_native_acceptance.md)
records successful rollback, dirty-draft retention, installed GUI 55 and newer
invalid-feed rejection. Earlier blocked/not-reached statements below are history.
The user confirmed earlier UAC approval; no cause of later cancellations is assumed.


Latest: [native update lifecycle evidence](WSA-UX-029_slice14_update_lifecycle.md)
now records successful automatic/manual .2 updates and data retention, rollback
blocked by package-in-use, and the pending UAC visibility question. Earlier
not-run statements below are historical checkpoints.


## Latest rerun (2026-11-13, base 0da9334)

User resumed the native test and approved UAC. Attempt
`.artifacts/msix-tests/3076da95dba94228970ac531d5e08f3b` passed clean_install,
standard_user and core_suggestions through the corrected fresh receipt gate
(probe_exit_code=0, probe_result_present=true). It then failed synthetic fixture
seeding: the unpackaged smoke process cannot CreateProcess the protected
WindowsApps/Core executable (Win32 error 5). The installed app itself had already
successfully executed that Core. This is a test setup boundary, not evidence of
an installed engine failure. No WindowsApps permissions were changed.

The harness now unpacks each already verified signed MSIX with existing MakeAppx
into its ignored session directory, compares extracted Core executable and bundled
pack SHA-256 against the installed files, and uses that payload for synthetic
seed/check/check-dirty. App/Core/user-word proof still comes from the installed
app via fresh receipts. Learned-overlay readback is through the byte-matched
extracted Core; this is not an extra native learned-word UI claim. Actual native
execution of the new hash comparison still requires the next installation run.

The previous synthetic profile is preserved in that attempt's retained-profile.
Package and temporary trust cleanup are independently verified. Next attempt
`.artifacts/msix-tests/da237d1c8fb64cacb92b8e71f2515ddf` was canceled in UAC;
no certificate was added and no package installed. Overall both records remain
completed=false / all_checks_passed=false / release_qualified=false.

Local correction validation: release gates PASS 68; MakeAppx extracted the actual
signed .2 MSIX, Core matches build-info SHA, and real Core/store seed/check/dirty/
check-dirty all PASS on a fresh synthetic profile. No installation or trust change
in that independent software check. Raw logs `.artifacts/native-retention-release.log`,
`native-retention-payload.log`, `slice14-native-receipt-rerun.log` and
`slice14-native-retention-rerun.log`. Earlier receipt-failure history below is retained.
N to N+1, automatic update, policy, rollback and installed retention remain NOT RUN.


Date: 2026-11-13. Base aa450f5. The user explicitly approved temporary leaf trust
and installation on this PC. No production signer, publication or Hello consent
was inferred. App/MSIX bytes and macOS/backend/Core/pack sources are unchanged.

## Actual native results

Final LocalTest identity `WordSuggestor.Windows.LocalTest.615eb55aebd0`, publisher
`CN=WordSuggestor Local Test 615eb55aebd0`; package hashes match slice15 evidence.

| Check | Actual evidence |
|---|---|
| Temporary leaf trust | UAC approved for first attempt; helper added only its non-CA leaf to LocalMachine TrustedPeople |
| SignTool /pa /all /v + Authenticode + exact publisher | PASS for all three prepared packages while temporary trust was present |
| Clean App Installer installation of 0.2.0.1, ordinary user | PASS native harness clean_install and standard_user |
| Installed bundled Core/DA and synthetic user word | Actual app result schema1/ok=true with exact package name/version 0.2.0.1 |
| Complete installed probe gate | First harness rejected activation/result handoff despite eventual positive result; not a full native PASS |
| Native package/trust cleanup | PASS package absent, TrustedPeople leaf absent, trust-cleaned.json removed=true |
| Private signing key | Remains deleted; public certificate only |
| Second diagnostic attempt | Windows UAC cancellation; no trust addition/installation |
| N to N+1, automatic update, policy, rollback, installed data retention | NOT RUN; native harness stopped before these steps |
| Production release / Windows Hello | NOT QUALIFIED / USER-DEFERRED |

First attempt: ignored `.artifacts/msix-tests/d5978e49f505475db9421f66c5c5bc99`.
Record: completed=false, all_checks_passed=false, release_qualified=false,
test_trust_cleaned=true. The positive Core result is preserved under that attempt's
retained-profile/diagnostics, with other synthetic profile data moved there before
retry. No ordinary WordSuggestor user profile was moved or overwritten.
Second canceled attempt: `.artifacts/msix-tests/4e50d997e35d4820b75263771cf01eff`.
Raw logs: `.artifacts/slice14-native-approved.log` and native-diagnostic.log.
These records intentionally retain failures; they are not rewritten as PASS.

## Qualification tooling correction

Only test/release tooling changes. The activation helper now treats successful
COM activation as dispatch only. It does not try to recover an exit status from
a short-lived externally activated process. The parent requires a newly generated
reserved qualification-GUID result path, waits at most 30 seconds and validates a
closed schema, numeric schema version, Boolean positive result, exact installed
identity/version and null error. Missing/malformed/negative/oversized/unexpected
results fail. A PID/activation-success message alone cannot qualify the app.

The first failure's exact process-status cause was not captured by the old helper;
this correction removes that dependency, but native rerun is still required to
qualify the complete flow. Bounded exception class/HRESULT and probe exit/presence
metadata are now recorded; no raw app output/document/credential is logged.

## Local validation

- build_app.ps1: PASS zero warnings/errors.
- test_release_scripts.ps1: PASS 68 checks (56 existing plus 12 receipt checks).
- test_app.ps1 -SkipBuild: PASS 98.
- test_settings_qualification.ps1: PASS 41.

New tests cover exact successful shape/identity/version, numeric schema, negative
result, contradictory error, unknown content fields, malformed/oversized output,
unreserved path, missing-result timeout and delayed result from a real child
process. These software tests do not attest installed updates or native G05.
Raw evidence: `.artifacts/native-receipt-{build,release,app,safety}.log`.

## Next action

The latest UAC was reported canceled by Windows. The user has been asked whether
to open a new dialog with the corrected fixture or park the native test. Do not
retry that dialog until answered.
When resumed, use the same prepared final app series and corrected test runner:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_msix.ps1 -PreparedLocalTestSession .artifacts/msix-tests/prepared-615eb55aebd0 -SkipBuild -AllowTemporaryMachineTrust
```

Run from WordSuggestorWindows with an unlocked desktop. Preserve nonempty previous
fixture data rather than deleting it. Then require actual install, N+1, rollback,
policy and data results and verify cleanup. In product terms this is what will
prove users keep their documents/settings while installed beta versions update;
public release, G05 mutation and backend sharing remain closed meanwhile.
