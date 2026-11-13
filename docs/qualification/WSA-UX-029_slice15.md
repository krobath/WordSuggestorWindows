# WSA-UX-029 slice 15 - final Windows software acceptance

## Authorized native follow-up (2026-11-13)

Native tests now verify automatic and actual manual .1-to-.2 updates, installed
Core/user words, and retained settings/tabs/learning/sharing fixtures. Rollback
remains blocked by Windows package-in-use; automatic timing was not repeatable
within every observation window. The harness reuses normal owned-app shutdown
and offers an explicit LocalTest-only manual lane that never qualifies automatic
updates. Package/trust cleanup is verified. Full native/release acceptance stays open.
See [exact results and next action](WSA-UX-029_slice14_update_lifecycle.md).
Earlier pending-approval/no-install statements are historical preparation results.


Date: 2026-11-13. Windows base b0d0eec, Core pinned to
0278cbc515580f6f9c9e18020e5ed753d9fc7b11; macOS reference ba18b45.
Final source-only snapshot regression: PASS all 14 sequential gates in one run.
Full sprint/release acceptance remains false while external/human gates are open.

## Implementation and cross-platform boundary

Keep WPF/.NET 10, separate Settings sidebar/editor/overlay, Core CLI and SQLite
packs. The only product change in this slice is SettingsRow's explicit accessible
name for unnamed interactive row editors. Native ComboBox peers exposed an empty
Name despite LabeledBy; the row now supplies its localized title and updates only
its own name on language change. Explicit editor names and read-only value text
are preserved. New layout tests verify actual peers and native UIA.

Existing test_clean_checkout now supports FullRegression: source-only Windows
snapshot plus committed pinned Core, existing sequential gates, exact per-gate
logs/statuses, non-Markdown source hashes and a final JSON receipt. It makes no
index change and never enrolls Hello, installs a package, adds trust or uploads.
All includes Layout and still excludes native Hello/update mutation. Synthetic
account transport covers signed-out/signed-in layout without real credentials.

Windows-only UI/tests/docs. No backend/macOS/shared Docs/shared Core library,
Package.swift, ranking/normalization or pack-format changes. Earlier slices 12/13
added CLI-only operations with old-client compatibility. This is Windows evidence,
not native macOS execution or public Mac download/Sparkle regression evidence.

## Reproducible commands

