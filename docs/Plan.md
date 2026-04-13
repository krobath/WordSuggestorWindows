# WordSuggestorWindows Plan

Last updated: `2026-04-12`
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

### WSA-DX-003_windows_toolbar_feature_parity_roadmap
Status: `Done` (`2026-04-11`)

Scope:

- Capture the macOS toolbar feature analysis as a Windows implementation roadmap.
- Distinguish implemented macOS behavior from placeholder/TODO behavior before planning Windows parity work.
- Split the remaining toolbar features into correctly named Windows sprints.

Implemented:

- Added the toolbar feature parity roadmap to `docs/UiParityPlan.md`.
- Updated `docs/ParityMatrix.md` with per-button status for global suggestions, language selection, word lists, import, OCR, speech-to-text, text-to-speech, insights, and settings.
- Added the new planned implementation sprints below so each toolbar capability can be implemented and documented independently.
- Normalized the next Windows settings sprint to `WSA-UX-010_windows_settings_window_parity`, which is the next available Windows UX number after `WSA-UX-009`.

Validation:

- Documentation-only review; no app build required because this sprint changes planning documents only.

Known note:

- The "Fagordslister" toolbar button is a macOS TODO today. Windows parity should first preserve that current behavior rather than inventing a new manager before a shared word-list design exists.

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

### WSA-RT-007_windows_clear_overlay_after_accept
Status: `Done` (`2026-04-11`)

Scope:

- Clear the floating suggestion overlay immediately after the user accepts a suggestion in the internal editor.
- Preserve the accepted-word insertion behavior from `WSA-UX-005`: accepted word, trailing space, caret after the space.
- Ensure old candidates stay hidden until the user starts typing the next token.

Implemented:

- Added a shared `ClearSuggestionSession()` helper in `MainWindowViewModel`.
- Updated `ExecuteAcceptSelectedSuggestion()` so every accept route (`Tab`, `Ctrl+1` through `Ctrl+0`, overlay click) clears the current candidate list after inserting the accepted term.
- Cancelled the pending suggestion refresh that is scheduled by the `EditorText` update during accept, preventing stale suggestions from reappearing immediately after the inserted trailing space.
- Reset selected suggestion, current page, busy state, and overlay-related property notifications through the existing suggestion page state path.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after the launch smoke showed no new `WordSuggestorWindows.App` crash event.

Known note:

- Superseded by `WSA-RT-008`: the accepted behavior is now that the overlay remains visible but empty after accept/boundary input.

### WSA-RT-008_windows_empty_overlay_boundary_state
Status: `Done` (`2026-04-11`)

Scope:

- Keep the floating suggestion overlay visible when the user presses `Space`, presses `Enter`, or accepts a suggestion.
- Clear the previous candidates in those boundary states so stale suggestions are not shown.
- Refill the overlay when the user begins typing the next token.

Implemented:

- Added `_isSuggestionOverlaySessionVisible` so overlay visibility no longer depends directly on `HasSuggestions`.
- Changed `ShouldShowSuggestionOverlay` to use the overlay session state, allowing the box to remain visible with zero suggestions.
- Added boundary-token detection so text ending in whitespace, newline, or another non-token character clears the candidates immediately and keeps the overlay visible.
- Kept empty-document state hidden by clearing the session with `keepOverlayVisible: false`.
- Updated accept handling to clear the candidate list while preserving the visible overlay box after the accepted word and trailing space are inserted.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after the launch smoke showed no new `WordSuggestorWindows.App` crash event.

Known note:

- Manual smoke should confirm the overlay remains visible but empty after `Space`, `Enter`, `Tab` accept, `Ctrl+1` accept, and overlay mouse-click accept.

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
- A startup crash discovered after this pass is tracked and fixed in `WSA-UX-007_windows_rich_editor_startup_fix`.

### WSA-UX-007_windows_rich_editor_startup_fix
Status: `Done` (`2026-04-11`)

Scope:

- Fix the startup regression where the Windows app exited immediately after `scripts\run_app.ps1` printed the launch message.
- Preserve the new rich editor surface from `WSA-UX-006` instead of reverting it.
- Document the event-log diagnosis for future Windows UI regression triage.

Implemented:

