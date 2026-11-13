# WSA-UX-029 slice 15: reproducible software and accessibility acceptance

## Summary

Complete the independent closing software regression and document the remaining
native/backend/human acceptance dependencies. Fix actual empty accessible editor
names discovered by the new Settings layout matrix.

## User-visible behavior

Settings row editors expose their localized labels to assistive technology,
including after changing interface language. Explicit control names/read-only
value text remain intact. No learning consent/default, feature availability,
backend, package identity, language pack or macOS UI behavior changes here.

## Implementation details

SettingsRow tracks only the accessibility names it supplies, updating them with
the row title. SettingsLayoutSmoke uses actual WPF peers and native UI Automation
for two synthetic account states, two locales, eight categories and minimum /
default sizes, plus enabled-control focus/scroll and offscreen raster scales.
The fake account transport keeps the matrix independent of real accounts/secrets.

Existing test_clean_checkout gains FullRegression using existing sequential gates,
source-only snapshot/pinned Core, per-gate logs/statuses, software-source.sha256
and regression.json. A nonzero/blocked gate stops the run; software qualification
and release qualification stay separate. Default short mode does not qualify the
full suite. Existing All includes Layout and excludes native Hello/update mutation.
No second release/build flow, public upload or backend contract is added.

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
- `/docs/changes/2026-11-13_wsa_ux_029_slice15_final_software_acceptance.md`
- `/docs/qualification/WSA-UX-029_slice15.md`
- `/docs/sprints/WSA-UX-029_closeout_slices12_15.md`
- `/docs/sprints/WSA-UX-029_windows_settings_sidebar_and_functional_parity.md`
- `/scripts/test_clean_checkout.ps1`
- `/scripts/test_settings_app.ps1`
- `/src/WordSuggestorWindows.App/Controls/SettingsRow.cs`
- `/tests/WordSuggestorWindows.Smoke/Program.cs`
- `/tests/WordSuggestorWindows.Smoke/SettingsLayoutSmoke.cs`

## Feature flags / settings impact

Schema 9 unchanged. A06 implemented with separate automatic consent; A07 remains
source-gated/unavailable, G05 partial/read-only, Hello user-deferred. Full
sprint_complete/release_qualified remain false. No hidden enable switch.

## Logging / telemetry impact

Ignored software logs contain fixed check names, synthetic fixtures and local
artifact paths. Public fixture metadata/hashes contain no private key or tokens.
No production document/account content, automatic upload or new telemetry.
Source fingerprints cover code/config/assets, excluding Markdown/output caches.

## Validation performed

Full source-only export regression PASS all 14 sequential gates: zero-warning
build, Core 675 + six DA fixtures, app 98, Settings All 16 suites (layout 1201,
automatic 24, sharing 30), five separate state suites, native GUI 54, media 3,
control/safety 41, restart 14, release 56. Old unchanged client/Core adapter PASS
58. Final self-contained signed-candidate payload PASS GUI 54 with SDK/source
discovery removed. Source fingerprints match final non-Markdown files.

Three final LocalTest MSIX versions (.1/.2/.3) signed with matching isolated
identity, inspected; full trusted verification correctly rejects all three.
Temporary private key removed, no device trust change or installation. No
production signing/feed/publication. Precise counts/hashes and raw-log locations
are in the same-repo qualification document.

The first full clean-source run failed on second-tab mouse selection timeout;
unchanged same-export repeat and final signed-candidate standalone payload both
passed all 54 GUI checks. Preserve that initial failure; do not claim a proven
product fix. Layout first isolated a real empty-name defect, corrected above.
No test assertion/GUI timeout was weakened. See exact commands, final source hash
list, signing hashes and manual matrix in ../qualification/WSA-UX-029_slice15.md.

## Known limitations / follow-up

UIA peers and offscreen raster scales do not attest human Narrator/high contrast
or physical mixed-DPI/multiple monitors. Actual desktop 96 DPI / one monitor.
Hello remains user-deferred. Installed MSIX/update/rollback still awaits explicit
trust approval on a test account/VM; G05 cannot be enabled without real native
policy evidence. Production signer/SSH/feed/public beta remain unavailable/unrun.
Backend receiver belongs to macOS agent; a real test-account roundtrip is required
before Windows enables sharing. Current local signed fixtures are untrusted and
cannot be uploaded as an official beta. No private key retained.

In practice the next steps let users transfer opted-in words to a reviewed
receiver and install trusted automatically updated beta releases. Maintainers
must first obtain the real endpoint/deployment and signer/test environment and
complete the documented human checks. Users can use the existing editor/settings
and offline/local learning; no public sharing or release is silently enabled.

## Consolidation notes for `WordSuggestorDocs`

Later consolidate the four Windows delivery checkpoints and remaining acceptance
gaps, linking local FinalAcceptance/qualification and slice13 backend prompt.
Keep Windows implementation/runbooks here. Shared Docs, backend and macOS source
were not edited; shared Core library/Package.swift/pack format remain unchanged.
Native macOS runtime and live DMG/Sparkle are not attested by Windows tests.
