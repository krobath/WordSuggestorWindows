# WordSuggestorWindows UI Parity Plan

Last updated: `2026-04-09`
Owner: `Windows track`
Status: `Approved implementation baseline from macOS UI review`

## Product direction

Windows WordSuggestor should be:

- native Windows in look and feel
- recognizably the same product as the macOS build
- aligned with macOS in control ordering, surface layout, and core interaction model

This means:

- reuse the same app icon across platforms
- use Windows-native icons when they are obvious and user-friendly
- allow SF Symbols fallback when Windows does not offer a clear equivalent
- preserve the toolbar layout, expanded editor structure, and suggestion panel semantics from macOS

## Locked decisions

### Shell model

- App startup should show a floating top toolbar, not a normal document window.
- The toolbar should be compact at startup and expandable downward into the internal editor.
- The expand/collapse affordance stays on the right side of the toolbar.

### Suggestion panel model

- The suggestion panel is a separate floating overlay, not embedded in the editor layout.
- It supports two placement modes:
  - static placement
  - follow-caret placement
- Follow-caret should be preferred where supported.
- If caret placement is not trustworthy, the runtime falls back to static placement.
- Pagination remains capped at 10 suggestions per page and up to 4 pages.

### Editor model

- The internal editor must support analysis coloring and underline semantics similar to macOS.
- Unknown words and spelling issues use red emphasis and underlining.
- Right-click is the first required trigger for the correction/context popover on Windows.
- Hover-triggered correction surfaces can be deferred.

### Shortcut model

- Windows candidate shortcuts should use `Ctrl+1` through `Ctrl+0`.

### External app priority

The first external targets for follow-caret and commit reliability are:

- word processors
- email clients
- browsers needed for Google Docs

## Confirmed macOS behaviors we are matching

### Floating toolbar

Source references:

- [WordSugestorApp.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/WordSugestorApp.swift)

Confirmed behavior:

- Floating toolbar shell is the primary app surface.
- Toolbar ordering is:
  - app icon/global on-off
  - language selector
  - word list
  - text import/analyzer
  - OCR
  - speech-to-text
  - text-to-speech
  - insights
  - settings
  - expand/collapse chevron

### Internal editor

Source references:

- [WordSugestorApp.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/WordSugestorApp.swift)
- [MacTextEditorRepresentable.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/MacTextEditorRepresentable.swift)
- [TextAnalyzerPanelView.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/TextAnalyzerPanelView.swift)

Confirmed behavior:

- Editor contains command row, main text area, status row, and analyzer summary/diagnostics panel.
- Word classes are color-coded.
- Spelling issues are shown with strong red underline and red emphasis.
- Semantic and punctuation diagnostics use blue underline treatment.

### Suggestion overlay

Source references:

- [SuggestionPanelView.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SuggestionPanelView.swift)
- [MacSuggestionPanelController.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/MacSuggestionPanelController.swift)
- [AppState.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/AppState.swift)

Confirmed behavior:

- Overlay is separate from the toolbar/editor shell.
- It is visually centered under the caret when follow-caret is active and supported.
- It exposes page controls and candidate metadata.
- It supports explicit placement mode switching.

### Correction popover

Source references:

- [MacTextEditorRepresentable.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/MacTextEditorRepresentable.swift)

Confirmed behavior:

- Right-click on a flagged word opens a richer correction/context surface.
- The popover can show status text, candidate list, and word-level actions.

## Windows implementation sprints

### WSA-UX-001_windows_toolbar_shell_parity

Deliver:

- floating toolbar shell
- shared app icon
- toolbar ordering parity
- expand/collapse transition into editor host

### WSA-UX-002_windows_internal_editor_surface_parity

Deliver:

- editor surface structure parity
- Windows-native attributed text rendering for analysis markers
- command row, status bar, and analyzer summary layout

### WSA-RT-002_windows_overlay_panel_and_commit_path

Deliver:

- separate floating suggestion overlay
- static and follow-caret modes
- pagination parity
- `Ctrl+1` through `Ctrl+0` candidate shortcuts
- static fallback when caret placement is unreliable

Status:

- Implemented for the internal editor baseline on `2026-04-09`
- External-app placement parity remains owned by `WSA-RT-003`

### WSA-RT-004_windows_editor_right_click_correction_popover

Deliver:

- right-click popover for spelling/unknown words first
- candidate insertion path from popover
- room for later semantic/punctuation expansion

### WSA-RT-003_windows_external_input_and_caret_integration

Deliver:

- cross-app capture and caret extraction
- initial support focus on word processors, mail clients, and Google Docs-capable browsers
- fallback to static placement when follow-caret quality is insufficient

## Guardrails

- Windows should not imitate AppKit chrome.
- Shared semantics must stay aligned with the macOS product.
- External-app support must be gated by placement confidence rather than optimistic guessing.
- The current normal-window WPF baseline is temporary and should not be treated as the final shell direction.