- Used the Windows Application event log to identify the crash as a `System.NullReferenceException` in `MainWindow.EditorTextBox_OnTextChanged`.
- Confirmed the `RichTextBox.Document` assignment can raise `TextChanged` while XAML is still loading the window.
- Moved `MainWindowViewModel` assignment before `InitializeComponent()` so early rich-editor events have access to the view model.
- Kept `DataContext` assignment after component initialization, so XAML binding behavior remains unchanged while the event handler is safe during construction.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (app launched after core CLI bootstrap; process remained responsive)
- Application event log check after the fixed launch showed no new `WordSuggestorWindows.App` crash event.

Known note:

- The `pkg-config`/`sqlite3.pc` and Swift symlink warnings can still appear during core CLI bootstrap, but `scripts\test_core_cli.ps1` confirms the Danish SQLite pack loads and returns suggestions.

### WSA-UX-008_windows_editor_layout_statusbar_startup_parity
Status: `Done` (`2026-04-11`)

Scope:

- Keep the Windows shell collapsed on startup, even when `run_app.ps1` injects smoke sample text.
- Remove implementation-note copy from the user-facing `Tekstanalyse` panel.
- Rework the editor status metrics from four separate cards into one compact status bar.
- Rebalance vertical spacing so the internal editor receives more of the expanded window height and the analyzer legend no longer leaves unnecessary bottom whitespace.

Implemented:

