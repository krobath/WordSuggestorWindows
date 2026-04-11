# WordSuggestorWindows Plan

Last updated: `2026-04-10`
Owner: `Windows track`
Status legend: `Done`, `In progress`, `Planned`, `Blocked`

## Naming rule

Canonical sprint IDs follow:

- `<AREA>-<TRACK>-<NNN>_<short-slug>`

For this repository, the expected default area is:

- `WSA` for app/runtime/UI work

Typical track codes for the Windows port:

- `DX` - repo/process/baseline
- `UX` - Windows UI parity and visual shell work
- `RT` - runtime/input/caret/panel/commit integration
- `TS` - smoke tests and validation

## Active plan

### WSA-DX-001_windows-repo-baseline
Status: `Done` (`2026-04-09`)

Scope:

- Create the Windows delivery repository baseline.
- Establish architecture, parity, and documentation conventions.
- Define the first safe implementation order.

Deliverables:

- `README.md`
- `docs/Architecture.md`
- `docs/ParityMatrix.md`
- `docs/changes/2026-04-09_wsa_dx_001_windows_repo_baseline.md`

Exit criteria:

- Windows repo has a canonical plan and architecture baseline.
- Porting work is staged by risk instead of attempting a direct AppKit-to-Windows rewrite.

### WSA-RT-001_windows_core_bridge_and_internal_editor
Status: `Done` (`2026-04-09`)

Scope:

- Establish the first Windows app shell.
- Prove end-to-end local suggestions in an internal editor.
- Bridge Windows app code to `WordSuggestorCore`.

Implemented so far:

- WPF solution scaffolded in `src/WordSuggestorWindows.App`.
- First internal editor window implemented with local suggestion list.
- Suggestion acceptance into the internal editor implemented.
- `ISuggestionProvider` abstraction added so UI code is isolated from bridge details.
- First `WordSuggestorCore` CLI bridge implemented using:
  - prebuilt CLI if available,
  - `swift run` fallback otherwise.
- Reproducible local helper scripts added:
  - `scripts/build_app.ps1`
  - `scripts/test_core_cli.ps1`
- Local Windows validation established:
  - app build passes.
  - core CLI smoke now passes after local bootstrap follow-up in `WSA-DX-002`.

Target outcome:

- User can type in the Windows app and receive local WordSuggestor suggestions without cross-app integration.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`

Known note:

- The editor path is now backed by the dedicated overlay panel baseline from `WSA-RT-002`, but attributed-text styling and right-click correction parity remain follow-up work.

### WSA-DX-002_windows_core_cli_bootstrap
Status: `Done` (`2026-04-09`)

Scope:

- Make local Windows `WordSuggestorCore` CLI execution reproducible in this workspace.
- Mirror the relevant Windows portability bootstrap steps from CI.
- Unblock the Windows app's local engine bridge.

Implemented:

- Added `scripts/bootstrap_core_cli.ps1`.
- Added local SQLite dev artifact provisioning under `WordSuggestorWindows/.artifacts/sqlite-dev`.
- Added Windows SDK path normalization to pin the Swift build to `10.0.22621.0`.
- Updated `scripts/test_core_cli.ps1` to bootstrap and then invoke the built CLI executable directly.
- Verified local CLI output for Danish prefix input (`skri`).

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`

Known note:

- Modulemap bootstrap into `Program Files (x86)` is not writable in this user context, but the local CLI path still succeeds with the current script and pinned env normalization.

### WSA-UX-001_windows_toolbar_shell_parity
Status: `Done` (`2026-04-09`)

Scope:

- Replace the temporary normal window with a floating top toolbar shell.
- Preserve the macOS toolbar control ordering while using Windows-native look and feel.
- Reuse the shared WordSuggestor app icon for the Windows shell.

Target outcome:

- Windows launches into a compact floating toolbar that is recognizably the same product as macOS.
- The toolbar expands downward into the internal editor from the right-side chevron.

Implemented:

- Replaced the initial standard WPF document window with a borderless floating toolbar shell.
- Added the shared app icon from the macOS asset catalog to the Windows app project and shell UI.
- Preserved the toolbar control ordering from the macOS product model with Windows-friendly chrome.
- Added expand/collapse behavior that keeps the toolbar pinned at the top while the editor opens downward.
- Moved the existing internal editor baseline into the expanded shell instead of a separate generic screen.
- Added early `Ctrl+1` through `Ctrl+0` in-editor suggestion picking support ahead of the overlay sprint.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (updated shell launched; process remained responsive)

