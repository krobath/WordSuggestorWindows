# WordSuggestorWindows UI Parity Plan

Last updated: `2026-04-11`
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
- [AppState.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/AppState.swift)
- [ScreenSnipper.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/ScreenSnipper.swift)
- [SpeechToTextService.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SpeechToTextService.swift)
- [SpeechHighlighter.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SpeechHighlighter.swift)
- [ErrorInsightsView.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/ErrorInsightsView.swift)
- [SettingsView.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SettingsView.swift)

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

### Toolbar feature parity roadmap

Source references:

- [WordSugestorApp.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/WordSugestorApp.swift)
- [AppState.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/AppState.swift)
- [ScreenSnipper.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/ScreenSnipper.swift)
- [SpeechToTextService.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SpeechToTextService.swift)
- [SpeechHighlighter.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SpeechHighlighter.swift)
- [ErrorInsightsView.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/ErrorInsightsView.swift)
- [SettingsView.swift](c:/Users/mswin01/Code/WordSuggestor/WordSuggestor/WordSuggestor/SettingsView.swift)
- [MainWindow.xaml](c:/Users/mswin01/Code/WordSuggestor/WordSuggestorWindows/src/WordSuggestorWindows.App/MainWindow.xaml)
- [MainWindowViewModel.cs](c:/Users/mswin01/Code/WordSuggestor/WordSuggestorWindows/src/WordSuggestorWindows.App/ViewModels/MainWindowViewModel.cs)

| Toolbar capability | macOS behavior confirmed | Windows parity strategy | Sprint |
|---|---|---|---|
| Toggle word suggestions on/off | `isGlobalCaptureEnabled` starts/stops `GlobalKeyCaptureManager` and persists the setting. | Connect the Windows toggle to global typing capture, focused text tracking, external commit, and overlay visibility. | `WSA-RT-003_windows_external_input_and_caret_integration` |
| Language selector | Menu exposes supported languages and shows when packs are missing. | Replace Danish-only selector with a language/pack-aware selector and route selection through the core CLI bridge. | `WSA-RT-009_windows_language_pack_selection` |
| Word list manager | Toolbar button is a TODO; settings exposes `isDomainListsEnabled`, and the domain-list tab is a placeholder. | Match current macOS behavior first; keep as placeholder/settings entry until a shared domain-list manager design exists. | Future shared/domain-list sprint |
| Import selected text | Prefers internal editor selection, then Accessibility/clipboard fallback from the frontmost app, then runs text analysis. | Prefer RichTextBox selection, then Windows UI Automation `TextPattern`/`TextPattern2`, with guarded clipboard fallback. | `WSA-RT-010_windows_selection_import_to_editor` |
| OCR / screen snip | Uses macOS `screencapture`, Vision OCR, copies text to clipboard, ingests into editor, then analyzes. | Use Windows-native screen capture/snipping plus OCR, copy recognized text to clipboard, ingest into editor, and analyze. | `WSA-RT-011_windows_ocr_snip_pipeline` |
| Speech to text | Uses Apple Speech framework with partial/final transcript replacement in the editor. | Use Windows speech recognition APIs and preserve the same active-range replacement model. | `WSA-RT-012_windows_speech_to_text_pipeline` |
| Text to speech | Resolves internal selection, external selection, or staged editor text; mirrors external text into the editor and highlights during playback. | Implement toolbar-level TTS around Windows speech synthesis and editor highlighting; reuse/replace the current overlay-row speech service as needed. | `WSA-RT-013_windows_text_to_speech_selection_pipeline` |
| Insights | Opens `ErrorInsightsView`, backed by local `ErrorTracking.sqlite` aggregates for suggestions, backspace, sentences, morphology, and frequent corrections. | Implement Windows-side error tracking store and native insights view with the same aggregate/privacy posture. | `WSA-RT-014_windows_error_insights_store_and_view` |
| Settings | Opens the native macOS Settings scene with tabs for general, sound, writing, domain lists, and profile. | Add a Windows-native settings window that preserves semantics, including placeholders where macOS is also placeholder-only. | `WSA-UX-010_windows_settings_window_parity` |

Implementation notes:

- "Identical functionality" means matching user-visible behavior and product semantics, not reusing Apple-specific APIs.
- Global input, OCR, speech-to-text, and TTS must be Windows-native implementations because the macOS code depends on AX, Vision, Speech, and AVSpeechSynthesizer.
- The word-list toolbar button should not be overbuilt in Windows before macOS/shared functionality exists; parity currently means preserving the placeholder behavior and documenting the later shared design need.

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

Status:

- Structure/layout baseline implemented on `2026-04-10`
- Rich attributed-text parity remains follow-up work on top of the rebuilt editor shell

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

### WSA-RT-005_windows_overlay_static_drag_and_rich_rows

Deliver:

- true manual static placement for the suggestion overlay
- richer candidate rows with inline match labels
- metadata line with ordklasse and grammar
- row-level speaker and info actions
- kind-based row tinting aligned with the macOS suggestion semantics

Status:

- Implemented for the internal editor baseline on `2026-04-10`
- static placement persistence currently lasts for the running session

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

### WSA-RT-009_windows_language_pack_selection

Deliver:

- supported-language selector parity with macOS
- pack availability indicator
- selected language and pack path routed into the Windows core bridge

### WSA-RT-010_windows_selection_import_to_editor

Deliver:

- selected text import into the internal editor
- internal-editor selection preference
- Windows UI Automation selection extraction for external apps
- guarded clipboard fallback for apps that do not expose reliable selection text

### WSA-RT-011_windows_ocr_snip_pipeline

Deliver:

- Windows-native snipping/capture flow
- OCR text extraction
- clipboard copy and internal-editor ingest
- editor analysis refresh after ingest

### WSA-RT-012_windows_speech_to_text_pipeline

Deliver:

- toolbar microphone start/stop state
- partial/final transcript handling
- active editor range replacement
- language-aware recognition where supported

### WSA-RT-013_windows_text_to_speech_selection_pipeline

Deliver:

- toolbar-level selected/staged text reading
- editor highlight integration while reading
- external selection mirroring into the editor when needed
- Windows-native speech synthesis service

### WSA-RT-014_windows_error_insights_store_and_view

Deliver:

- local Windows error tracking store
- accepted suggestion, backspace, and sentence aggregate capture
- native insights view with totals, timelines, breakdowns, and frequent corrections

### WSA-UX-010_windows_settings_window_parity

Deliver:

- Windows-native settings window
- settings sections aligned with macOS semantics
- domain-list and profile placeholders preserved where macOS is also placeholder-only
- persisted Windows app settings ready for later shared profile alignment

## Guardrails

- Windows should not imitate AppKit chrome.
- Shared semantics must stay aligned with the macOS product.
- External-app support must be gated by placement confidence rather than optimistic guessing.
- The current normal-window WPF baseline is temporary and should not be treated as the final shell direction.