- Changed startup sample handling so initial editor text is preserved and suggestions can still refresh, but `IsEditorExpanded` remains collapsed until the user opens the editor.
- Removed the descriptive Windows parity note from the `Tekstanalyse` UI.
- Replaced the four-card status metric `UniformGrid` with a single bordered horizontal status bar using the existing `StatusMetrics` view model contract.
- Tightened command row, editor container, status bar, and analyzer legend padding/margins to move more vertical space into the `RichTextBox` row.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild` -> `PASS` (app launched after core CLI bootstrap; process remained responsive)

Known note:

- Visual micro-tuning may still be needed after comparing a fresh screenshot against the macOS reference, especially exact status-bar spacing and legend wrapping.

### WSA-UX-009_windows_editor_vertical_space_and_statusbar_tuning
Status: `Done` (`2026-04-11`)

Scope:

- Remove remaining unused vertical space below the editor/analyzer region by making the expanded editor body stretch to the fixed expanded window height.
- Tune the compact status bar closer to the macOS/Word-like reference where each metric reads as a single inline status item.

Implemented:

- Changed the root shell grid's expanded editor row from `Auto` to `*`, allowing the editor body to consume the remaining expanded-window height.
- Set the expanded editor body and inner grid to stretch vertically so the `RichTextBox` row now receives the extra space instead of leaving blank space below the analysis legend.
- Tuned the status bar item layout from `Icon Label Value` to `Icon Value Label`, closer to the macOS reference format such as `Aa 69 tegn`.
- Removed the status bar's per-item separator line and tightened the bar/legend vertical padding.

Validation:

- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after the launch smoke showed no new `WordSuggestorWindows.App` crash event.

Known note:

- First build attempt was blocked by a still-running `WordSuggestorWindows.App.exe`; after stopping that local test process, the build passed.

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

### WSA-RT-009_windows_language_pack_selection
Status: `Done` (`2026-04-12`)

Scope:

- Replace the current Danish-only Windows language selector with the same language choices exposed by the macOS toolbar.
- Show pack availability clearly when a language pack is missing.
- Route the selected language and pack path through the Windows `WordSuggestorCore` CLI bridge.

Implemented:

- Added a Windows `LanguageOption` model for language code, compact toolbar label, display label, pack tag, legacy pack filename, and resolved pack path.
- Replaced the hardcoded `DA` selector with the macOS-supported language set:
  - Danish
  - English
  - German
  - French
  - Spanish
  - Italian
  - Swedish
  - Norwegian Bokmal
  - Norwegian Nynorsk
- Added Windows-side pack discovery that mirrors the core/macOS pack naming convention:
  - `%APPDATA%\WordSuggestor\Packs\<tag>_pack_v*.sqlite`
  - `%APPDATA%\WordSuggestor\Packs\<tag>_pack.sqlite`
  - `WordSuggestorWindows\Packs\<tag>_pack*.sqlite`
  - `WordSuggestorCore\Ressources\<tag>_pack*.sqlite`
  - legacy `da_lexicon.sqlite` / `en_lexicon.sqlite`
- Updated the CLI bridge to pass the selected language code and resolved pack path into `WordSuggestorSuggestCLI`.
- Kept missing-pack languages selectable but safe: the toolbar shows an availability marker, the status message explains that the pack is missing, and suggestions remain empty instead of failing the app.
- Updated the provider summary so the active language is visible in runtime diagnostics.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.

Known note:

- The current workspace only contains the Danish legacy pack `WordSuggestorCore\Ressources\da_lexicon.sqlite`, so other languages correctly show as missing-pack options until their SQLite packs are installed.

Target outcome:

- The Windows language selector matches macOS product behavior while using Windows-native control styling.

### WSA-RT-010_windows_selection_import_to_editor
Status: `Done` (`2026-04-12`)

Scope:

- Implement the toolbar action that imports selected text into the internal editor.
- Prefer internal editor selection when WordSuggestor owns the selection.
- Use Windows UI Automation for external app selection, with guarded clipboard fallback only when WordSuggestor is not the foreground target.
- Normalize the imported text and run the same editor analysis path used for OCR/imported text.

Implemented:

- Added a Windows selection import result model and Windows UI Automation selection import service.
- Added a lightweight external selection polling path in `MainWindow` that captures recent selected text while another app is foreground.
- Changed the top toolbar `TXT` action from placeholder status text to real import behavior.
- Implemented import priority:
  - current internal RichTextBox selection
  - live external UI Automation selection when another app is still foreground
  - recent cached external UI Automation selection captured before the toolbar was clicked
- Added view-model import handling that expands the editor, replaces the editor text with the imported text, moves the caret to the end, and lets the existing suggestion refresh path run when the imported text ends in an active token.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\test_core_cli.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.

Known note:

- This sprint's original UI Automation-only limitation is hardened by `WSA-RT-010A_windows_selection_import_clipboard_fallback`.
- External selection support depends on the target app exposing selection through Windows UI Automation `TextPattern`.

Target outcome:

- The Windows `TXT` toolbar action behaves like macOS `state.importSelectionForAnalysis()`.

### WSA-RT-010A_windows_selection_import_clipboard_fallback
Status: `Done` (`2026-04-12`)

Scope:

- Harden selected-text import so it works in more apps than UI Automation-only selection import.
- Add a guarded clipboard fallback that targets the most recent external foreground window, not WordSuggestor itself.
- Preserve the user's clipboard as far as possible after the fallback copy attempt.

Implemented:

- Added a sentinel-based clipboard copy fallback in `WindowsSelectionImportService`.
- Stored the most recent non-WordSuggestor foreground window while polling for external selection.
- Updated the `TXT` toolbar action so fallback order is now:
  - internal RichTextBox selection
  - live external UI Automation selection
  - recent cached external UI Automation selection
  - guarded clipboard fallback using `Ctrl+C` against the most recent external foreground window
- Restored WordSuggestor focus after the fallback attempt.
- Restored the previous clipboard data object after the copy attempt where Windows clipboard access permits it.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.

Known note:

- Clipboard fallback is best-effort because some apps block synthetic copy, expose delayed clipboard formats, or do not allow WordSuggestor to bring them foreground programmatically.
- This fallback deliberately uses a sentinel to avoid importing stale clipboard text when no copy actually happened.

### WSA-TS-002_windows_selection_import_app_compatibility_matrix
Status: `Done` (`2026-04-12`)

Scope:

- Add lightweight non-content diagnostics for the selected-text import path.
- Create a manual compatibility matrix for identifying which Windows applications expose selected text and which block UI Automation or clipboard fallback.
- Make the app-testing process repeatable before expanding the broader external-app integration sprint.

Implemented:

- Added `SelectionImportDiagnostic` as a non-content diagnostic model for selection-import stages and outcomes.
- Emitted diagnostics from `WindowsSelectionImportService` for UI Automation selection, guarded clipboard fallback activation, synthetic copy, selection detection, and clipboard restoration.
- Routed diagnostics from `MainWindow` to the debugger output stream and `%LOCALAPPDATA%\WordSuggestor\diagnostics\selection-import.log` using the prefix `WordSuggestor selection import:`.
- Added `docs/SelectionImportCompatibilityMatrix.md` with result codes, priority app list, and a manual recording template.
- Updated manual smoke and parity docs so selected-text import support is tracked as an empirical compatibility matrix.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

Known note:

- The diagnostics intentionally log only stage, outcome, target handle, and character counts. They must not log the selected text itself.
- The matrix starts as `UNTESTED`; app-specific results should be filled in through manual desktop testing.
- The first build validation was blocked by a still-running local `WordSuggestorWindows.App.exe`; after stopping that test process, the build passed.

### WSA-RT-011_windows_ocr_snip_pipeline
Status: `Done` (`2026-04-12`)

Scope:

- Implement Windows-native screen/PDF/image snipping for OCR input.
- Run OCR with a Windows-compatible OCR backend.
- Copy recognized text to the clipboard and ingest it into the internal editor.
- Trigger the existing editor analysis path after text is ingested.

Implemented:

- Added `WindowsOcrService` for Windows-native OCR ingestion through the system screen snip clipboard path.
- Added `OcrImportResult` as the Windows OCR import contract.
- Wired the top toolbar `OCR` action to:
  - hide the WordSuggestor toolbar while the screen area is captured,
  - launch the Windows screen snip overlay through synthetic `Win+Shift+S`,
  - wait for the captured image to appear on the clipboard,
  - run Windows OCR through a local PowerShell/WinRT bridge,
  - copy recognized text back to the clipboard,
  - ingest recognized text into the internal editor through the same import/analyzer path used by selected-text import.
- Added OCR text normalization for line endings, soft line wrapping, simple list breaks, and OCR hyphenation markers.
- Kept the app on `net9.0-windows` and avoided a new `Microsoft.Windows.SDK.NET.Ref` package restore dependency by using a runtime bridge for Windows OCR.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- Direct Windows OCR API smoke against a generated PNG containing `Hej OCR test` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorWindows diff --check` -> `PASS` (only Git CRLF/LF normalization warnings on existing Windows files)
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

