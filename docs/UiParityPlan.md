# WordSuggestorWindows UI Parity Plan

Last updated: `2026-04-13`
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
| Language selector | Menu exposes supported languages and shows when packs are missing. | Replace Danish-only selector with a language/pack-aware selector and route selection through the core CLI bridge. | `WSA-RT-009_windows_language_pack_selection` done for Windows pack-aware selector baseline |
| Word list manager | Toolbar button is a TODO; settings exposes `isDomainListsEnabled`, and the domain-list tab is a placeholder. | Match current macOS behavior first; keep as placeholder/settings entry until a shared domain-list manager design exists. | Future shared/domain-list sprint |
| Import selected text | Prefers internal editor selection, then Accessibility/clipboard fallback from the frontmost app, then runs text analysis. | Prefer RichTextBox selection, then cached/live Windows UI Automation `TextPattern` selection, then guarded clipboard fallback; track app-specific blockers in a compatibility matrix. | `WSA-RT-010` + `WSA-RT-010A` done; `WSA-TS-002` diagnostics/matrix done |
| OCR / screen snip | Uses macOS `screencapture`, Vision OCR, copies text to clipboard, ingests into editor, then analyzes. | Use Windows screen snip plus Windows OCR via runtime WinRT bridge, copy recognized text to clipboard, ingest into editor, and analyze. | `WSA-RT-011` baseline done; `WSA-RT-011B` callback flow done; `WSA-RT-011C` diagnostics done; `WSA-RT-011D` file-access-token callback done |
| Speech to text | Uses Apple Speech framework with partial/final transcript replacement in the editor. | Use Windows speech recognition APIs and preserve the same active-range replacement model. | `WSA-RT-012_windows_speech_to_text_pipeline` baseline done |
| Text to speech | Resolves internal selection, external selection, or staged editor text; mirrors external text into the editor; supports selected/system voices and reading speed settings. | Implement toolbar-level TTS around Windows speech synthesis, language-aware voice selection, and editor highlighting; reuse/replace the current overlay-row speech service as needed. | `WSA-RT-013` baseline done; `WSA-RT-013A` voice selection/diagnostics done |
| Insights | Opens `ErrorInsightsView`, backed by local `ErrorTracking.sqlite` aggregates for suggestions, backspace, sentences, morphology, and frequent corrections. | Implement Windows-side error tracking store and native insights view with the same aggregate/privacy posture. | `WSA-RT-014_windows_error_insights_store_and_view` baseline done |
| Settings | Opens the native macOS Settings scene with categories for general, suggestions, text analysis, error tracking, advanced settings, plus legacy placeholder tabs for domain lists/profile. | Add a Windows-native settings window that preserves semantics, including placeholders where macOS is also placeholder-only. | `WSA-UX-010_windows_settings_window_parity` baseline done |

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

Status:

- Implemented on `2026-04-12`
- Danish resolves to the existing legacy pack `WordSuggestorCore\Ressources\da_lexicon.sqlite`
- Other macOS-supported languages are visible but marked as missing until their SQLite packs are installed

### WSA-RT-010_windows_selection_import_to_editor

Deliver:

- selected text import into the internal editor
- internal-editor selection preference
- Windows UI Automation selection extraction for external apps
- guarded clipboard fallback for apps that do not expose reliable selection text

Status:

- Implemented on `2026-04-12`
- Internal RichTextBox selection and live/recent cached external UI Automation selection are supported

### WSA-RT-010A_windows_selection_import_clipboard_fallback

Deliver:

- guarded clipboard fallback for apps that do not expose reliable UI Automation selection text
- focus targeting against the most recent external foreground window
- clipboard sentinel to avoid stale-text imports
- best-effort clipboard restoration after fallback copy

Status:

- Implemented on `2026-04-12`
- Clipboard fallback remains best-effort because external apps can block synthetic copy or foreground activation

### WSA-TS-002_windows_selection_import_app_compatibility_matrix

Deliver:

- non-content diagnostics for selected-text import routes
- compatibility matrix for priority apps and document/control modes
- result codes for UIA success, clipboard fallback success, partial support, and blocked apps

Status:

- Implemented on `2026-04-12`
- App-specific rows start as `UNTESTED` and should be filled through manual desktop smoke runs

### WSA-RT-011_windows_ocr_snip_pipeline

Deliver:

- Windows-native snipping/capture flow
- OCR text extraction
- clipboard copy and internal-editor ingest
- editor analysis refresh after ingest

Status:

- Implemented on `2026-04-12`
- Uses Microsofts Snipping Tool protocol callback flow plus a local PowerShell/WinRT OCR bridge to avoid adding an offline package restore dependency
- `WSA-RT-011B` supersedes `WSA-RT-011A`: the implementation no longer injects `Win+Shift+S`, but launches `ms-screenclip://capture/image?...` with a `wordsuggestor-ocr:` redirect callback and shared-storage token redemption
- `WSA-RT-011C` adds token-safe local OCR flow diagnostics in `%LOCALAPPDATA%\WordSuggestor\diagnostics\ocr-flow.log`
- `WSA-RT-011D` treats the observed `file-access-token` callback query parameter as the redeemable shared-storage token, avoiding clipboard/saved-file fallback as the primary OCR path
- Direct PDF-file OCR import is deferred; visible PDF content can be captured through the screen snip path

### WSA-RT-012_windows_speech_to_text_pipeline

Deliver:

