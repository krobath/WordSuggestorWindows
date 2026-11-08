# WSA-RT-016 / WSA-UX-012 / WSA-TS-004 / WSA-DX-004

Date: 2026-11-08. Delivery: implemented Windows beta slice, independently runnable
payload and signing-ready MSIX. **Not a signed or fully qualified public RC.**
No signing identity or release feed is available. No trust store was modified.

## Result and architectural decision

The baseline Windows repo was clean at `4d67106`. Its WPF .NET 9 app built and
launched, and had substantial working Core, speech, OCR, Office selection-import,
settings and insights code. A new solution would discard that work without solving
focus/target trust. Controlled WPF refactoring was selected. See `../ReleaseAudit.md`
for concrete findings, migration tradeoffs and conflicting dated references.

The three boundaries are Core/SQLite language data, WPF orchestration, and isolated
Windows adapters. Ranking and normalization remain in Core. Existing resource/icon,
toolbar ordering, editor actions, settings schema/data location, TTS/STT/OCR,
UIA/Word COM selection import and historical sources are preserved.

Refactored: separate activating editor, non-activating toolbar/overlay, caret and
selection transaction, shared mouse/keyboard commit, bounded versioned Core stdin
transport, pack validation, persistent Core user words, diagnostics and build flow.
Replaced: unverified foreground SendInput commits with snapshot-bound Notepad
native-control commits; passive keystroke/full-selection capture with qualified
snapshot observation. Removed C# suffix-based linguistic guesses; analysis controls
are explicitly disabled with preferences retained until Core analysis is exposed.
Word/Outlook/browser commits are gated, with a documented adapter migration path.

## Reproduction and observed results

Commands below run from the Windows repo. First restore needs network access;
suggestions remain offline. No production credentials are needed.

| Command / check | Observed result |
|---|---|
| `powershell -ExecutionPolicy Bypass -File scripts\build_app.ps1` | PASS, zero warnings/errors on app build |
| `powershell -ExecutionPolicy Bypass -File scripts\test_core_cli.ps1` | PASS, six ordered DA batch/JSON term/kind/score parity fixtures |
| `powershell -ExecutionPolicy Bypass -File scripts\test_app.ps1` | PASS, 54 checks: real Core/DA, metadata/schema/columns, user words/restart, timeout/cancel/reap, adversarial processes, stale/duplicate commits, privacy, settings/future formats, UTF-16 selection and placement geometry |
| `powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1` | PASS, 23 checks on development app; extended 25-check suite also passes on the final standalone payload through test_package |
| `powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1` | PASS, three adapter/contract checks: actual Windows OCR on generated PNG, actual OneCore Helle playback process, production STT encoder/receiver UTF-8/tab fixture |
| Clean optimized Core build using existing bootstrap with fresh scratch | PASS, 115.20 seconds; local SDK VFS overlay fixes mixed-SDK imports without modifying protected SDK files |
| `powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1` | PASS complete default flow: optimized Core build (143.10 seconds), self-contained x64 publish and Windows SDK MakeAppx validation |
| `scripts\test_package.ps1 -AppDirectory <artifact-app-directory>` | PASS, 25 desktop checks on self-contained payload copied outside source with SDK paths removed; includes actual native Open/Save Unicode round-trips |
| `scripts\test_clean_checkout.ps1` | PASS at Windows `54acfb2` / locked Core `5d8d7a0`: fresh app build 0 warnings/errors, SQLite compiled from downloaded checksum-pinned source, fresh Core build, six parity fixtures and 54 app checks |

Swift's existing `executeTask` Sendable warning, missing sqlite pkg-config and
symlink warnings are distinct from app build warnings. The initial Core smoke
incorrectly accepted a failed Swift build/stale binary; native exit checks now
prevent that. A first clean release build exposed SDK module-map incompatibility;
the corrected local overlay was then verified from a fresh scratch directory.
GUI test development also exposed real caret initialization/selection feedback
defects, which were fixed and regression-tested. Shell-dialog UIA SetValue proved
insufficient; file-dialog smoke now uses real keyboard entry into unique test paths.
The first committed-export attempt additionally caught missing pinned SDK headers
when selecting Visual Studio Build Tools. `54acfb2` moves SDK paths before SQLite
compilation; a new committed export then passed without source build caches.

Current artifact (ignored, not committed):

```text
.artifacts/release/0.2.0.0-26a1bba905f74fae9adf437b3f15262d/WordSuggestor.Beta.0.2.0.0.unsigned.msix
SHA256 2E243FD78A4B734F5F00AE1106B9E18F3CBC241EE3AE42FC3575BFA16729C1F8
```

The payload includes .NET 10, Swift runtime/resources, optimized Core and real DA
data. `build-info.json` records Core/pack hashes. This artifact has no trusted
signature, installation proof or update feed. Input revisions/hashes and installed
toolchain prerequisites are in `dependencies.lock.json` and `global.json`.