Known note:

- This sprint implements the macOS-like screen snip path. Direct PDF-file OCR import is deferred; visible PDF content can be OCR'ed by selecting it with the screen snip.
- The first attempt to compile directly against WinRT OCR required `Microsoft.Windows.SDK.NET.Ref`, which was not available offline in this workspace, so the implementation uses a local PowerShell/WinRT bridge instead.
- The launch-smoke process was stopped after validation so the debug executable is not left locked.

Target outcome:

- The Windows `OCR` toolbar action matches the macOS `ScreenSnipper` product flow with Windows-native APIs.

### WSA-RT-011A_windows_ocr_screen_snip_invocation_fix
Status: `Done` (`2026-04-12`)

Scope:

- Fix OCR startup on Windows installations where the `ms-screenclip:` URI opens the Snipping Tool window instead of the direct capture overlay.
- Preserve the OCR recognition, clipboard text copy, and internal-editor ingest path from `WSA-RT-011`.

Implemented:

- Replaced the `ms-screenclip:` URI launch with a native `SendInput` sequence for `Win+Shift+S`.
- Kept the existing clipboard image wait, OCR bridge, text normalization, clipboard text copy, and editor import behavior unchanged.
- Updated OCR documentation to describe the `Win+Shift+S` overlay invocation path.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

Known note:

- Manual confirmation is still needed because the actual screen selection overlay is interactive.
- The launch-smoke process was stopped after validation so the debug executable is not left locked.
- Superseded by `WSA-RT-011B_windows_ocr_snipping_tool_protocol_callback`, which replaces hotkey injection with Microsofts Snipping Tool protocol callback flow.

### WSA-RT-011B_windows_ocr_snipping_tool_protocol_callback
Status: `Done` (`2026-04-12`)

Scope:

- Replace the fragile `Win+Shift+S` hotkey injection path from `WSA-RT-011A`.
- Use Microsofts Snipping Tool protocol integration with redirect callback and shared-storage token retrieval.
- Preserve OCR recognition, clipboard text copy, and internal-editor ingest behavior.

Implemented:

- Added `OcrScreenClipCallback` as the parsed Snipping Tool callback contract.
- Added `WindowsOcrCallbackBridge` to:
  - detect `wordsuggestor-ocr:` startup callbacks,
  - reconstruct split callback URI arguments defensively,
  - parse `code`, `reason`, `token`, and correlation id,
  - persist callback URI data to `%LOCALAPPDATA%\WordSuggestor\ocr-callbacks`,
  - emit non-token diagnostics to `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log`.
