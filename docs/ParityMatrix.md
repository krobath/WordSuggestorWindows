# WordSuggestor macOS -> Windows Parity Matrix

Last updated: `2026-04-09`
Status: `Initial baseline`

## Legend

- `Shared` - expected to reuse `WordSuggestorCore` or shared semantics
- `New Windows impl` - requires Windows-native implementation
- `Deferred` - not part of the first Windows milestone

| Capability | macOS source | Windows strategy | Status |
|---|---|---|---|
| Suggestion engine | `WordSuggestorCore` | Reuse shared core | `Shared` |
| Language packs / SQLite lexicon | `WordSuggestorCore` | Reuse shared core and pack assets | `Shared` |
| Internal editor typing flow | SwiftUI/AppKit app state | Rebuild in Windows app shell | `Planned` |
| Suggestion list rendering | `SuggestionPanelView.swift` semantics | Recreate with Windows-native UI | `Planned` |
| In-app suggestion accept | `AppState` + local commit path | New Windows impl | `Planned` |
| Global key capture | `GlobalKeyCaptureManager.swift` | New Windows impl | `Deferred` |
| Cross-app focused text access | macOS AX | New Windows impl via Windows APIs | `Deferred` |
| Caret anchor extraction | macOS AX | New Windows impl via Windows APIs | `Deferred` |
| Floating suggestion panel | `MacSuggestionPanelController.swift` | New Windows impl | `Planned` |
| Settings semantics | `SettingsView.swift` | Preserve semantics, render natively on Windows | `Planned` |
| OCR / screen snip | `ScreenSnipper.swift` | New Windows impl | `Deferred` |
| Speech to text | `SpeechToTextService.swift` | New Windows impl | `Deferred` |
| Text to speech | `NeuralTTSService.swift` / system TTS | New Windows impl | `Deferred` |
| Error insights / diagnostics UI | macOS app views | New Windows impl, later milestone | `Deferred` |

## First milestone parity target

The first Windows milestone should cover:

- Local app shell
- Internal editor
- Local suggestions from `WordSuggestorCore`
- Suggestion accept/replace inside the Windows app
- Minimal settings surface needed to exercise the feature

## Highest-risk parity areas

1. Global input capture
2. Caret extraction in external apps
3. Commiting text back into third-party apps
4. Overlay placement reliability across different Windows apps