Known note:

- The temporary in-app suggestion preview strip used in this sprint has since been replaced by the dedicated floating overlay delivered in `WSA-RT-002`.
- Editor analyzer rows and legend are currently structural parity placeholders and not yet full attributed-text parity.

### WSA-RT-002_windows_overlay_panel_and_commit_path
Status: `Done` (`2026-04-09`)

Scope:

- Implement the separate Windows suggestion overlay panel.
- Support static placement and follow-caret placement.
- Add `Ctrl+1` through `Ctrl+0` candidate shortcuts and page navigation.
- Fall back to static placement whenever follow-caret is not reliable.

Target outcome:

- The suggestion overlay behaves like the macOS panel in the internal editor first.

Implemented:

- Added a separate borderless `SuggestionOverlayWindow` instead of rendering suggestions inside the editor shell.
- Added pagination semantics that preserve 10 visible suggestions per page and up to 4 pages by requesting up to 40 candidates from the CLI provider.
- Added `Ctrl+1` through `Ctrl+0` candidate picking against the current overlay page.
- Added `Ctrl+Left` and `Ctrl+Right` page navigation inside the editor.
- Added placement mode switching between static and follow-caret from the overlay header.
- Added automatic fallback to static anchor placement when the editor caret cannot be resolved reliably.
- Removed the temporary in-shell suggestion preview strip from the expanded editor surface.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (overlay-enabled app launched; process remained responsive)

Known note:

- Follow-caret is currently implemented for the internal Windows editor surface only.
- External app overlay anchoring and commit paths remain part of `WSA-RT-003`.
- The current overlay matches product semantics first; richer visual styling parity still depends on later UI refinement.

### WSA-UX-003_windows_overlay_density_refinement
Status: `Done` (`2026-04-10`)

Scope:

- Increase the usable vertical density of the floating suggestion overlay.
- Make the first overlay page show all 10 suggestions without immediate scrolling in the normal editor scenario.
- Bring row proportions closer to the macOS suggestion panel.

Implemented:

- Increased the overlay window height to better match the amount of content shown in the macOS panel.
- Reduced row padding, margins, and font sizes in the suggestion list while keeping the two-line information layout.
- Reduced header and footer chrome density so more vertical space is available to the suggestions themselves.
- Kept page controls and placement controls intact while making them visually lighter.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (refined overlay app launched; process remained responsive)

Known note:

- This sprint improves the density and fit of the internal-editor overlay only.
- Visual styling and per-row metadata parity can still be refined further during later UX passes.

### WSA-UX-004_windows_overlay_row_height_refinement
Status: `Done` (`2026-04-10`)

Scope:

- Reduce the per-row height in the floating suggestion overlay further.
- Ensure the first page can show 10 visible candidates without requiring scroll in the default smoke scenario.
- Preserve readability and the existing paging/placement controls.

Implemented:

- Reduced overlay row padding, row margins, and row typography further.
- Reduced header and footer chrome slightly again so the list gets more usable vertical space.
- Compressed the footer help into a single line to free height for suggestion rows.
- Reduced shortcut and score text footprint to keep each candidate row tighter.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (refined overlay app launched; process remained responsive)

Known note:

- This sprint only tightens overlay row presentation; it does not change pagination logic or external-app behavior.

### WSA-RT-005_windows_overlay_static_drag_and_rich_rows
Status: `Done` (`2026-04-10`)

Scope:

- Make static placement mean true manual placement chosen by the user.
- Enrich each overlay row with macOS-style metadata and per-row actions.
- Preserve Windows-native presentation while matching the macOS suggestion semantics more closely.

Implemented:

- Extended the shared `SuggestCLI` JSON contract so Windows receives `type`, `pos`, and `gram` alongside `term`, `score`, and `kind`.
- Added a richer Windows suggestion presentation layer that maps match kinds to Danish labels, explanatory text, and row tint colors.
- Rebuilt the overlay row template so each candidate now shows:
  - term
  - inline match label in parentheses
  - metadata line with ordklasse and grammar tag when available
  - speaker button
  - info button