- Changed app startup so OCR callback processes are handled before normal WPF startup and exit without opening the main window.
- Changed `WindowsOcrService` to:
  - register `wordsuggestor-ocr:` under `HKCU\Software\Classes`,
  - launch `ms-screenclip://capture/image?...` with `rectangle`, `enabledModes=RectangleSnip`, `auto-save=false`, `user-agent=WordSuggestor`, correlation id, and redirect URI,
  - wait for the matching callback file,
  - redeem the returned shared-storage token through a local PowerShell/WinRT bridge,
  - run the existing Windows OCR bridge on the redeemed image,
  - copy recognized text to the clipboard and import it into the editor.
- Removed the `SendInput` hotkey injection path.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process remained responsive)
- Application event log check after launch showed no recent `WordSuggestorWindows.App` crash event.
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

Known note:

- Manual confirmation is still needed because the Snipping Tool screen selection and redirect callback are interactive.
- The protocol registration is per-user under HKCU and does not require admin rights.
- The launch-smoke process was stopped after validation so the debug executable is not left locked.

### WSA-RT-011C_windows_ocr_flow_diagnostics
Status: `Done` (`2026-04-12`)

Scope:

- Add token-safe diagnostics for the OCR path after the Snipping Tool callback is received.
- Make it possible to distinguish token redemption failure, OCR bridge failure, clipboard failure, and editor import failure.
- Preserve the existing OCR user flow while adding local-only troubleshooting output.

Implemented:

