# WordSuggestor macOS -> Windows Parity Matrix

Last updated: `2026-04-11`
Status: `Parity plan aligned with macOS UI review`

## Legend

- `Shared` - expected to reuse `WordSuggestorCore` or shared semantics
- `New Windows impl` - requires Windows-native implementation
- `Deferred` - not part of the first Windows milestone

| Capability | macOS source | Windows strategy | Status |
|---|---|---|---|
| Shared app icon | `Assets.xcassets/AppIcon.appiconset` | Reuse same app icon asset on Windows | `Baseline done` |
| Floating top toolbar shell | `FloatingToolbarView` + `FloatingToolbarPanel` | Rebuild with Windows-native chrome and same control ordering | `Baseline done` |
| Toolbar control ordering | `FloatingToolbarView` | Preserve macOS ordering, allow Windows-native icons where obvious | `Baseline done` |
| Toolbar global suggestions toggle | `AppState.isGlobalCaptureEnabled` + `GlobalKeyCaptureManager` | Connect existing Windows toggle to global capture, focused text tracking, and overlay session control | `Planned: WSA-RT-003` |
| Toolbar language selector | `FloatingToolbarView.supportedLanguages` + `SQLiteLanguagePackSource.defaultLocator` | Replace Danish-only Windows selector with language/pack-aware selector | `Planned: WSA-RT-009` |
| Toolbar word-list button | `FloatingToolbarView` TODO + `SettingsView.domainTab` placeholder | Preserve placeholder/settings behavior until shared domain-list manager exists | `Placeholder parity only` |
| Toolbar selected-text import | `AppState.importSelectionForAnalysis()` | Use internal editor selection first, then Windows UI Automation selection, then guarded clipboard fallback | `Planned: WSA-RT-010` |
| Toolbar OCR action | `ScreenSnipper.swift` + `AppState.ingestOCRText()` | Windows-native snipping/capture, OCR, clipboard copy, editor ingest, analysis refresh | `Planned: WSA-RT-011` |
| Toolbar speech-to-text action | `AppState.toggleSpeechToTextIntoEditor()` + `SpeechToTextService.swift` | Windows speech recognition with active-range partial/final transcript replacement | `Planned: WSA-RT-012` |
| Toolbar text-to-speech action | `FloatingToolbarView.speakSelection()` + `SpeechHighlighter.swift` | Windows-native selected/staged text speech with editor highlight integration | `Planned: WSA-RT-013` |
| Toolbar insights action | `ErrorInsightsView.swift` + `ErrorTrackingStore.swift` | Windows error tracking store plus native insights view | `Planned: WSA-RT-014` |
| Toolbar settings action | SwiftUI `Settings` scene + `SettingsView.swift` | Windows-native settings window preserving macOS semantics | `Planned: WSA-UX-010` |
| Expand/collapse toolbar -> editor shell | `FloatingToolbarInstaller` sizing/placement | Rebuild in Windows panel host | `Baseline done` |
| Suggestion engine | `WordSuggestorCore` | Reuse shared core | `Shared` |
| Language packs / SQLite lexicon | `WordSuggestorCore` | Reuse shared core and pack assets | `Shared; Windows selector dynamic routing planned in WSA-RT-009` |
| Internal editor typing flow | SwiftUI/AppKit app state | Rebuild in Windows app shell | `Baseline done inside structured editor shell` |
| Editor surface structure | `WordSugestorApp.swift` + `TextAnalyzerPanelView.swift` | Rebuild command row, editor area, status row, and analyzer panel layout natively on Windows | `Baseline done` |
| Editor analysis coloring | `MacTextEditorRepresentable.swift` + `TextAnalyzer.swift` | Recreate attributed-text behavior in Windows editor | `Planned on top of shell baseline` |
| Right-click correction/context popover | `MacTextEditorRepresentable.swift` | Start with right-click popover on flagged words | `Planned` |
| Suggestion list rendering | `SuggestionPanelView.swift` semantics | Recreate with Windows-native UI | `Baseline done in separate overlay window` |
| Suggestion row metadata | `SuggestionPanelView.swift` candidate rows | Show inline match label plus POS/grammar metadata in Windows overlay | `Baseline done in internal editor path` |
| Suggestion row kind tinting | `SuggestionPanelView.swift` row background semantics | Preserve distinct visual treatment for ordinary / phonetic / misspelling / synonym | `Baseline done in internal editor path` |
| Suggestion row speaker/info actions | `SuggestionPanelView.swift` row actions | Recreate with Windows-native action buttons | `Baseline done in internal editor path` |
| In-app suggestion accept | `AppState` + local commit path | New Windows impl | `Baseline done in overlay` |
| Suggestion pagination | `AppState.pageSize` + `SuggestionPanelView` | Preserve 10-per-page and up to 4 pages | `Baseline done in internal editor path` |
| Static/follow-caret placement toggle | `SuggestionPanelView` + `AppState` | Preserve both modes in Windows overlay | `Baseline done in internal editor path` |
| Manual static placement behavior | `AppState` placement control semantics | Let user drag overlay and keep absolute static placement until moved again | `Baseline done for session runtime` |
| Follow-caret fallback policy | `AppState` + `MacSuggestionPanelController` | Fall back to static placement when caret confidence is low | `Baseline done in internal editor path` |
| Windows candidate shortcuts | macOS option-based shortcuts | Use `Ctrl+1` through `Ctrl+0` on Windows | `Baseline done` |
| Global key capture | `GlobalKeyCaptureManager.swift` | New Windows impl | `Planned: WSA-RT-003` |
| Cross-app focused text access | macOS AX | New Windows impl via Windows APIs | `Planned: WSA-RT-003` |
| Caret anchor extraction | macOS AX | New Windows impl via Windows APIs | `Planned: WSA-RT-003` |
| Floating suggestion panel | `MacSuggestionPanelController.swift` | New Windows impl | `Baseline done in internal editor path` |
| Settings semantics | `SettingsView.swift` | Preserve semantics, render natively on Windows | `Planned: WSA-UX-010` |
| OCR / screen snip | `ScreenSnipper.swift` | New Windows impl | `Planned: WSA-RT-011` |
| Speech to text | `SpeechToTextService.swift` | New Windows impl | `Planned: WSA-RT-012` |
| Suggestion row text to speech | `SuggestionPanelView.swift` + system TTS | New Windows impl for overlay row speaker action | `Baseline done in internal editor path` |
| Full app text to speech feature | `NeuralTTSService.swift` / system TTS | New Windows impl | `Planned: WSA-RT-013` |
| Error insights / diagnostics UI | macOS app views | New Windows impl, later milestone | `Planned: WSA-RT-014` |

## First milestone parity target

The next Windows UI parity milestone should cover:

- Internal editor correction and analysis styling baseline
- Right-click correction/context popover on flagged words
- Cleaner visual refinement of the overlay and editor pairing
- Toolbar feature parity roadmap from `WSA-DX-003`, starting with language selection and external-input foundations

## Highest-risk parity areas

1. Global input capture
2. Caret extraction in external apps
3. Committing text back into third-party apps
4. Overlay placement reliability across different Windows apps