- Added distinct background tinting for ordinary, phonetic, misspelling, and synonym suggestions.
- Changed static placement behavior so the user can drag the overlay header while static mode is active, and the overlay stays at that absolute screen position until moved again.
- Kept follow-caret as the preferred mode and preserved static fallback when caret anchoring is unavailable.
- Added a Windows-native speech path for the row speaker button using a PowerShell/SAPI bridge.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (app launched; process remained responsive)

Known note:

- Manual static placement currently persists for the running session; persistence across app restarts is not implemented yet.
- The info button currently shows runtime metadata available from the Windows bridge, not the fuller future word-insight surface from the editor correction popover.

### WSA-RT-006_windows_overlay_crash_and_danish_utf8
Status: `Done` (`2026-04-11`)

Scope:

- Fix the overlay crash that happened as soon as suggestion rows were rendered after short input.
- Make Danish letters part of the bridge regression path.
- Clarify the SQLite/pkg-config warning seen during bootstrap.

Implemented:

- Fixed the WPF `Run.Text` bindings in the overlay row template by making them explicit one-way bindings.
- Updated the CLI bridge to write request files as UTF-8 and read CLI stdout/stderr as UTF-8.
- Restored Danish UI labels in the overlay footer, TTS tooltip, and TTS status message.
- Updated `scripts/test_core_cli.ps1` to smoke-test `skri`, `læ`, `ø`, `å`, `smør`, and `blå`.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Direct `WordSuggestorSuggestCLI` smoke with `læ`, `ø`, `å`, `smør`, and `blå` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap -SampleText "Jeg prøver at læ"` -> `PASS` (app launched; process remained responsive)
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- Application event log check after launch showed no new `WordSuggestorWindows.App` crash event.

Known note:

- `warning: couldn't find pc file for sqlite3` is a build/pkg-config discovery warning, not evidence that the Danish SQLite pack failed to load. The successful CLI output and `Pack opened ... da_lexicon.sqlite` log confirm the pack was loaded.

### WSA-UX-005_windows_editor_ui_cleanup_and_shortcut_flow
Status: `Done` (`2026-04-11`)

Scope:

- Remove duplicated editor information boxes that should be represented by button state instead.
- Restore visible Danish UI labels for æ/ø/å-sensitive strings touched by the Windows overlay/editor work.
- Make suggestion acceptance and page navigation match the intended keyboard flow.

Implemented:

- Removed the informational card above the internal editor text field that duplicated analyzer/toggle state.
- Removed the informational card directly below the editor text field that duplicated suggestion overlay placement/page/provider state.
- Removed the lower analyzer panel cards for overlay placement and engine bridge state.
- Kept analyzer and placement state visible through toggle-button visual state instead; the overlay placement buttons now render as checked toggle buttons.
- Updated suggestion acceptance so a trailing space is inserted after the accepted word unless the following text already starts with whitespace.
- Added window-level and overlay-level keyboard handling for `Ctrl+Left` and `Ctrl+Right` so paging still works when focus is not exactly inside the editor textbox.
- Restored visible Danish UI labels in the command row, toolbar tooltips, and overlay footer.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap -SampleText "Jeg prøver at læ"` -> `PASS` (app launched; process remained responsive)
- Recent Application event log check after launch showed no new `WordSuggestorWindows.App` crash event.

Known note:

- The next pass can focus on visual parity against the macOS screenshot now that the input/shortcut regressions are addressed.

### WSA-UX-006_windows_rich_editor_surface_and_coloring_baseline
Status: `Done` (`2026-04-11`)

Scope:

- Make the internal editor input field consume the maximum stable space available inside the expanded window while preserving the status bar and text analysis legend below it.
- Ensure the editor wraps horizontally and scrolls vertically.
- Add the first visible Windows-side word coloring baseline inside the editor.

Implemented:

- Replaced the plain WPF `TextBox` editor with a `RichTextBox` so individual words can receive independent styling.
- Kept the editor in the `*` row between the command row and the status/analyzer panels so it fills the available expanded-window height.
- Configured the editor with vertical scrolling and disabled horizontal scrolling so text wraps to the available width.
- Added manual synchronization between the rich text document, `MainWindowViewModel.EditorText`, and caret index so the existing suggestion overlay and accept flow still work.
- Added a first Windows-side POS-style token classifier that colors editor words using the same category colors as the analyzer legend.
- Preserved the analyzer color toggle so the rich text coloring can be turned on and off from the command row.
- Updated caret anchoring to use the `RichTextBox` caret rectangle instead of the former `TextBox.GetRectFromCharacterIndex` path.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap -SampleText "Jeg skriver den blå bog og læser hurtigt"` -> `PASS` (app launched; process remained responsive)
- Recent Application event log check after launch showed no new `WordSuggestorWindows.App` crash event.

