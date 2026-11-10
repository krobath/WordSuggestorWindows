# WSA-UX-029 slice 3 — Core Settings capabilities and adapter

Date: 2026-11-10. Windows x64 `10.0.26300.0`, ordinary user, one 96-DPI monitor.
.NET 10.0.403, Swift 6.3, pinned compiler SDK 10.0.22621.0. Windows base `8af50e6`;
Core `c87f68aa2c145220817c0264279854d5b9b32078`; canonical Mac reference `ba18b45`.
DA SHA-256 `6970C398077C235355F513D9491E4193534FE88B751AEEA07EDDEF9ABC3FB82F`.

## Delivered boundary

Retain WPF and the existing process bridge. Add v1 capability discovery and typed
v2 queries for real Core source preferences, explicit domain catalogs, morphological
readings/families and Danish diagnostics. Existing request shapes/default results
remain supported. Core does linguistic work; C# validates transport/scope/payloads.
No settings schema, profile/data location, consent, backend or release change.
No new enabled Settings controls: slices 5–6 still own visible feature effects.
Next slice is 4 (speech rate, strategy and highlights).

Learning requirement is resolved explicitly: selectable readings expose stable
opaque IDs, but neither choice persistence nor adaptive learning is implemented
by the bridge. It cannot attest to saved-choice/sharing parity. The DA fixture
lacks term_variants/ngram/phonetic evidence; optional-table tests use a synthetic
SQLite fixture, not a release pack. The ai_ml_llm seed is explicit qualification
input, not bundled/production-promoted. See [contract](../BridgeContract.md).

## Reproduction and qualification scope

From this repo, with sibling Core at the pinned revision and installed toolchains:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1
powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1 -SettingsContract
powershell -ExecutionPolicy Bypass -File scripts\test_settings_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File scripts\test_settings_qualification.ps1
```

Python 3.10+ is used only by the portable test. `-Mode Core` runs just the new
Windows gate, `-Mode State`/`Ui` retain previous gates. If an app locks build output,
build in a separate checkout/export; do not terminate the user's session. For an
independent .NET artifacts build, the Settings gate supports `-RunnerPath`.
Desktop tests require an unlocked session, move mouse/keyboard with synthetic
data, and use isolated profiles; do not interact during execution. SDK/atomic-file
and desktop tool access is beyond the tool sandbox, still at ordinary user level.
No production account, signing certificate, trust installation or network feed
is required for this feature slice.

## Results

Windows and the locked Core revision were exported with `git archive` into empty
sibling directories. No working-tree sources, generated binaries or build caches
were copied. Installed toolchains were prerequisites; the logs and existence of
the built app/CLI were checked inside that export. Existing scripts above ran
against the exported source, including fresh SQLite/Core bootstrap.

| Gate | Concrete result |
|---|---|
| Clean Windows build | PASS, zero warnings and zero errors |
| Fresh Core CLI | PASS, built in the export from the pinned source |
| Existing DA fixtures | PASS, six real batch/v1 ordered-result fixtures |
| Portable Settings contract | PASS, 169 checks: source switches, default v1/v2 parity, explicit seed domain, readings/families, both comma styles/modes, original UTF-16, bounds and no input echo |
| Windows Settings adapter | PASS, 58 checks: actual Core, old-protocol rejection, precise scope, malformed/wrong identity/payload/ranges, private stderr, exit, timeout/cancel and child reaping |
| Standard app | PASS, 67 checks from the committed-source scope |
| Settings state / desktop | PASS, 63 / 58 checks with synthetic profiles and actual interactive WPF windows |
| App/editor/Notepad GUI | PASS, 30 checks, one monitor at 96 DPI; keyboard/mouse acceptance, prefix/caret/focus and static/caret-permission regression |
| Media adapters | PASS, three contract checks; audible/microphone/snipping behavior remains manual |
| Qualification safety | PASS, 35 contract/mock checks; not proof of positive Hello/OS access |

Qualified candidate tree: `4c1c61f562584720de2bb61e8bb4d04460d937fb`. Final
documentation follows qualification; implementation/test/script subtree hashes
are checked unchanged before commit:

- `src`: `88f3c900867ac75ecb755951922db1dded9b952e`
- `tests`: `6ad487662f807b6adacdd9c75cde729cf4aceb2f`
- `scripts`: `333652c60306dbde0c221055392e50b4f542a56c`

The first export stopped at a generated SQLite fixture exceeding Windows SQLite
path limits. Core's runner now uses a securely allocated, cleaned temporary root;
its report remains in the requested artifact directory. The final fresh export
above passed unchanged expectations. No failed run is counted as a PASS.

For clean-source reproduction, export the finished Windows commit and locked
Core commit into empty sibling directories, keep installed toolchains on PATH,
and run the exported scripts using absolute script paths. For the first Core
gate do not use SkipBootstrap. Confirm output paths are in the export. Earlier
uncommitted editor/release files were deliberately excluded; their larger local
test counts are not used as evidence for this commit.

The complete Core `swift test` was also attempted with the same SDK/SQLite setup.
Compilation fails in unchanged files including
`Tests/WordSuggestorCoreTests/ContextualDanishBaselineRuntimeTests.swift` and
`Tools/ClosedClassResidualFamilyValidation/main.swift` because
`CFAbsoluteTimeGetCurrent` is unavailable in scope on Windows. The full library
suite did not execute and is **not PASS**. Its portability repair is separate;
targeted CLI/adapter gates are qualified independently. Known pkg-config/debug
symlink warnings do not prevent the CLI product from building.

## Manual smoke matrix and remaining gates

| Area | Slice evidence | Remaining manual/platform work |
|---|---|---|
| Source preferences | Actual Core switches exercised on optional-table fixture; real DA default result parity | Metadata-qualified DA/EN/DE packs, later controls with visible runtime effects |
| Domain lists | Real tracked seed catalog/provider and separate insert form | Production domain-pack approval/installation; shortcut lanes in slice 5 |
| Word information | Real Core analysis and stable reading IDs; unavailable family warning; synthetic family fixture | Visible word-info/interpretation UI and saved-choice consent/storage in slices 5–6 |
| Analysis | Real original UTF-16 diagnostic/edit offsets including emoji/decomposed characters, both comma modes/styles | Live markers/correction groups/selection UI in slice 6; no automatic diagnostic commits claimed |
| Scope/privacy/process | Exact response identity, stale-scope predicate, malformed/missing payload, child stderr/exit/timeout/cancel/reap | Future UI must check current scope again on dispatcher; no new live analysis/tab flow claimed |
| Existing app/Settings/Notepad | Regression gates below | Human visual/keyboard/Narrator/high contrast; mixed DPI/multiple monitors |
| Cross-platform | Portable fixtures executed on Windows | Mac owner runs same test at same Core/pack; Windows pass does not establish Mac parity execution |
| Speech/Hello/update/release | Existing unavailable/status behavior retained | Audible TTS, provisioned Hello, OS-toggle roundtrip, signed install and N→N+1 remain open |

No complete human, screen-reader, physical DPI, macOS or signed-update session
is claimed. No public release/upload is performed. User data and settings remain
under their existing authorities; ordinary app v1 suggestion behavior is retained.

## Recovery and next work

Old CLIs negotiate v1-only; new calls reject unsupported features instead of
ignoring options. Restore a known-good Core executable/pinned source if its process
is unavailable; do not substitute linguistic results in C#. The normal data backup
and schema-2 recovery procedure from slice 2 is unchanged. No new on-disk format
needs rollback. Release rollback remains known-good code with a higher MSIX number.
Proceed with slice 4; keep source/domain/analysis UI activation for slices 5–6.