## Desktop/manual qualification matrix

These are automated observations on the actual Windows desktop, not human sign-off.
Notepad tested: 11.2607.14.0; one physical monitor at 96 DPI.

| Area | Evidence | Still required |
|---|---|---|
| Startup/focus/editor | Toolbar-only startup, toolbar preserves Notepad foreground, normal separate editor and close/reopen/single-instance behavior | Human visual and long editing session |
| Suggestions/commit | Real DA/Core; internal and Notepad mouse/Ctrl+1, caret/prefix, CRLF/æøå/emoji, marked-range replacement, stale target rejection; clipboard sequence unchanged | Other Notepad versions, paging/undo soak and external Unicode range variants |
| Files | Native Save exact text and Open Unicode round-trips PASS on isolated payload | Cancel, overwrite, quit guards and permission errors manually |
| Accessibility | UIA action names/editor TextPattern | Human keyboard-only and Narrator/NVDA/high-contrast walkthrough |
| Display placement | Negative-origin/clamping math | Physical multi-monitor 125/150/200% mixed DPI |
| OCR/speech | Actual OCR fixture and playback process; STT wire regression fixed | Human hearing, highlight timing, microphone/DA recognizer, interactive snipping/cancel/clipboard concurrency |
| Office/web | UIA/Word COM selection import code retained | Word/Outlook/Edge/Chrome/Docs commits unsupported until isolated adapter qualification |
| Data/offline | Restart/future-schema tests; no backend in suggestion path | Signed upgrade/uninstall retention |
| Distribution | MakeAppx validation and final isolated payload test | Trusted install/update/uninstall/tamper/interruption/rollback qualification |

## Remaining work and external actions

1. Obtain a trusted package-signing identity and controlled HTTPS beta hosting. A
   signing service is optional; the certificate identifies the publisher and
   authenticates unchanged packages. Never send private keys/passwords in chat.
2. Use `../Release.md` to sign, verify trust and generate the feed. Test signed N →
   N+1 → last-good payload republished as N+2 on a disposable VM, preserving data;
   verify tampered packages, offline/interrupted update and uninstall behavior.
3. Complete the human accessibility/audio/file-dialog and physical monitor matrix
   in `../ManualSmoke.md`. This environment did not supply those hardware/manual checks.
4. Qualify Word first, then Outlook, browser inputs and Google Docs adapters;
   retain fail-closed commits. Restore coloring/corrections from Core analysis.
5. Obtain a metadata-complete DA pack and actual EN/DE packs. The supplied DA file
   is legacy schema 0; current Core builder emits 14 while older contract prose
   says 9. Reviewed schema/column compatibility is explicit; no pack is rewritten.
   Run identical revision/hash fixtures on a macOS machine before Mac parity sign-off.
6. Account/backend sync, secure token storage (when needed), diagnostic rotation,
   domain lists and newer macOS learning features remain future work.

## Source review and commits

Core commit: `5d8d7a0` — `WSA-RT-016_windows_beta_hardening: add versioned stdin bridge and persistent Core user words`.
Windows integration: `b1e0c3d` — `WSA-RT-016_windows_beta_hardening: deliver native editor, guarded commits and MSIX beta flow`.
Clean-build correction: `54acfb2` — `WSA-DX-004_windows_clean_build: set pinned SDK paths before compiling SQLite`.
Docs handoff: `9091c99` — `WSA-RT-016_windows_beta_handoff: record implementation evidence and release gates`.
Generated packages, dependency caches, credentials and machine-specific output
paths are excluded. Existing user data and reference repositories are preserved.

## Final qualification results

Clean committed-export PASS: `.artifacts/clean-checkout/d54c503a48d84240b04943fdeb1b1a3d`.
Both exported repos contain committed source only, no copied bin/obj/.build or
SQLite artifacts. Existing installed toolchains and the NuGet machine cache are
prerequisites; this is a clean-source build, not a fresh Windows VM qualification.
Core CLI built in 38.47 seconds; six real Danish parity fixtures and 54 app checks
passed. Full Core engine unit suites and macOS-machine parity were not run.

Final isolated payload PASS: 25 real desktop checks on the exact artifact above,
including Unicode Open/Save, copied outside the workspace with SDK paths removed.
Core SHA-256: `F1C61DAADC5017C38A6BBD7C1D69B0F4706BFFAA114C14BB6BA9855E779F5EA3`.
DA SHA-256: `6970C398077C235355F513D9491E4193534FE88B751AEEA07EDDEF9ABC3FB82F`.
MSIX length: 125,656,140 bytes. MakeAppx reports `Package creation succeeded`.
The same test was also run against the preceding staging payload; both passed.

Final diff review found no newly committed binary artifacts, credential patterns,
private keys or user-specific absolute paths; whitespace check passed. Synthetic
GUI document canaries were absent from the final run's diagnostics. This is scoped
evidence, not a claim that all historical logs or user-supplied files are sanitized.
No unrelated source changes were discarded; macOS and Tools worktrees remain clean.