- Added `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` as the main OCR flow diagnostic log.
- Logged the UI OCR action lifecycle: toolbar click, hiding/restoring the WordSuggestor window, null result, and successful editor import.
- Logged the service OCR lifecycle: callback protocol registration, Snipping Tool launch, callback wait/result, shared-storage token redemption, OCR bridge execution, normalization length, clipboard copy failure, and final cleanup.
- Kept callback token values and recognized OCR text out of diagnostics; logs contain only correlation ids, paths, exit codes, stderr summaries, and character/line counts.
- Changed the OCR bridge to read from the actual redeemed image path returned by token redemption rather than assuming it always equals the requested temp path.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\run_app.ps1 -SkipBuild -SkipBootstrap` -> `PASS` (app launched; process was stopped after smoke)
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)
- Installed recognizer check -> `en-GB Microsoft Speech Recognizer 8.0 for Windows (English - UK)`
- Application event log check showed no useful recent `WordSuggestorWindows.App` failures for the OCR import issue; the new local flow log is the next troubleshooting source.

Known note:

- The interactive Snipping Tool capture must still be tested manually. If OCR still does not import text, inspect `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` together with `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-callback.log`.

### WSA-RT-011D_windows_ocr_file_access_token_callback
Status: `Done` (`2026-04-13`)

Scope:

- Handle Snipping Tool success callbacks that return the shared-storage token as `file-access-token`.
- Preserve the documented `token` parameter as the primary path while accepting the actual Windows callback key observed in local testing.
- Avoid clipboard or saved-screenshot fallback paths for the primary OCR implementation.

Implemented:

- Changed `OcrScreenClipCallback.IsSuccess` so HTTP-like `code=200` is treated as success even when the callback token is missing.
- Added token-safe callback diagnostics that log callback query parameter names, token presence, and token length without logging query values.
- Added `file-access-token` as the observed Snipping Tool callback token key, while keeping `token` and other shared-storage aliases for compatibility.
- Removed the clipboard-image fallback path from the intended OCR flow; the robust path is now callback token parsing plus shared-storage token redemption.
- Kept diagnostics for the no-token case so the app reports a contract failure instead of silently falling back to version-dependent clipboard behavior.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

Known note:

- Local logs showed `keys=code,file-access-token,reason,x-request-correlation-id`, which is why `file-access-token` is now treated as the token key.
- If OCR still fails, `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log` will show whether token redemption or the OCR bridge is the failing stage.

### WSA-RT-012_windows_speech_to_text_pipeline
Status: `Done` (`2026-04-13`)

Scope:

- Implement speech-to-text dictation into the internal editor.
- Support start/stop state from the toolbar microphone button.
- Apply partial transcripts into the active editor range and finalize the replacement when recognition completes.
- Respect the active app language where the Windows speech API supports it.

Implemented:

- Added `WindowsSpeechToTextService`, a local PowerShell bridge around Windows Desktop Speech Recognition.
- Wired the toolbar `MIC` action to start/stop the speech bridge.
- Added toolbar button state via `IsSpeechToTextListening`, `SpeechToTextToolTip`, and `SpeechToTextButtonBackground`.
- Added final transcript insertion into the internal editor at the current caret position.
- Added hypothesis status updates so the status line shows what Windows is currently hearing without inserting partial text prematurely.
- Added language-aware recognizer selection:
  - exact active language match when a recognizer is installed,
  - same two-letter language fallback when available,
  - first installed recognizer as a final fallback.
- Added `SpeechToTextTranscript` as the internal transcript event model.

Validation:

- `powershell -ExecutionPolicy Bypass -File .\WordSuggestorWindows\scripts\build_app.ps1` -> `PASS`
- `git -C .\WordSuggestorWindows diff --check` -> `PASS`
- `git -C .\WordSuggestorCore status --short` -> `PASS` (no changes)

Known note:

- The current Windows machine only reports `Microsoft Speech Recognizer 8.0 for Windows (English - UK)` / `en-GB` via `System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers()`. Danish dictation requires installing a Danish Windows Speech Recognition recognizer; until then Danish UI language selection falls back to the installed recognizer.
- Direct PowerShell probing also showed that grammar loading can fail with `E_ACCESSDENIED` in this environment if Windows speech recognition/audio permissions are not fully available to the desktop session. The app surfaces bridge stderr in the status line instead of failing silently.
- Manual microphone permission/audio-device testing is still required because CI/build validation cannot speak into the microphone.

Target outcome:

- The Windows `MIC` toolbar action has a first native start/stop dictation path into the internal editor and can be refined toward fuller macOS partial-range replacement parity.

### WSA-RT-013_windows_text_to_speech_selection_pipeline
Status: `Planned`

Scope:

- Implement toolbar-level text-to-speech for selected or staged editor text.
- Prefer internal editor selection, then external app selection, then staged editor content.
- Mirror external selection into the editor when needed so highlighting can be shown during playback.
- Reuse or replace the current overlay-row SAPI bridge with a more direct Windows TTS service when practical.

Target outcome:

- The Windows `TTS` toolbar action matches the macOS `speakSelection()` behavior, including visible reading context in the editor.

### WSA-RT-014_windows_error_insights_store_and_view
Status: `Planned`

Scope:

- Implement the Windows equivalent of local error tracking for accepted suggestions, backspace activity, and sentence boundary events.
- Store aggregate-oriented insight data locally with the same privacy posture as macOS.
- Add the Windows insights view for totals, timeline, suggestion-kind breakdown, morphology breakdown, and frequent corrections.

Target outcome:

- The Windows `INS` toolbar action opens a native insights view backed by Windows-side tracking data.

### WSA-UX-010_windows_settings_window_parity
Status: `Planned`

Scope:

- Implement a Windows-native settings window with tabs/sections matching the macOS settings model.
- Include settings for suggestions, editor behavior, speech/TTS, error tracking, text analysis, domain lists, diagnostics, and profile placeholders where macOS currently exposes placeholders.
- Persist settings through the Windows app state in a way that can later align with shared cross-platform profile data.

Target outcome:

- The Windows settings button opens a native settings surface that preserves macOS semantics without copying AppKit/SwiftUI chrome.

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
10. `WSA-RT-009` - language pack selection parity
11. `WSA-RT-010` - selected text import into editor
12. `WSA-RT-010A` - selected text import clipboard fallback
13. `WSA-TS-002` - selected text import app compatibility matrix
14. `WSA-RT-011` - OCR snip pipeline
15. `WSA-RT-011A` - OCR screen snip invocation fix
16. `WSA-RT-011B` - OCR Snipping Tool callback protocol
17. `WSA-RT-011C` - OCR flow diagnostics
18. `WSA-RT-011D` - OCR file-access-token callback
19. `WSA-RT-012` - speech-to-text dictation pipeline
20. `WSA-RT-013` - toolbar text-to-speech selection pipeline
21. `WSA-RT-014` - error insights store and view
22. `WSA-UX-010` - settings window parity

## Working rules for this repo

1. Each sprint gets a change-note in `docs/changes/`.
2. Each completed sprint updates this plan.
3. If validation is deferred, the reason must be written explicitly.
4. Commit messages should include the sprint ID at the start.