From WordSuggestorWindows, after the documented toolchain prerequisites:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test_clean_checkout.ps1 -FullRegression
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -Mode Layout
powershell -ExecutionPolicy Bypass -File scripts\prepare_msix_test.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_package.ps1 -AppDirectory '<candidate app directory>' -SkipBuild
```

This pre-commit qualification uses FullRegression + IncludeWorkingTree. It copies
source only, builds Core from its commit, retains installed toolchain prerequisites,
and labels the receipt working_tree_snapshot. The final software hash list must
match committed non-Markdown files. No generated outputs enter Git.

## Final results

Final source snapshot: `.artifacts/clean-49b4cb03e5b9`, Windows base b0d0eec plus
this slice's working source; Core 0278cbc. Started 2026-11-13T01:14:07.9437265+00:00,
finished 2026-11-13T01:19:16.6668925+00:00. All 14 sequential gates exit 0, software_qualified
true, release_qualified false. No source build caches were copied into the export.
`software-source.sha256` matches the final reviewed non-Markdown working source;
its own SHA-256 is `74c2dbf0ab2d16bfb9cbbe61867a1bd33e81977526ecd786fc1cbf9469db7811`. Documentation-only updates follow these tests.

| Gate | Actual result |
|---|---|
| Clean source build | PASS zero warnings/errors |
| Core CLI contracts and DA batch/JSON fixtures | PASS 675 checks + 6 real Danish fixtures |
| App regression | PASS 98 |
| Settings All | PASS all 16 suites, including layout 1201 / AutomaticWords 24 / UnknownSharing 30 |
| Suggestion / analysis / account / update / advanced state | PASS 27 / 30 / 44 / 26 / 24 |
| Native editor/Notepad GUI | PASS 54, DPI 96, one monitor |
| Media | PASS 3: real OCR, actual TTS process, STT encoding/tab contract (no mic recording) |
| Control/safety register | PASS 41 |
| Restart/build-run process safety | PASS 14, synthetic fixture only |
| Release tools / pinned backend validator | PASS 56 |

Settings All exact counts: Layout 1201, AdvancedUi 34, UpdateUi 37, AccountUi 55,
AnalysisUi 39, Suggestions 48, Speech 102, Core 58, CommittedWord 79, State 63,
Ui 58, SpeechUi 25, UnknownWords 38, UnknownWordsUi 47, AutomaticWords 24,
UnknownSharing 30. Human/platform gaps remain unchanged by these counts.

The first full run clean-a8d66bb677bb stopped on GUI second-tab selection timeout
after all earlier software gates passed. The exact unchanged export then passed
all GUI 54 checks in slice15-gui-retry.log. No assertion or timeout was changed.
The final whole run is recorded separately; the original failure is retained.
This is a transient test observation with no proven root cause, not a fixed bug.

Native layout: PASS 1201 checks (both account states, two locales, eight categories,
minimum/default window sizes, help/editor names, native UIA and enabled-button
focus/scroll). WPF raster scales 100/125/150/200% PASS; actual desktop remains
96 DPI and one monitor. Windows registry build 26300.9974 / DisplayVersion 26H2;
this does not attest other supported OS versions or physical mixed DPI.

Final standalone signed-candidate payload: PASS GUI 54 checks outside workspace
with .NET/Swift SDK discovery removed. Earlier unchanged Windows adapter: PASS 58
against current Core. Core batch/JSON fixtures prove interface parity on Windows;
macOS runtime fixtures were not executed. Full Swift XCTest is not claimed.

## Final local signing fixtures

The final UI correction is included in prepared-615eb55aebd0; the earlier slice14
fixture remains historical source before that correction. Same isolated identity
`WordSuggestor.Windows.LocalTest.615eb55aebd0`, publisher
`CN=WordSuggestor Local Test 615eb55aebd0`, numeric versions .1/.2/.3. All three
contain the same app source, exercising a higher-version rollback procedure.

| Package version | MSIX SHA-256 |
|---|---|
| 0.2.0.1 | 0ed3a8a7b282178605cf35c0c9bb7045396ecb311a2584d18ae26a05fee88b4d |
| 0.2.0.2 | 859cf5e0aeae5ddac2f463a7ee4907182b827a1f4bc70c31325a8c56b2f89ebf |
| 0.2.0.3 | fc8270999bec7158c6353d7caaca071b6bcca07b8199a58897905494258c9d7e |

Signer present/matching; full trusted verification correctly rejects each.
signature_verified remains false. Ephemeral private key removed; no trust entry,
installation, SSH upload, update association or public feed created. Raw logs and
public receipt stay ignored under .artifacts/slice15-* and msix-tests/prepared-*.
An additional unsigned .4 staging build was used to verify packaging; never published.

## Manual / platform acceptance matrix

| Flow | Result and practical limit |
|---|---|
| Core/da_DK, user words, learned overlay | Automated actual CLI + stores PASS |
| Internal editor, Unicode/prefix/caret, tabs/recovery | Actual desktop GUI automation PASS; first transient failure retained |
| Mouse and Ctrl+1 in Notepad, target/clipboard/focus | Actual desktop GUI automation PASS |
| Settings sidebar, labels, help, locale, minimum size | Native WPF/UIA/keyboard automation PASS; synthetic account responses |
| Automatic lookup while typing, opt-out/cancellation | Physical editor/Notepad + real Core PASS; dictionary fixtures synthetic |
| Sharing queue, auth/idempotency/retry/revoke | Local contract/transport fixtures PASS; real receiver NOT RUN |
| OCR / TTS / STT | PASS 3 adapter checks and 102 speech checks; audible quality, real mic/snipping remain human NOT RUN |
| Windows Hello PIN/biometry | USER-DEFERRED |
| Real MSIX install/update/rollback/data/uninstall | NOT RUN; explicit temporary-trust approval pending |
| G05 native write/read/restore | NOT RUN, production toggle read-only |
| Narrator / high contrast / human keyboard review | NOT RUN; UIA tests are not human signoff |
| Physical mixed DPI / multiple monitors | NOT RUN, actual 96 DPI / one monitor |
| Production signing / HTTPS App Installer/public beta | NOT DONE, no approved identity/access |
| macOS runtime / public DMG/Sparkle | NOT RUN; owning sources remain unchanged |

## Next action

Follow [FinalAcceptance.md](../FinalAcceptance.md) for the remaining operator and
human checks. Backend receiver stays with the macOS agent, using the same-repo
slice13 prompt. Only reviewed deployed backend evidence and a real test-account
roundtrip can enable A07. Obtain approved test trust/environment before installed
update tests; provide stable trusted production signing before public beta. Users
can use the current Windows editor/settings and local learning in the meantime;
there is no hidden upload, new backend contract or released automatic-update claim.