## Changed files

The inventory below records the implementation and documentation files, excluding
ignored build/test/package output.

Other repositories:

- `WordSuggestorCore/Tools/SuggestCLI/main.swift`
- `WordSuggestorCore/Tools/SuggestCLI/StdioProtocol.swift`
- `WordSuggestorDocs/90_agent_ops/handoffs/windows_track.md`

Windows repository:

- `.gitattributes`
- `.gitignore`
- `dependencies.lock.json`
- `docs/Architecture.md`
- `docs/BridgeContract.md`
- `docs/changes/2026-11-08_wsa_rt_016_windows_beta_hardening.md`
- `docs/ManualSmoke.md`
- `docs/ParityMatrix.md`
- `docs/Plan.md`
- `docs/Release.md`
- `docs/ReleaseAudit.md`
- `docs/SelectionImportCompatibilityMatrix.md`
- `docs/UiParityPlan.md`
- `global.json`
- `packaging/AppxManifest.xml`
- `README.md`
- `scripts/bootstrap_core_cli.ps1`
- `scripts/build_app.ps1`
- `scripts/check_update.ps1`
- `scripts/dotnet_env.ps1`
- `scripts/package_app.ps1`
- `scripts/run_app.ps1`
- `scripts/swift_sdk_overlay.ps1`
- `scripts/test_app.ps1`
- `scripts/test_clean_checkout.ps1`
- `scripts/test_core_cli.ps1`
- `scripts/test_gui.ps1`
- `scripts/test_media.ps1`
- `scripts/test_package.ps1`
- `src/WordSuggestorWindows.App/app.manifest`
- `src/WordSuggestorWindows.App/App.xaml`
- `src/WordSuggestorWindows.App/App.xaml.cs`
- `src/WordSuggestorWindows.App/AssemblyInfo.cs`
- `src/WordSuggestorWindows.App/InternalEditorWindow.xaml`
- `src/WordSuggestorWindows.App/InternalEditorWindow.xaml.cs`
- `src/WordSuggestorWindows.App/MainWindow.xaml`
- `src/WordSuggestorWindows.App/MainWindow.xaml.cs`
- `src/WordSuggestorWindows.App/Models/AppSettingsSnapshot.cs`
- `src/WordSuggestorWindows.App/Models/SelectionImportDiagnostic.cs`
- `src/WordSuggestorWindows.App/Services/EditorCommit.cs`
- `src/WordSuggestorWindows.App/Services/ISuggestionProvider.cs`
- `src/WordSuggestorWindows.App/Services/LanguagePackValidator.cs`
- `src/WordSuggestorWindows.App/Services/NonActivatingWindow.cs`
- `src/WordSuggestorWindows.App/Services/ScreenPlacement.cs`
- `src/WordSuggestorWindows.App/Services/SingleInstance.cs`
- `src/WordSuggestorWindows.App/Services/WindowsAppSettingsService.cs`
- `src/WordSuggestorWindows.App/Services/WindowsDataPaths.cs`
- `src/WordSuggestorWindows.App/Services/WindowsErrorInsightsStore.cs`
- `src/WordSuggestorWindows.App/Services/WindowsExternalSuggestionCommitService.cs`
- `src/WordSuggestorWindows.App/Services/WindowsGlobalSuggestionCaptureService.cs`
- `src/WordSuggestorWindows.App/Services/WindowsNativeTextAdapter.cs`
- `src/WordSuggestorWindows.App/Services/WindowsOcrCallbackBridge.cs`
- `src/WordSuggestorWindows.App/Services/WindowsOcrService.cs`
- `src/WordSuggestorWindows.App/Services/WindowsSelectionImportService.cs`
- `src/WordSuggestorWindows.App/Services/WindowsSpeechToTextService.cs`
- `src/WordSuggestorWindows.App/Services/WindowsTextToSpeechService.cs`
- `src/WordSuggestorWindows.App/Services/WordSuggestorCoreCliSuggestionProvider.cs`
- `src/WordSuggestorWindows.App/SettingsWindow.xaml`
- `src/WordSuggestorWindows.App/SharedStyles.xaml`
- `src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml`
- `src/WordSuggestorWindows.App/SuggestionOverlayWindow.xaml.cs`
- `src/WordSuggestorWindows.App/UserWordsWindow.cs`
- `src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs`
- `src/WordSuggestorWindows.App/WordSuggestorWindows.App.csproj`
- `tests/WordSuggestorWindows.Smoke/GuiSmoke.cs`
- `tests/WordSuggestorWindows.Smoke/MediaSmoke.cs`
- `tests/WordSuggestorWindows.Smoke/PackFixture.cs`
- `tests/WordSuggestorWindows.Smoke/Program.cs`
- `tests/WordSuggestorWindows.Smoke/WordSuggestorWindows.Smoke.csproj`
- `WordSuggestorWindows.sln`