Known note:

- The new editor coloring is a Windows-side baseline classifier intended to restore visible color behavior. It is not yet full macOS `TextAnalyzer` parity or lexicon-backed analysis.
- Multi-paragraph caret indexing may need a dedicated follow-up if users rely heavily on long multi-paragraph internal-editor documents before the full analyzer port lands.

### WSA-UX-002_windows_internal_editor_surface_parity
Status: `Done` (`2026-04-10`)

Scope:

- Rebuild the internal editor surface so it matches the macOS structure.
- Add command row, status bar, and text-analyzer legend/panel layout.
- Preserve the macOS word-class coloring and underline semantics with Windows-native rendering.

Target outcome:

- The internal editor no longer behaves like a generic textbox screen.
- The Windows editor reflects the same information architecture as the macOS editor screenshots.

Implemented:

- Reworked the expanded editor shell into a clearer three-part structure:
  - command row
  - editor surface with contextual header/status strip
  - metric row plus analyzer legend section
- Replaced the old plain command strip with a Windows-native command/toggle row for:
  - cut/copy/paste
  - analyzer coloring
  - semantic diagnostics
  - punctuation diagnostics
  - suggestion refresh
- Added status metric cards for characters, words, spelling, and grammar/punctuation counts.
- Added a dedicated analyzer legend section driven by reusable view-model data instead of static duplicated XAML blocks.
- Added editor-surface summaries for:
  - editor readiness
  - analyzer toggle state
  - overlay/provider support
- Kept the floating suggestion overlay functional on top of the rebuilt editor surface.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (updated editor shell launched; process remained responsive)

Known note:

- This sprint establishes editor structure and layout parity, not full attributed-text rendering parity.
- Word-class coloring, underline rendering, and flagged-word interaction still need later work on top of this shell baseline.

### WSA-RT-003_windows_external_input_and_caret_integration
Status: `Planned`

Scope:

- Implement global typing capture and focused-text tracking on Windows.
- Implement caret/selection/context extraction for supported targets.
- Implement external-app commit path with guarded fallbacks.

Priority targets:

- mainstream word processors
- email clients
- browsers needed for Google Docs

Policy:

- prefer follow-caret when supported
- fall back to static placement when caret anchoring is not trustworthy

### WSA-RT-004_windows_editor_right_click_correction_popover
Status: `Planned`

Scope:

- Implement right-click correction/context popover inside the Windows internal editor.
- Target unknown words, spelling markers, and later semantic/punctuation markers.
- Defer hover-triggered correction popovers until right-click parity is stable.

Target outcome:

- Right-clicking a flagged word opens the same kind of corrective surface the macOS editor exposes.

### WSA-TS-001_windows_smoke_and_regression_baseline
Status: `Done` (`2026-04-09`)

Scope:

- Define local smoke commands and validation artifacts.
- Add parity-focused regression checklist for Windows milestones.

Implemented:

- Added `scripts/run_app.ps1` to bootstrap the CLI, build the WPF app, and launch it with smoke-ready startup text.
- Added `docs/ManualSmoke.md` with canonical commands, checklist, expected behavior, and failure triage.
- Added startup sample text support so the app can open directly into a suggestion-validation state.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (launch initiated; process remained responsive)

Known note:

- Interactive GUI verification still depends on an operator-run smoke because the app window must be observed in a desktop session.

## Recommended implementation order

1. `WSA-DX-001` - baseline docs and repo structure
2. `WSA-RT-001` - internal editor + core bridge
3. `WSA-TS-001` - smoke and regression gates
4. `WSA-UX-001` - floating toolbar shell parity
5. `WSA-RT-002` - suggestion overlay parity
6. `WSA-UX-003` - overlay density refinement
7. `WSA-UX-002` - internal editor surface parity
8. `WSA-RT-004` - right-click correction popover
9. `WSA-RT-003` - external-app Windows integration

## Working rules for this repo

1. Each sprint gets a change-note in `docs/changes/`.
2. Each completed sprint updates this plan.
3. If validation is deferred, the reason must be written explicitly.
4. Commit messages should include the sprint ID at the start.
