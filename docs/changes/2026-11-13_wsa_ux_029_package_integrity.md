# WSA-UX-029: enforced package integrity and measured native tamper outcomes

## Summary

Investigate the user's historical Settings build failure and actual accepted
payload-corruption fixture. Harden the MSIX and distinguish invalid signatures
from original payload reuse using complete installed-content verification.

## User-visible behavior

New packages request Windows runtime content integrity enforcement. The app,
Settings layout, data schema, Core/pack logic and update consent are unchanged.
The current app and a fresh build compile without errors/warnings; no obsolete
OpenInstaller API is restored. No Windows Hello prompt or official publication.

## Implementation details

Add uap10:PackageIntegrity/Content Enforcement=on to the existing manifest at the
unchanged Windows 19041 minimum. The shared release validator requires that exact
policy before packaging/publication qualification. Old signed test fixtures must
be regenerated; this is not a signing bypass or a new update mechanism.

msix_integrity supplies bounded corruption fixtures and installed-content checks.
The native harness requires SignTool AND native rejection of changed signature
footprints. A separate newer payload-only corruption may either be rejected or
install only original signed bytes: every block-map-listed installed file must
match the trusted archive in SHA-256 and size before any launch. Invalid paths,
duplicates, missing files and altered content fail. Original trust is established
by the existing Test-ReleaseBundle -VerifySignature gate. Production staging
still requires an intact trusted source archive, irrespective of OS block reuse.

Check old/same version before the independent payload case. Uninstall acceptance
now checks missing registration, exact retained hashes and semantic Core/store
readback, replacing existence-only retention checks. Do not edit WindowsApps ACLs
or installed files. Preserve the user's failed e281 attempt as failure; its
removed installed payload could not be inspected retrospectively.

## Files changed

- `/README.md`
- `/docs/Architecture.md`
- `/docs/FinalAcceptance.md`
- `/docs/ManualSmoke.md`
- `/docs/ParityMatrix.md`
- `/docs/Plan.md`
- `/docs/README.md`
- `/docs/Release.md`
- `/docs/UiParityPlan.md`
- `/docs/changes/2026-11-13_wsa_ux_029_package_integrity.md`
- `/docs/qualification/WSA-UX-029_slice14_native_acceptance.md`
- `/docs/qualification/WSA-UX-029_slice14_package_integrity.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/packaging/AppxManifest.xml`
- `/scripts/msix_integrity.ps1`
- `/scripts/release_common.ps1`
- `/scripts/test_msix.ps1`
- `/scripts/test_release_scripts.ps1`

## Feature flags / settings impact

No settings/consent/identity/publisher/minimum-OS migration. G05 stays read-only;
Hello positive verification remains user-deferred; unknown-word sharing stays
source-gated/off pending the macOS-owned backend. Skip flags remain isolated
LocalTest-only and false in native results; neither can qualify production.

## Logging / telemetry impact

No telemetry/upload or user-content logging added. Ignored qualification output
contains fixed outcomes, file count, fixture DLL name/byte offset, versions,
public identity/certificate metadata and hashes. Synthetic retention content is
local test data. No PFX, private key, credential, document text or binary committed.

## Validation performed

- build_app.ps1: PASS 0 warnings/errors, including a fresh ArtifactsPath build.
- test_settings_app.ps1 -Mode UpdateState: rebuilt successfully, PASS 26.
- test_app.ps1 -SkipBuild: PASS 98.
- test_release_scripts.ps1: PASS 94, including 21 new policy/hash/corruption checks.
- prepare_msix_test.ps1: built four signed self-contained x64 fixtures with the
  enforced policy; non-exportable ephemeral key removed.
- Actual authorized Windows run 64c8c10eb8654aa69d42ebee180c9e4b: install/Core,
  manual .1-to-.2 update, dirty rollback .3, data readback, invalid newer feeds,
  corrupt signature rejection, downgrade rejection, same-version stability and
  uninstall-retention PASS. Payload-corrupt .4 registered, but all 441 installed
  payload files matched the approved signed original. No unverified code launched.
- 19 native checks true; G05 blocked and three deliberately skipped flags false.
  completed=true; all_checks_passed=false; release_qualified=false. No full native
  release PASS claimed. See the qualification record for the wrapper exit nuance.
- Cleanup independently verified: no test registration/key/leaf trust/app process.
- Other four repository worktrees remain clean. See
  /docs/qualification/WSA-UX-029_slice14_package_integrity.md for commands, fixture
  hashes, raw-log locations, sources, prior failure and the exact result matrix.

## Known limitations / follow-up

Current build errors are not reproduced; the pasted output predates 92f2ed5's
update-controller change. Do not claim Hello native acceptance from it. G05 native
policy mutation returns 0x800706F7. The new partial run intentionally did not
retest automatic update/desktop/relaunch on this hardened fixture; earlier results
are historical. Runtime protection was declared/installed, but no protected
installed-file tampering experiment was performed. Supported minimum OS, human
Narrator/mixed DPI, interrupted update and unknown signer need separate acceptance.

Next, users need the complete install/update/desktop path qualified together on
the intended test Windows setup, with G05 resolved. Official beta still requires
the stable trusted production signing identity and canonical HTTPS feed/publication.
Real unknown-word sharing requires the macOS-owned receiver; it is not enabled.
Prepare a fresh nonce to rerun instead of deleting prior synthetic data/evidence.

## Consolidation notes for `WordSuggestorDocs`

Windows-only implementation evidence belongs here. Consolidate the new integrity
policy, 441-file verified payload outcome, uninstall retention PASS and remaining
native/human/external gates by link. Do not advertise a public beta, full sprint
completion, production signing or macOS parity qualification. macOS/Core/backend/
shared Docs source untouched, so no corresponding platform commit is needed.
