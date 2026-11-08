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
| `powershell -ExecutionPolicy Bypass -File scripts\test_gui.ps1` | PASS, 23 real desktop checks before extending native Open coverage; final extended result below |
| `powershell -ExecutionPolicy Bypass -File scripts\test_media.ps1` | PASS, three adapter/contract checks: actual Windows OCR on generated PNG, actual OneCore Helle playback process, production STT encoder/receiver UTF-8/tab fixture |
| Clean optimized Core build using existing bootstrap with fresh scratch | PASS, 115.20 seconds; local SDK VFS overlay fixes mixed-SDK imports without modifying protected SDK files |
| `powershell -ExecutionPolicy Bypass -File scripts\package_app.ps1 -SkipCoreBuild -CoreCliPath <verified-release-cli>` | PASS, self-contained x64 publish and Windows SDK MakeAppx manifest/package validation |
| `scripts\test_package.ps1 -AppDirectory <artifact-app-directory>` | PASS, 25 desktop checks on self-contained payload copied outside source with SDK paths removed; includes actual native Open/Save Unicode round-trips |
| `scripts\test_clean_checkout.ps1` | Final committed-export result below |

Swift's existing `executeTask` Sendable warning, missing sqlite pkg-config and
symlink warnings are distinct from app build warnings. The initial Core smoke
incorrectly accepted a failed Swift build/stale binary; native exit checks now
prevent that. A first clean release build exposed SDK module-map incompatibility;
the corrected local overlay was then verified from a fresh scratch directory.
GUI test development also exposed real caret initialization/selection feedback
defects, which were fixed and regression-tested. Shell-dialog UIA SetValue proved
insufficient; file-dialog smoke now uses real keyboard entry into unique test paths.

Current artifact (ignored, not committed):

```text
.artifacts/release/0.2.0.0-27dfad98074b468aad0a59d8a51132f8/WordSuggestor.Beta.0.2.0.0.unsigned.msix
SHA256 E98370656C47BB4626BB9F5F4BC419E697FBDC6E622272DB296B5C59728BCE1D
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
Windows integration and final evidence commits are recorded after qualification.
Generated packages, dependency caches, credentials and machine-specific output
paths are excluded. Existing user data and reference repositories are preserved.

## Final qualification results

Updated after isolated payload and committed-export execution.

## Changed files

The inventory below records the implementation and documentation files, excluding
ignored build/test/package output.

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
