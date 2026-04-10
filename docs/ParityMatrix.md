# WordSuggestor macOS -> Windows Parity Matrix

Last updated: `2026-04-10`
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
| Expand/collapse toolbar -> editor shell | `FloatingToolbarInstaller` sizing/placement | Rebuild in Windows panel host | `Baseline done` |
| Suggestion engine | `WordSuggestorCore` | Reuse shared core | `Shared` |
| Language packs / SQLite lexicon | `WordSuggestorCore` | Reuse shared core and pack assets | `Shared` |
| Internal editor typing flow | SwiftUI/AppKit app state | Rebuild in Windows app shell | `Baseline done inside structured editor shell` |
| Editor surface structure | `WordSugestorApp.swift` + `TextAnalyzerPanelView.swift` | Rebuild command row, editor area, status row, and analyzer panel layout natively on Windows | `Baseline done` |
| Editor analysis coloring | `MacTextEditorRepresentable.swift` + `TextAnalyzer.swift` | Recreate attributed-text behavior in Windows editor | `Planned on top of shell baseline` |
| Right-click correction/context popover | `MacTextEditorRepresentable.swift` | Start with right-click popover on flagged words | `Planned` |
| Suggestion list rendering | `SuggestionPanelView.swift` semantics | Recreate with Windows-native UI | `Baseline done in separate overlay window` |
| In-app suggestion accept | `AppState` + local commit path | New Windows impl | `Baseline done in overlay` |
| Suggestion pagination | `AppState.pageSize` + `SuggestionPanelView` | Preserve 10-per-page and up to 4 pages | `Baseline done in internal editor path` |
| Static/follow-caret placement toggle | `SuggestionPanelView` + `AppState` | Preserve both modes in Windows overlay | `Baseline done in internal editor path` |
| Follow-caret fallback policy | `AppState` + `MacSuggestionPanelController` | Fall back to static placement when caret confidence is low | `Baseline done in internal editor path` |
| Windows candidate shortcuts | macOS option-based shortcuts | Use `Ctrl+1` through `Ctrl+0` on Windows | `Baseline done` |
| Global key capture | `GlobalKeyCaptureManager.swift` | New Windows impl | `Deferred until overlay parity is stable` |
| Cross-app focused text access | macOS AX | New Windows impl via Windows APIs | `Deferred until overlay parity is stable` |
| Caret anchor extraction | macOS AX | New Windows impl via Windows APIs | `Deferred until overlay parity is stable` |
| Floating suggestion panel | `MacSuggestionPanelController.swift` | New Windows impl | `Baseline done in internal editor path` |
| Settings semantics | `SettingsView.swift` | Preserve semantics, render natively on Windows | `Planned` |
| OCR / screen snip | `ScreenSnipper.swift` | New Windows impl | `Deferred` |
| Speech to text | `SpeechToTextService.swift` | New Windows impl | `Deferred` |
| Text to speech | `NeuralTTSService.swift` / system TTS | New Windows impl | `Deferred` |
| Error insights / diagnostics UI | macOS app views | New Windows impl, later milestone | `Deferred` |

## First milestone parity target

The next Windows UI parity milestone should cover:

- Internal editor correction and analysis styling baseline
- Right-click correction/context popover on flagged words
- Cleaner visual refinement of the overlay and editor pairing

## Highest-risk parity areas

1. Global input capture
2. Caret extraction in external apps
3. Committing text back into third-party apps
4. Overlay placement reliability across different Windows apps