- toolbar microphone start/stop state
- partial/final transcript handling
- active editor range replacement
- language-aware recognition where supported

Status:

- Implemented on `2026-04-13`
- Uses a local PowerShell bridge around Windows Desktop Speech Recognition because `System.Speech` is available to Windows PowerShell on this machine but is not directly resolvable as a compile-time assembly for the `net9.0-windows` WPF project
- Final recognized phrases are inserted at the internal editor caret; hypothesis text updates status only
- Toolbar `MIC` active state is reflected through button background and tooltip
- Language matching is best-effort against installed Windows Speech Recognition recognizers; this machine currently exposes only `en-GB`, so Danish dictation requires installing a Danish recognizer

### WSA-RT-013_windows_text_to_speech_selection_pipeline

Deliver:

- toolbar-level selected/staged text reading
- editor highlight integration while reading
- external selection mirroring into the editor when needed
- Windows-native speech synthesis service

Status:

- Implemented on `2026-04-13`
- Adds toolbar-level `TTS` start/stop behavior through a Windows SAPI bridge
- Reads internal editor selection first, then live external UIA selection, recent cached external UIA selection, guarded clipboard fallback selection, and finally staged editor text
- Mirrors external selections into the internal editor before playback for visible reading context
- Initial highlight parity is covered by `WSA-RT-013B`

### WSA-RT-013A_windows_tts_voice_selection_and_diagnostics

Deliver:

- persisted Windows speech settings aligned with the macOS system voice model
- SAPI voice catalog filtered by the active WordSuggestor language
- toolbar TTS fallback diagnostics
- user guidance for installing missing language voices

Status:

- Implemented on `2026-04-13`
- Adds per-language system voice overrides and reading speed settings to the Windows local settings profile
- Filters the settings voice picker by the selected WordSuggestor language and shows fallback voices only when no language match is installed
- Logs TTS flow to `%LOCALAPPDATA%\WordSuggestor\diagnostics\tts-flow.log`
- Current bridge still uses SAPI Desktop voices; neural/offline voice packs and OneCore voice playback remain follow-up work

### WSA-RT-013B_windows_tts_external_selection_and_highlight_parity

Deliver:

- UI-level TTS diagnostics before speech starts
- more robust external-app selection capture for toolbar and hotkey use
- visible editor word highlighting during playback
- global Windows hotkey for reading the current external selection without stealing foreground focus first

Status:

- Implemented on `2026-04-13`
- Adds `Ctrl+Alt+T` while WordSuggestor is running so users can mark text in Edge/Word/other apps and invoke TTS while that app remains foreground
- Reads the last known external target window through UI Automation before cached selection and clipboard fallback
- Hardens clipboard fallback with foreground verification, retry, and `SendInput` Win32 error diagnostics
- Adds light-blue active-token highlighting in the internal editor and restores internal editor selections after playback stops
- Uses estimated highlight timing around the current SAPI process bridge; exact word-boundary callbacks and OneCore voice playback remain follow-up work

### WSA-RT-013C_windows_tts_clipboard_fallback_and_highlight_tuning

Deliver:

- fix malformed Win32 input injection for external clipboard fallback
- tune the interim SAPI-process reading highlight so short text is visibly highlighted
- record the current OneCore/SAPI voice boundary clearly

Status:

- Implemented on `2026-04-13`
- Corrects the 64-bit `SendInput` `INPUT` union layout that caused `GetLastWin32Error=87` in the VS Code clipboard fallback path
- Uses a stronger light-blue highlight and a longer minimum highlight duration for short TTS text
- Confirms `Microsoft Helle - Danish (Denmark)` is installed as a OneCore voice but is not exposed through `SAPI.SpVoice.GetVoices()`; OneCore playback remains a dedicated backend follow-up

### WSA-RT-014_windows_error_insights_store_and_view

Deliver:

- local Windows error tracking store
- accepted suggestion, backspace, and sentence aggregate capture
- native insights view with totals, timelines, breakdowns, and frequent corrections

Status:

- Implemented on `2026-04-13`
- Captures accepted suggestions, backspace activity, and sentence boundary events in a local JSONL store under `%LOCALAPPDATA%\WordSuggestor\insights`
- Opens a native WPF insights view from the toolbar `INS` action with totals, recent events, suggestion-kind breakdown, part-of-speech breakdown, and frequent corrections
- Deeper charting/timeline parity and analyzer-backed morphology remain follow-up refinements

### WSA-UX-010_windows_settings_window_parity

Deliver:

- Windows-native settings window
- settings sections aligned with macOS semantics
- domain-list and profile placeholders preserved where macOS is also placeholder-only
- persisted Windows app settings ready for later shared profile alignment

Status:

- Implemented on `2026-04-13`
- Opens from the toolbar settings button
- Persists active Windows settings to `%LOCALAPPDATA%\WordSuggestor\settings\settings-v1.json`
- Categories are aligned with the current macOS settings layout: `Generelt`, `Ordforslag`, `Tekstanalyse`, `Fejlsporing`, and `Avanceret`
- Disabled placeholder controls are used for domain-list, sentence-example, debug/performance, and deeper voice/runtime options that are not yet implemented on Windows

## Guardrails

- Windows should not imitate AppKit chrome.
- Shared semantics must stay aligned with the macOS product.
- External-app support must be gated by placement confidence rather than optimistic guessing.
- The current normal-window WPF baseline is temporary and should not be treated as the final shell direction.
